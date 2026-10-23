using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Simulador.Dispositivos;

namespace Simulador.EntradaReal;

/// <summary>
/// Captura global (todo el sistema) del teclado y mouse físicos mediante hooks de bajo nivel
/// (WH_KEYBOARD_LL / WH_MOUSE_LL). Los hooks corren en un hilo propio con bucle de mensajes;
/// los eventos se depositan en una cola concurrente que el hilo principal drena con TryLeer().
/// La entrada nunca se bloquea: siempre se pasa al siguiente hook (las demás apps la reciben).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CapturaEntradaGlobal : IDisposable
{
    private const int IntervaloMovimientoMs = 50;

    private readonly ConcurrentQueue<EventoEntrada> _eventos = new();
    private readonly byte[] _estadoTeclas = new byte[256];
    private readonly char[] _bufferUnicode = new char[8];

    // Referencias a los delegados: evitan que el GC los recolecte mientras Windows los invoca
    private readonly Win32.HookProc _procTeclado;
    private readonly Win32.HookProc _procMouse;

    private IntPtr _hookTeclado;
    private IntPtr _hookMouse;
    private Thread? _hilo;
    private uint _idHilo;
    private long _ultimoMovimiento;

    public bool Activa { get; private set; }

    public CapturaEntradaGlobal()
    {
        _procTeclado = ProcesarTeclado;
        _procMouse = ProcesarMouse;
    }

    public void Iniciar()
    {
        if (Activa) return;

        using var listo = new ManualResetEventSlim(false);
        Exception? error = null;

        _hilo = new Thread(() =>
        {
            _idHilo = Win32.GetCurrentThreadId();
            IntPtr modulo = Win32.GetModuleHandleW(null);
            _hookTeclado = Win32.SetWindowsHookExW(Win32.WH_KEYBOARD_LL, _procTeclado, modulo, 0);
            _hookMouse = Win32.SetWindowsHookExW(Win32.WH_MOUSE_LL, _procMouse, modulo, 0);

            if (_hookTeclado == IntPtr.Zero || _hookMouse == IntPtr.Zero)
            {
                error = new Win32Exception(Marshal.GetLastWin32Error(), "No se pudieron instalar los hooks globales.");
                QuitarHooks();
                listo.Set();
                return;
            }

            listo.Set();

            // Bucle de mensajes: requerido para que Windows entregue los eventos a los hooks LL
            while (Win32.GetMessageW(out _, IntPtr.Zero, 0, 0) > 0) { }

            QuitarHooks();
        })
        {
            IsBackground = true,
            Name = "CapturaEntradaGlobal"
        };

        _hilo.Start();
        listo.Wait();

        if (error is not null)
        {
            _hilo.Join();
            throw error;
        }

        Activa = true;
    }

    public bool TryLeer(out EventoEntrada evento) => _eventos.TryDequeue(out evento!);

    public void Detener()
    {
        if (!Activa) return;
        Win32.PostThreadMessageW(_idHilo, Win32.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _hilo?.Join(2000);
        Activa = false;
        _eventos.Clear();
    }

    public void Dispose() => Detener();

    private void QuitarHooks()
    {
        if (_hookTeclado != IntPtr.Zero) Win32.UnhookWindowsHookEx(_hookTeclado);
        if (_hookMouse != IntPtr.Zero) Win32.UnhookWindowsHookEx(_hookMouse);
        _hookTeclado = _hookMouse = IntPtr.Zero;
    }

    // Callbacks: deben ser mínimos (Windows elimina el hook si excede LowLevelHooksTimeout)
    private IntPtr ProcesarTeclado(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == Win32.WM_KEYDOWN || wParam == Win32.WM_SYSKEYDOWN))
        {
            var datos = Marshal.PtrToStructure<Win32.KBDLLHOOKSTRUCT>(lParam);
            if (datos.vkCode == Win32.VK_ESCAPE)
                _eventos.Enqueue(new EventoEntrada.Escape());
            else if (TraducirTecla(datos) is char c)
                _eventos.Enqueue(new EventoEntrada.Tecla(c));
        }
        return Win32.CallNextHookEx(_hookTeclado, nCode, wParam, lParam);
    }

    private IntPtr ProcesarMouse(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var datos = Marshal.PtrToStructure<Win32.MSLLHOOKSTRUCT>(lParam);
            int x = datos.pt.x, y = datos.pt.y;

            switch ((int)wParam)
            {
                case Win32.WM_LBUTTONDOWN:
                    _eventos.Enqueue(new EventoEntrada.Mouse(new EventoMouse(TipoEventoMouse.ClicIzq, x, y)));
                    break;
                case Win32.WM_RBUTTONDOWN:
                    _eventos.Enqueue(new EventoEntrada.Mouse(new EventoMouse(TipoEventoMouse.ClicDer, x, y)));
                    break;
                case Win32.WM_MBUTTONDOWN:
                    _eventos.Enqueue(new EventoEntrada.Mouse(new EventoMouse(TipoEventoMouse.ClicCentral, x, y)));
                    break;
                case Win32.WM_MOUSEWHEEL:
                    int delta = (short)(datos.mouseData >> 16);
                    _eventos.Enqueue(new EventoEntrada.Mouse(new EventoMouse(TipoEventoMouse.Rueda, x, y, delta)));
                    break;
                case Win32.WM_MOUSEMOVE:
                    // Movimiento coalescido: como máximo un evento cada 50 ms
                    long ahora = Environment.TickCount64;
                    if (ahora - _ultimoMovimiento >= IntervaloMovimientoMs)
                    {
                        _ultimoMovimiento = ahora;
                        _eventos.Enqueue(new EventoEntrada.Movimiento(x, y));
                    }
                    break;
            }
        }
        return Win32.CallNextHookEx(_hookMouse, nCode, wParam, lParam);
    }

    /// <summary>
    /// Traduce la tecla virtual al carácter según la distribución de teclado de la ventana activa
    /// (respeta Shift, Bloq Mayús y AltGr). Devuelve null para teclas no imprimibles.
    /// </summary>
    private char? TraducirTecla(Win32.KBDLLHOOKSTRUCT datos)
    {
        if (datos.vkCode == Win32.VK_RETURN)
            return '↵';

        Array.Clear(_estadoTeclas);
        if (Win32.GetAsyncKeyState(Win32.VK_SHIFT) < 0) _estadoTeclas[Win32.VK_SHIFT] = 0x80;
        if (Win32.GetAsyncKeyState(Win32.VK_CONTROL) < 0) _estadoTeclas[Win32.VK_CONTROL] = 0x80;
        if (Win32.GetAsyncKeyState(Win32.VK_MENU) < 0) _estadoTeclas[Win32.VK_MENU] = 0x80;
        if ((Win32.GetKeyState(Win32.VK_CAPITAL) & 1) != 0) _estadoTeclas[Win32.VK_CAPITAL] = 0x01;

        uint hiloActivo = Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), IntPtr.Zero);
        IntPtr distribucion = Win32.GetKeyboardLayout(hiloActivo);

        int n = Win32.ToUnicodeEx(datos.vkCode, datos.scanCode, _estadoTeclas,
            _bufferUnicode, _bufferUnicode.Length, Win32.TOUNICODE_NO_STATE_CHANGE, distribucion);

        if (n != 1 || char.IsControl(_bufferUnicode[0]))
            return null;
        return _bufferUnicode[0];
    }
}
