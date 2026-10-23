using Simulador.Modelo;

namespace Simulador.Nucleo;

public sealed class Estadisticas
{
    private readonly Dictionary<TipoDispositivo, int> _ticksUso = new()
    {
        [TipoDispositivo.Teclado] = 0,
        [TipoDispositivo.Disco] = 0,
        [TipoDispositivo.Impresora] = 0,
        [TipoDispositivo.Mouse] = 0
    };

    private readonly Dictionary<TipoDispositivo, int> _operacionesPorDispositivo = new()
    {
        [TipoDispositivo.Teclado] = 0,
        [TipoDispositivo.Disco] = 0,
        [TipoDispositivo.Impresora] = 0,
        [TipoDispositivo.Mouse] = 0
    };

    private int _sumaTiemposAtencion;

    public int SolicitudesCreadas { get; private set; }
    public int SolicitudesFinalizadas { get; private set; }
    public int SolicitudesFallidas { get; private set; }
    public int InterrupcionesAtendidas { get; private set; }
    public int ErroresDetectados { get; private set; }

    public double TiempoPromedioAtencion =>
        SolicitudesFinalizadas > 0 ? (double)_sumaTiemposAtencion / SolicitudesFinalizadas : 0.0;

    public IReadOnlyDictionary<TipoDispositivo, int> TicksUso => _ticksUso;
    public IReadOnlyDictionary<TipoDispositivo, int> OperacionesPorDispositivo => _operacionesPorDispositivo;

    public void RegistrarCreacion() => SolicitudesCreadas++;

    public void Registrar(SolicitudES s)
    {
        if (s.Estado == EstadoSolicitud.Finalizada)
        {
            SolicitudesFinalizadas++;
            InterrupcionesAtendidas++;
            _operacionesPorDispositivo[s.Dispositivo]++;
            _sumaTiemposAtencion += (s.TickFin - s.TickCreacion);
        }
    }

    public void RegistrarRechazo(SolicitudES s)
    {
        SolicitudesFallidas++;
        ErroresDetectados++;
    }

    public void RegistrarError(CodigoError error)
    {
        if (error == CodigoError.E04_ReintentosAgotados)
            SolicitudesFallidas++;
        ErroresDetectados++;
    }

    public void RegistrarTickUso(TipoDispositivo tipo)
    {
        _ticksUso[tipo]++;
    }

    public (TipoDispositivo Dispositivo, int Operaciones, int Ticks) ObtenerDispositivoMasUtilizado()
    {
        var mejor = _ticksUso
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => _operacionesPorDispositivo[kv.Key])
            .First();
        return (mejor.Key, _operacionesPorDispositivo[mejor.Key], mejor.Value);
    }

    public void Limpiar()
    {
        SolicitudesCreadas = 0;
        SolicitudesFinalizadas = 0;
        SolicitudesFallidas = 0;
        InterrupcionesAtendidas = 0;
        ErroresDetectados = 0;
        _sumaTiemposAtencion = 0;
        foreach (var k in _ticksUso.Keys.ToList())
        {
            _ticksUso[k] = 0;
            _operacionesPorDispositivo[k] = 0;
        }
    }
}
