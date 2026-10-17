using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Controladores;

public sealed class ControladorImpresora : ControladorDispositivo
{
    public ImpresoraVirtual Impresora => (ImpresoraVirtual)Dispositivo;

    public ControladorImpresora(
        ImpresoraVirtual dispositivo,
        RelojLogico reloj,
        VectorInterrupciones vector,
        Bitacora bitacora,
        Estadisticas estadisticas)
        : base(TipoDispositivo.Impresora, dispositivo, reloj, vector, bitacora, estadisticas)
    {
    }

    // La impresora solo recibe datos: leer no tiene sentido (E05)
    protected override bool SoportaOperacion(TipoOperacion op) =>
        op != TipoOperacion.Lectura;

    public override void Reiniciar()
    {
        ((ImpresoraVirtual)Dispositivo).RecargarPapel(); // el operador repone papel
        _bitacora.RegistrarEvento("[IMPRESORA] Papel recargado (20 hojas).");
        base.Reiniciar();
    }
}
