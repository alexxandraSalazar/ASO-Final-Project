using Simulador.Modelo;

namespace Simulador.Dispositivos;

public sealed class ImpresoraVirtual : IDispositivoVirtual
{
    public const int HojasPorDefecto = 20;
    public const int CiclosArranque = 2;
    public const int CiclosTotales = 8;
    private const int MaxReintentos = 3;

    private SolicitudES? _actual;
    private int _transcurridos;
    private int _intentos;
    private int _fallosPendientes;
    private bool _fallaSinPapelInyectada;

    public int Hojas { get; private set; }
    public EstadoDispositivo Estado { get; private set; } = EstadoDispositivo.Disponible;
    public int RegistroControl { get; private set; }
    public int RegistroError { get; private set; }
    public string RegistroDatos { get; private set; } = "";
    public int CiclosRestantes { get; private set; }

    public ImpresoraVirtual(int hojasIniciales = HojasPorDefecto)
    {
        Hojas = hojasIniciales;
    }

    public void RecargarPapel(int cantidad = HojasPorDefecto)
    {
        Hojas = cantidad;
        _fallaSinPapelInyectada = false;
    }

    public void EstablecerHojas(int hojas)
    {
        Hojas = Math.Max(0, hojas);
        if (Hojas == 0)
        {
            Estado = EstadoDispositivo.FueraDeServicio;
            RegistroError = (int)CodigoError.E07_SinPapel;
        }
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

        // Al terminar el arranque y pasar a la transferencia, verifica el papel
        if (_transcurridos > arranque)
        {
            if (_actual?.Operacion == TipoOperacion.Escritura && (Hojas <= 0 || _fallaSinPapelInyectada))
            {
                Hojas = 0;
                _fallaSinPapelInyectada = false;
                Estado = EstadoDispositivo.FueraDeServicio;
                RegistroError = (int)CodigoError.E07_SinPapel;
                return false;
            }
            Estado = EstadoDispositivo.Procesando;
        }
        else
        {
            Estado = EstadoDispositivo.Ocupado;
        }

        if (CiclosRestantes > 0)
            return false;

        if (RegistroControl == 2)
        {
            if (Hojas > 0) Hojas--;
        }
        else if (RegistroControl == 3)
        {
            RegistroDatos = $"Estado={Estado}, Hojas={Hojas}";
        }

        return true; // levanta IRQ2
    }

    public void InyectarFalla(CodigoError codigo, int repeticiones = 1)
    {
        switch (codigo)
        {
            case CodigoError.E04_ReintentosAgotados:
                _fallosPendientes = repeticiones;
                break;
            case CodigoError.E07_SinPapel:
                if (Estado == EstadoDispositivo.Disponible)
                {
                    Hojas = 0;
                    Estado = EstadoDispositivo.FueraDeServicio;
                    RegistroError = (int)CodigoError.E07_SinPapel;
                }
                else
                {
                    _fallaSinPapelInyectada = true;
                }
                break;
            case CodigoError.E01_FueraDeServicio:
                Estado = EstadoDispositivo.FueraDeServicio;
                RegistroError = (int)CodigoError.E01_FueraDeServicio;
                break;
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
        _fallaSinPapelInyectada = false;
        _intentos = 0;
        _transcurridos = 0;
    }
}
