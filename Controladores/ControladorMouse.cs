using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Controladores;

public sealed class ControladorMouse : ControladorDispositivo
{
    public MouseVirtual Mouse => (MouseVirtual)Dispositivo;

    public ControladorMouse(
        MouseVirtual dispositivo,
        RelojLogico reloj,
        VectorInterrupciones vector,
        Bitacora bitacora,
        Estadisticas estadisticas)
        : base(TipoDispositivo.Mouse, dispositivo, reloj, vector, bitacora, estadisticas)
    {
    }

    // El mouse solo produce datos: escribir no tiene sentido (E05)
    protected override bool SoportaOperacion(TipoOperacion op) =>
        op != TipoOperacion.Escritura;

    /// <summary>
    /// Recibe un clic o giro de rueda del usuario hacia el buffer del mouse.
    /// Si el buffer se desborda (E08), registra el error y emite campana simulada.
    /// </summary>
    public bool RecibirEventoUsuario(EventoMouse evento)
    {
        bool ok = Mouse.RecibirEvento(evento);
        if (!ok)
        {
            _estadisticas.RegistrarError(CodigoError.E08_BufferMouseLleno);
            _bitacora.RegistrarEvento($"[BEEP / WARNING] Mouse: Buffer de entrada lleno (E08). Evento '{evento}' descartado.");
        }
        else
        {
            _bitacora.RegistrarEvento($"[MOUSE] Evento '{evento}' almacenado en buffer ({Mouse.EventosEnBuffer}/{MouseVirtual.CapacidadBuffer}).");
        }
        return ok;
    }

    // El movimiento solo actualiza la posición; no genera entradas en bitácora para no saturarla
    public void ActualizarPosicion(int x, int y) => Mouse.ActualizarPosicion(x, y);
}
