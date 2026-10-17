using Simulador.Modelo;

namespace Simulador.Dispositivos;

public sealed class DiscoVirtual : IDispositivoVirtual
{
    public const int TotalBloques = 1024;
    public const int CiclosArranque = 4;
    public const int CiclosTotales = 10;
    private const int MaxReintentos = 3;

    private readonly string[] _bloques = new string[TotalBloques];
    private SolicitudES? _actual;
    private int _transcurridos;
    private int _intentos;
    private int _fallosPendientes;

    public EstadoDispositivo Estado { get; private set; } = EstadoDispositivo.Disponible;
    public int RegistroControl { get; private set; }
    public int RegistroError { get; private set; }
    public string RegistroDatos { get; private set; } = "";
    public int CiclosRestantes { get; private set; }

    public string LeerBloqueDirecto(int n) => (n >= 0 && n < TotalBloques) ? (_bloques[n] ?? "") : "";
    public void EscribirBloqueDirecto(int n, string datos)
    {
        if (n >= 0 && n < TotalBloques) _bloques[n] = datos;
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
        RegistroDatos = s.Operacion == TipoOperacion.Escritura ? s.Datos : "";
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
            _fallosPendientes--; // fallo de comunicación
            if (++_intentos > MaxReintentos) // más de 3 reintentos
            {
                Estado = EstadoDispositivo.FueraDeServicio;
                RegistroError = (int)CodigoError.E04_ReintentosAgotados;
            }
            return false; // el tick se pierde
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
            RegistroDatos = _bloques[_actual!.Bloque] ?? "";
        else if (RegistroControl == 2)
            _bloques[_actual!.Bloque] = RegistroDatos;
        else if (RegistroControl == 3)
            RegistroDatos = $"Estado={Estado}, Bloques={TotalBloques}";

        return true; // el controlador levanta la IRQ1
    }

    // Programa fallos de comunicación consecutivos para la próxima operación
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
}
