using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Controladores;

public sealed class ControladorDisco : ControladorDispositivo
{
    public DiscoVirtual Disco => (DiscoVirtual)Dispositivo;

    public ControladorDisco(
        DiscoVirtual dispositivo,
        RelojLogico reloj,
        VectorInterrupciones vector,
        Bitacora bitacora,
        Estadisticas estadisticas)
        : base(TipoDispositivo.Disco, dispositivo, reloj, vector, bitacora, estadisticas)
    {
    }

    protected override bool SoportaOperacion(TipoOperacion op) => true;

    protected override CodigoError Validar(SolicitudES s) =>
        s.Operacion != TipoOperacion.ConsultarEstado &&
        (s.Bloque < 0 || s.Bloque >= DiscoVirtual.TotalBloques)
            ? CodigoError.E03_BloqueInvalido
            : CodigoError.Ninguno;
}
