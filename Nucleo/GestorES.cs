using Simulador.Controladores;
using Simulador.Dispositivos;
using Simulador.Modelo;

namespace Simulador.Nucleo;

/// <summary>
/// Gestor de E/S del Sistema Operativo simulado (Kernel Space)
/// </summary>
public sealed class GestorES
{
    private int _siguienteId = 1;

    public RelojLogico Reloj { get; } = new();
    public ColaSolicitudes ColaGeneral { get; } = new();
    public VectorInterrupciones Vector { get; } = new();
    public Bitacora Bitacora { get; }
    public Estadisticas Estadisticas { get; } = new();

    public TecladoVirtual Teclado { get; } = new();
    public DiscoVirtual Disco { get; } = new();
    public ImpresoraVirtual Impresora { get; } = new();
    public MouseVirtual Mouse { get; } = new();

    public ControladorTeclado ControladorTeclado { get; }
    public ControladorDisco ControladorDisco { get; }
    public ControladorImpresora ControladorImpresora { get; }
    public ControladorMouse ControladorMouse { get; }
    public IReadOnlyList<ControladorDispositivo> Controladores { get; }

    public GestorES()
    {
        Bitacora = new Bitacora(Reloj);

        ControladorTeclado = new ControladorTeclado(Teclado, Reloj, Vector, Bitacora, Estadisticas);
        ControladorDisco = new ControladorDisco(Disco, Reloj, Vector, Bitacora, Estadisticas);
        ControladorImpresora = new ControladorImpresora(Impresora, Reloj, Vector, Bitacora, Estadisticas);
        ControladorMouse = new ControladorMouse(Mouse, Reloj, Vector, Bitacora, Estadisticas);

        Controladores = new ControladorDispositivo[]
        {
            ControladorTeclado,
            ControladorDisco,
            ControladorImpresora,
            ControladorMouse
        };

        // Registrar las ISR en el vector de interrupciones
        foreach (var ctrl in Controladores)
        {
            Vector.Registrar((int)ctrl.Tipo, ctrl.AtenderInterrupcion);
        }
    }

    /// <summary>
    /// Llamada al sistema SolicitarES(): único punto de entrada desde la capa de aplicación.
    /// </summary>
    public SolicitudES SolicitarES(
        TipoDispositivo dispositivo,
        TipoOperacion operacion,
        string datos = "",
        int bloque = 0,
        int procesoId = 1)
    {
        var solicitud = new SolicitudES
        {
            Id = _siguienteId++,
            ProcesoId = procesoId,
            Dispositivo = dispositivo,
            Operacion = operacion,
            Datos = datos,
            Bloque = bloque,
            TickCreacion = Reloj.Tick,
            Estado = EstadoSolicitud.Pendiente
        };

        ColaGeneral.Encolar(solicitud);
        Estadisticas.RegistrarCreacion();
        Bitacora.RegistrarEvento($"[SO] Solicitud creada: {solicitud}");
        return solicitud;
    }

    /// <summary>
    /// Despacha todas las solicitudes pendientes en la cola general hacia sus controladores respectivos.
    /// (Fase 3 del Tick, o invocable de inmediato al inicializar un escenario en el tick 0).
    /// </summary>
    public void Despachar()
    {
        while (!ColaGeneral.EstaVacia)
        {
            var s = ColaGeneral.Desencolar()!;
            var controlador = ObtenerControlador(s.Dispositivo);
            var codigo = controlador.Recibir(s);

            if (codigo != CodigoError.Ninguno)
            {
                Bitacora.Registrar(s, huboInterrupcion: false);
                Estadisticas.RegistrarRechazo(s);
                Bitacora.RegistrarEvento(FormatearMensajeError(s, codigo));
            }
        }
    }

    /// <summary>
    /// Ejecuta un ciclo del reloj lógico siguiendo las fases del diseño:
    /// Si hay solicitudes en la cola general antes de avanzar los dispositivos (por ejemplo, creadas en este tick),
    /// se pueden despachar o seguir el ciclo Fase 1 (Avance) -> Fase 2 (Interrupciones) -> Fase 3 (Despacho) -> Fase 4 (Reloj++).
    /// </summary>
    public void Tick(Action? generadorFase0 = null)
    {
        // Fase 0: Generación de carga en el tick actual
        generadorFase0?.Invoke();

        // Si existen solicitudes recién creadas en el tick actual antes de avanzar (p. ej. en tick 0),
        // despacharlas permite que inicien en el mismo tick de creación (como en la Tabla 10 del documento).
        Despachar();

        // Fase 4 (avance del marcador del reloj al cierre del ciclo de 1 tick)
        Reloj.Avanzar();

        // Fase 1: Avance de todos los dispositivos en paralelo lógico
        foreach (var ctrl in Controladores)
        {
            ctrl.AvanzarTick();
        }

        // Fase 2: Atención de interrupciones en orden de prioridad (IRQ0 -> IRQ1 -> IRQ2)
        Vector.Atender();

        // Fase 3: Despacho de cualquier solicitud remanente en la cola general
        Despachar();
    }

    public ControladorDispositivo ObtenerControlador(TipoDispositivo tipo) =>
        Controladores[(int)tipo];

    public int ContarSolicitudesPendientes() =>
        ColaGeneral.Cantidad + Controladores.Sum(c => c.ColaLocal.Cantidad);

    public int ContarSolicitudesEnProceso() =>
        Controladores.Count(c => c.Actual is not null);

    public bool TieneTrabajoPendiente() =>
        !ColaGeneral.EstaVacia || Controladores.Any(c => c.Actual is not null || !c.ColaLocal.EstaVacia);

    private static string FormatearMensajeError(SolicitudES s, CodigoError codigo) => codigo switch
    {
        CodigoError.E01_FueraDeServicio =>
            $"[WARNING] {s.Dispositivo} rechaza petición #{s.Id}: Dispositivo en estado de falla. ERROR: Dispositivo fuera de servicio. (E01)",
        CodigoError.E02_ColaLlena =>
            $"[ERROR] {s.Dispositivo} rechaza petición #{s.Id}: ERROR: El dispositivo está ocupado (cola local llena). (E02)",
        CodigoError.E03_BloqueInvalido =>
            $"[ERROR] {s.Dispositivo} rechaza petición #{s.Id}: Bloque {s.Bloque} fuera del rango permitido (0-1023). (E03)",
        CodigoError.E05_OperacionNoSoportada =>
            $"[ERROR] {s.Dispositivo} rechaza petición #{s.Id}: Operación '{s.Operacion}' no soportada por este dispositivo. (E05)",
        _ => $"[ERROR] {s.Dispositivo} rechaza petición #{s.Id}: {codigo}"
    };
}
