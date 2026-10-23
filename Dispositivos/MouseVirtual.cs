using Simulador.Modelo;

namespace Simulador.Dispositivos;

public sealed class MouseVirtual : IDispositivoVirtual
{
    public const int CapacidadBuffer = 16;
    public const int CiclosArranque = 1;
    public const int CiclosTotales = 2;
    private const int MaxReintentos = 3;

    private readonly Queue<EventoMouse> _buffer = new(CapacidadBuffer);
    private SolicitudES? _actual;
    private int _transcurridos;
    private int _intentos;
    private int _fallosPendientes;

    public EstadoDispositivo Estado { get; private set; } = EstadoDispositivo.Disponible;
    public int RegistroControl { get; private set; }
    public int RegistroError { get; private set; }
    public string RegistroDatos { get; private set; } = "";
    public int CiclosRestantes { get; private set; }
    public int EventosEnBuffer => _buffer.Count;

    // Última posición conocida del puntero (el movimiento no se almacena en el buffer)
    public int PosicionX { get; private set; }
    public int PosicionY { get; private set; }

    /// <summary>
    /// Recibe un clic o giro de rueda del mouse.
    /// Si el buffer de 16 posiciones está lleno, produce E08_BufferMouseLleno.
    /// </summary>
    public bool RecibirEvento(EventoMouse e)
    {
        ActualizarPosicion(e.X, e.Y);
        if (_buffer.Count >= CapacidadBuffer)
        {
            RegistroError = (int)CodigoError.E08_BufferMouseLleno;
            return false;
        }
        _buffer.Enqueue(e);
        return true;
    }

    public void ActualizarPosicion(int x, int y)
    {
        PosicionX = x;
        PosicionY = y;
    }

    public void Comenzar(SolicitudES s)
    {
        _actual = s;
        RegistroControl = s.Operacion switch
        {
            TipoOperacion.Lectura => 1,
            TipoOperacion.Escritura => 2,
            _ => 3
        };
        RegistroDatos = "";
        RegistroError = 0;
        CiclosRestantes = s.Operacion == TipoOperacion.ConsultarEstado ? 1 : CiclosTotales;
        _transcurridos = 0;
        _intentos = 0;
        Estado = EstadoDispositivo.Ocupado;
    }

    public bool Avanzar()
    {
        if (Estado is not (EstadoDispositivo.Ocupado or EstadoDispositivo.Procesando))
            return false;

        if (Estado == EstadoDispositivo.Ocupado && _fallosPendientes > 0)
        {
            _fallosPendientes--;
            if (++_intentos > MaxReintentos)
            {
                Estado = EstadoDispositivo.FueraDeServicio;
                RegistroError = (int)CodigoError.E04_ReintentosAgotados;
            }
            return false;
        }

        CiclosRestantes--;
        _transcurridos++;
        int arranque = _actual?.Operacion == TipoOperacion.ConsultarEstado ? 0 : CiclosArranque;
        Estado = _transcurridos <= arranque
            ? EstadoDispositivo.Ocupado
            : EstadoDispositivo.Procesando;

        if (CiclosRestantes > 0)
            return false;

        if (RegistroControl == 1)
        {
            RegistroDatos = _buffer.Count > 0 ? _buffer.Dequeue().ToString() : "";
        }
        else if (RegistroControl == 3)
        {
            RegistroDatos = $"Estado={Estado}, Pos=({PosicionX},{PosicionY}), Buffer={_buffer.Count}/{CapacidadBuffer}";
        }

        return true;
    }

    public void InyectarFalla(CodigoError codigo, int repeticiones = 1)
    {
        if (codigo == CodigoError.E04_ReintentosAgotados)
        {
            _fallosPendientes = repeticiones;
        }
        else if (codigo == CodigoError.E01_FueraDeServicio)
        {
            Estado = EstadoDispositivo.FueraDeServicio;
            RegistroError = (int)CodigoError.E01_FueraDeServicio;
        }
    }

    public void Reiniciar()
    {
        Estado = EstadoDispositivo.Disponible;
        RegistroControl = 0;
        RegistroError = 0;
        CiclosRestantes = 0;
        RegistroDatos = "";
        _fallosPendientes = 0;
        _intentos = 0;
        _transcurridos = 0;
    }

    public void LimpiarBuffer() => _buffer.Clear();
}
