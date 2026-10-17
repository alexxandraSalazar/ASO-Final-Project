using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Controladores;

public abstract class ControladorDispositivo
{
    protected readonly RelojLogico _reloj;
    protected readonly VectorInterrupciones _vector;
    protected readonly Bitacora _bitacora;
    protected readonly Estadisticas _estadisticas;

    public TipoDispositivo Tipo { get; }
    public IDispositivoVirtual Dispositivo { get; }
    public ColaSolicitudes ColaLocal { get; } = new(capacidad: 5);
    public SolicitudES? Actual { get; private set; }

    protected ControladorDispositivo(
        TipoDispositivo tipo,
        IDispositivoVirtual dispositivo,
        RelojLogico reloj,
        VectorInterrupciones vector,
        Bitacora bitacora,
        Estadisticas estadisticas)
    {
        Tipo = tipo;
        Dispositivo = dispositivo;
        _reloj = reloj;
        _vector = vector;
        _bitacora = bitacora;
        _estadisticas = estadisticas;
    }

    protected abstract bool SoportaOperacion(TipoOperacion op);
    protected virtual CodigoError Validar(SolicitudES s) => CodigoError.Ninguno;

    // Invocado por el Gestor de E/S en la fase de despacho
    public CodigoError Recibir(SolicitudES s)
    {
        if (Dispositivo.Estado == EstadoDispositivo.FueraDeServicio)
            return Rechazar(s, CodigoError.E01_FueraDeServicio);

        if (!SoportaOperacion(s.Operacion))
            return Rechazar(s, CodigoError.E05_OperacionNoSoportada);

        var error = Validar(s);
        if (error != CodigoError.Ninguno)
            return Rechazar(s, error);

        if (Dispositivo.Estado == EstadoDispositivo.Disponible)
        {
            Iniciar(s); // atención inmediata
            return CodigoError.Ninguno;
        }

        if (ColaLocal.EstaLlena)
            return Rechazar(s, CodigoError.E02_ColaLlena);

        s.Estado = EstadoSolicitud.Pendiente;
        ColaLocal.Encolar(s); // espera su turno (FIFO)
        _bitacora.RegistrarEvento($"[{Tipo.ToString().ToUpper()}] Dispositivo ocupado. Solicitud #{s.Id} encolada en cola local ({ColaLocal.Cantidad}/{ColaLocal.Capacidad}).");
        return CodigoError.Ninguno;
    }

    private CodigoError Rechazar(SolicitudES s, CodigoError codigo)
    {
        s.Estado = EstadoSolicitud.Fallida;
        s.Error = codigo;
        s.TickFin = _reloj.Tick;
        return codigo; // el Gestor de E/S lo registra y avisa a la aplicación
    }

    protected void Iniciar(SolicitudES s)
    {
        Actual = s;
        s.Estado = EstadoSolicitud.EnProceso;
        s.TickInicio = _reloj.Tick;
        Dispositivo.Comenzar(s); // carga registros; Estado = Ocupado
        _bitacora.RegistrarEvento($"[{Tipo.ToString().ToUpper()}] Solicitud #{s.Id} ({s.Operacion}) iniciada...");
    }

    public void AvanzarTick()
    {
        // Fase 1 del tick
        if (Actual is null || Dispositivo.Estado == EstadoDispositivo.FueraDeServicio)
            return;

        _estadisticas.RegistrarTickUso(Tipo);
        bool termino = Dispositivo.Avanzar(); // consume un ciclo

        if (Dispositivo.Estado == EstadoDispositivo.FueraDeServicio)
            ManejarFalla(); // E04, E07
        else if (termino)
            _vector.Levantar((int)Tipo); // levanta la IRQ del dispositivo
    }

    private void ManejarFalla()
    {
        if (Actual is null) return;
        var codigo = (CodigoError)Dispositivo.RegistroError;

        if (codigo == CodigoError.E04_ReintentosAgotados)
        {
            Actual.Estado = EstadoSolicitud.Fallida;
            Actual.Error = CodigoError.E04_ReintentosAgotados;
            Actual.TickFin = _reloj.Tick;
            _bitacora.Registrar(Actual, huboInterrupcion: false);
            _estadisticas.RegistrarError(CodigoError.E04_ReintentosAgotados);
            _bitacora.RegistrarEvento($"[ERROR] {Tipo}: Falla de comunicación tras agotar 3 reintentos (E04). Dispositivo Fuera de servicio.");
            Actual = null;
        }
        else if (codigo == CodigoError.E07_SinPapel)
        {
            // En E07 la solicitud queda retenida (en pausa) y se reanuda al ejecutar Reiniciar()
            _estadisticas.RegistrarError(CodigoError.E07_SinPapel);
            _bitacora.RegistrarEvento($"[ERROR] {Tipo}: Sin papel durante la impresión (E07). Solicitud #{Actual.Id} retenida en pausa; dispositivo Fuera de servicio.");
        }
    }

    public void AtenderInterrupcion()
    {
        // ISR (Fase 2 del tick)
        var s = Actual!;
        s.Datos = Dispositivo.RegistroDatos; // lee el registro de datos
        s.Estado = EstadoSolicitud.Finalizada;
        s.TickFin = _reloj.Tick;

        _bitacora.RegistrarEvento($"[INTERRUPCIÓN] {Tipo}: operación completada (IRQ{(int)Tipo}).");
        _bitacora.RegistrarEvento($"[CONTROLADOR] Atendiendo interrupción de {Tipo}...");
        _bitacora.Registrar(s, huboInterrupcion: true);
        _estadisticas.Registrar(s);
        _bitacora.RegistrarEvento($"[SO] Solicitud #{s.Id} finalizada correctamente.");

        Dispositivo.Reiniciar(); // vuelve a Disponible
        Actual = null;

        var siguiente = ColaLocal.Desencolar(); // atiende la siguiente de la cola
        if (siguiente is not null)
            Iniciar(siguiente);
    }

    public virtual void Reiniciar()
    {
        // recuperación tras una falla
        if (Dispositivo.Estado != EstadoDispositivo.FueraDeServicio) return;
        Dispositivo.Reiniciar();
        _bitacora.RegistrarEvento($"[CONTROLADOR] {Tipo} reiniciado -> Disponible.");

        if (Actual is not null)
            Iniciar(Actual); // reanuda la solicitud retenida
        else if (ColaLocal.Desencolar() is { } sig)
            Iniciar(sig);
    }
}
