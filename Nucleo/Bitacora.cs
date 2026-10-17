using Simulador.Modelo;

namespace Simulador.Nucleo;

public sealed record RegistroBitacora(
    int Numero,
    int SolicitudId,
    int ProcesoId,
    TipoDispositivo Dispositivo,
    TipoOperacion Operacion,
    EstadoSolicitud Estado,
    bool HuboInterrupcion,
    int TickCreacion,
    int TickFin,
    CodigoError Error,
    string Datos
);

public sealed class Bitacora
{
    private readonly List<RegistroBitacora> _registros = new();
    private readonly List<string> _eventos = new();
    private readonly RelojLogico _reloj;

    public IReadOnlyList<RegistroBitacora> Registros => _registros;
    public IReadOnlyList<string> Eventos => _eventos;

    public Bitacora(RelojLogico reloj)
    {
        _reloj = reloj;
    }

    public void Registrar(SolicitudES sol, bool huboInterrupcion)
    {
        var reg = new RegistroBitacora(
            Numero: _registros.Count + 1,
            SolicitudId: sol.Id,
            ProcesoId: sol.ProcesoId,
            Dispositivo: sol.Dispositivo,
            Operacion: sol.Operacion,
            Estado: sol.Estado,
            HuboInterrupcion: huboInterrupcion,
            TickCreacion: sol.TickCreacion,
            TickFin: sol.TickFin,
            Error: sol.Error,
            Datos: sol.Datos
        );
        _registros.Add(reg);
    }

    public void RegistrarEvento(string mensaje)
    {
        _eventos.Add($"[Tick {_reloj.Tick,3}] {mensaje}");
    }

    public void Limpiar()
    {
        _registros.Clear();
        _eventos.Clear();
    }
}
