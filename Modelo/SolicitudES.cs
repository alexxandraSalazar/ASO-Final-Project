namespace Simulador.Modelo;

/// <summary>
/// Solicitud de E/S (I/O Request Packet - IRP)
/// </summary>
public sealed class SolicitudES
{
    public int Id { get; init; }
    public int ProcesoId { get; init; }
    public TipoDispositivo Dispositivo { get; init; }
    public TipoOperacion Operacion { get; init; }
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    public string Datos { get; set; } = ""; // datos a escribir o leídos
    public int Bloque { get; init; }        // específico del Disco (LBA)
    public int TickCreacion { get; init; }
    public int TickInicio { get; set; }
    public int TickFin { get; set; }
    public CodigoError Error { get; set; } = CodigoError.Ninguno;

    public int TiempoAtencion => TickFin >= TickCreacion ? TickFin - TickCreacion : 0;
    public int TiempoEspera => TickInicio >= TickCreacion ? TickInicio - TickCreacion : 0;

    public override string ToString()
    {
        string extra = Dispositivo == TipoDispositivo.Disco && Operacion != TipoOperacion.ConsultarEstado
            ? $" [Bloque {Bloque}]"
            : (!string.IsNullOrEmpty(Datos) ? $" [\"{Datos}\"]" : "");
        return $"#{Id} (P{ProcesoId}) {Dispositivo} -> {Operacion}{extra} [{Estado}]";
    }
}
