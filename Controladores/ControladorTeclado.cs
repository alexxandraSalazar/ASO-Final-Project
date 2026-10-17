using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Controladores;

public sealed class ControladorTeclado : ControladorDispositivo
{
    public TecladoVirtual Teclado => (TecladoVirtual)Dispositivo;

    public ControladorTeclado(
        TecladoVirtual dispositivo,
        RelojLogico reloj,
        VectorInterrupciones vector,
        Bitacora bitacora,
        Estadisticas estadisticas)
        : base(TipoDispositivo.Teclado, dispositivo, reloj, vector, bitacora, estadisticas)
    {
    }

    // El teclado solo produce datos: escribir no tiene sentido (E05)
    protected override bool SoportaOperacion(TipoOperacion op) =>
        op != TipoOperacion.Escritura;

    /// <summary>
    /// Recibe una pulsación del usuario hacia el buffer del teclado.
    /// Si el buffer se desborda (E06), registra el error y emite campana simulada.
    /// </summary>
    public bool RecibirPulsacionUsuario(char tecla)
    {
        bool ok = Teclado.RecibirTecla(tecla);
        if (!ok)
        {
            _estadisticas.RegistrarError(CodigoError.E06_BufferTecladoLleno);
            _bitacora.RegistrarEvento($"[BEEP / WARNING] Teclado: Buffer de entrada lleno (E06). Pulsación '{tecla}' descartada.");
        }
        else
        {
            _bitacora.RegistrarEvento($"[TECLADO] Pulsación '{tecla}' almacenada en buffer ({Teclado.PulsacionesEnBuffer}/{TecladoVirtual.CapacidadBuffer}).");
        }
        return ok;
    }
}
