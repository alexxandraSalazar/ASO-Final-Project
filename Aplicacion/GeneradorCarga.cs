using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Aplicacion;

public sealed record ResultadoCasoPrueba(
    string Id,
    string Nombre,
    bool Exitoso,
    string ResumenObtenido,
    string ResultadoEsperado
);

public static class GeneradorCarga
{
    /// <summary>
    /// Ejecuta el Escenario Integrado de las Tablas 19 y 20 del documento técnico.
    /// </summary>
    public static GestorES EjecutarEscenarioIntegrado(bool mostrarSalida = true)
    {
        var gestor = new GestorES();
        gestor.ControladorTeclado.RecibirPulsacionUsuario('A');

        // En el tick 0 se encolan:
        // 1. Disco -> Lectura
        // 2. Impresora -> Escritura
        // 3. Disco -> Escritura
        // 4. Teclado -> Lectura
        gestor.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 100, procesoId: 1);
        gestor.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, datos: "Informe_ASO.pdf", procesoId: 2);
        gestor.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Escritura, datos: "DATOS_BLOQUE_205", bloque: 205, procesoId: 3);
        gestor.SolicitarES(TipoDispositivo.Teclado, TipoOperacion.Lectura, procesoId: 4);

        if (mostrarSalida)
        {
            Consola.MostrarEncabezado("ESCENARIO INTEGRADO (Tablas 19 y 20 del Documento Técnico)");
            Consola.MostrarColaGeneral(gestor.ColaGeneral, "COLA DE E/S (Tick 0 antes del despacho)");
        }

        while (gestor.TieneTrabajoPendiente())
        {
            gestor.Tick();
        }

        if (mostrarSalida)
        {
            Consola.MostrarEventosRecientes(gestor.Bitacora, 50);
            Consola.MostrarBitacora(gestor.Bitacora);
            Consola.MostrarEstadisticas(gestor);
        }

        return gestor;
    }

    /// <summary>
    /// Simulación de carga aleatoria determinista con semilla fija.
    /// </summary>
    public static GestorES EjecutarCargaAleatoria(int ticksGeneracion = 20, int semilla = 2026, bool mostrarSalida = true)
    {
        var rng = new Random(semilla);
        var gestor = new GestorES();

        if (mostrarSalida)
        {
            Consola.MostrarEncabezado($"SIMULACIÓN DE CARGA ALEATORIA DETERMINISTA (Semilla = {semilla}, Ticks generación = {ticksGeneracion})");
        }

        for (int t = 0; t < ticksGeneracion; t++)
        {
            gestor.Tick(() =>
            {
                // Posibilidad de que el usuario pulse una tecla
                if (rng.NextDouble() < 0.45)
                {
                    char tecla = (char)('A' + rng.Next(0, 26));
                    gestor.ControladorTeclado.RecibirPulsacionUsuario(tecla);
                }

                // Generar entre 0 y 2 solicitudes en este tick
                int cantidad = rng.Next(0, 3);
                for (int i = 0; i < cantidad; i++)
                {
                    var disp = (TipoDispositivo)rng.Next(0, 3);
                    TipoOperacion op = disp switch
                    {
                        TipoDispositivo.Teclado => TipoOperacion.Lectura,
                        TipoDispositivo.Impresora => TipoOperacion.Escritura,
                        _ => rng.Next(0, 2) == 0 ? TipoOperacion.Lectura : TipoOperacion.Escritura
                    };

                    int bloque = disp == TipoDispositivo.Disco ? rng.Next(0, 512) : 0;
                    string datos = op == TipoOperacion.Escritura ? $"Payload_T{gestor.Reloj.Tick}_{i}" : "";
                    gestor.SolicitarES(disp, op, datos, bloque, procesoId: rng.Next(1, 10));
                }
            });
        }

        // Drenar las operaciones pendientes hasta terminar
        int salvaguarda = 200;
        while (gestor.TieneTrabajoPendiente() && salvaguarda-- > 0)
        {
            gestor.Tick();
        }

        if (mostrarSalida)
        {
            Consola.MostrarEventosRecientes(gestor.Bitacora, 35);
            Consola.MostrarBitacora(gestor.Bitacora);
            Consola.MostrarEstadisticas(gestor);
        }

        return gestor;
    }

    /// <summary>
    /// Ejecuta los 16 casos de prueba (CP-001 a CP-016) especificados en las secciones 16 y 17.
    /// </summary>
    public static List<ResultadoCasoPrueba> EjecutarTodosLosCasosDePrueba(bool mostrarDetalle = true)
    {
        var resultados = new List<ResultadoCasoPrueba>
        {
            EjecutarCP001(),
            EjecutarCP002(),
            EjecutarCP003(),
            EjecutarCP004(),
            EjecutarCP005(),
            EjecutarCP006(),
            EjecutarCP007(),
            EjecutarCP008(),
            EjecutarCP009(),
            EjecutarCP010(),
            EjecutarCP011(),
            EjecutarCP012(),
            EjecutarCP013(),
            EjecutarCP014(),
            EjecutarCP015(),
            EjecutarCP016()
        };

        if (mostrarDetalle)
        {
            Consola.MostrarEncabezado("SUITE DE 16 CASOS DE PRUEBA DEL PROYECTO (CP-001 a CP-016)");
            foreach (var r in resultados)
            {
                Console.ForegroundColor = r.Exitoso ? ConsoleColor.Green : ConsoleColor.Red;
                string badge = r.Exitoso ? "[PASÓ]" : "[FALLÓ]";
                Console.Write($"{badge} {r.Id} - {r.Nombre}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine($"       Obtenido : {r.ResumenObtenido}");
                Console.WriteLine($"       Esperado : {r.ResultadoEsperado}");
                Console.WriteLine();
            }

            int pasados = resultados.Count(x => x.Exitoso);
            Console.ForegroundColor = pasados == resultados.Count ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"Resumen: {pasados}/{resultados.Count} casos de prueba superados exitosamente.");
            Console.ResetColor();
        }

        return resultados;
    }

    // CP-001: Petición única a dispositivo inactivo
    private static ResultadoCasoPrueba EjecutarCP001()
    {
        var g = new GestorES();
        var s = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 100, procesoId: 1);
        g.Despachar();
        bool estadoInicialOcupado = g.Disco.Estado == EstadoDispositivo.Ocupado && g.Disco.CiclosRestantes == 10;

        for (int i = 0; i < 4; i++) g.Tick();
        bool enTick4Ocupado = g.Disco.Estado == EstadoDispositivo.Ocupado;

        g.Tick(); // Tick 5 -> pasa a Procesando
        bool enTick5Procesando = g.Disco.Estado == EstadoDispositivo.Procesando;

        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = estadoInicialOcupado && enTick4Ocupado && enTick5Procesando &&
                  s.Estado == EstadoSolicitud.Finalizada && s.TickFin == 10 &&
                  g.Disco.Estado == EstadoDispositivo.Disponible &&
                  g.Estadisticas.InterrupcionesAtendidas == 1;

        return new ResultadoCasoPrueba(
            "CP-001",
            "Petición única a dispositivo inactivo (Disco Lectura Bloque 100)",
            ok,
            $"Estado final={s.Estado}, TickFin={s.TickFin}, IRQ={g.Estadisticas.InterrupcionesAtendidas}, Disco={g.Disco.Estado}",
            "Ocupado (ticks 1-4) -> Procesando (ticks 5-10) -> IRQ1 en t+10, Finalizada y Disco Disponible."
        );
    }

    // CP-002: Encolamiento por ráfaga (Burst) en Impresora
    private static ResultadoCasoPrueba EjecutarCP002()
    {
        var g = new GestorES();
        var a = g.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, "DocA", procesoId: 1);
        var b = g.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, "DocB", procesoId: 2);
        var c = g.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, "DocC", procesoId: 3);
        g.Despachar();

        int tamColaInicial = g.ControladorImpresora.ColaLocal.Cantidad;
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = tamColaInicial == 2 &&
                  a.TickFin == 8 && b.TickFin == 16 && c.TickFin == 24 &&
                  g.Estadisticas.InterrupcionesAtendidas == 3;

        return new ResultadoCasoPrueba(
            "CP-002",
            "Encolamiento por ráfaga (Burst) en Impresora (A, B, C)",
            ok,
            $"Cola inicial={tamColaInicial}, Ticks fin=({a.TickFin}, {b.TickFin}, {c.TickFin}), IRQs={g.Estadisticas.InterrupcionesAtendidas}",
            "A en proceso, B y C en cola local (tamaño 2); finalizan en t+8, t+16 y t+24 con 3 IRQ2."
        );
    }

    // CP-003: Resolución de interrupción y avance de cola
    private static ResultadoCasoPrueba EjecutarCP003()
    {
        var g = new GestorES();
        var a = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 10, procesoId: 1);
        var b = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 20, procesoId: 2);
        var c = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 30, procesoId: 3);
        g.Despachar();

        // Avanzamos 9 ticks (T-1 para finalizar A)
        for (int i = 0; i < 9; i++) g.Tick();

        // Avanzamos 1 tick para que A termine en el tick 10
        g.Tick();

        bool ok = a.Estado == EstadoSolicitud.Finalizada &&
                  g.ControladorDisco.Actual?.Id == b.Id &&
                  b.Estado == EstadoSolicitud.EnProceso &&
                  g.Disco.Estado == EstadoDispositivo.Ocupado &&
                  g.ControladorDisco.ColaLocal.Cantidad == 1 &&
                  g.ControladorDisco.ColaLocal.Ver()?.Id == c.Id;

        return new ResultadoCasoPrueba(
            "CP-003",
            "Resolución de interrupción y avance automático de cola local",
            ok,
            $"A={a.Estado}, Actual=#{g.ControladorDisco.Actual?.Id} ({g.Disco.Estado}), ColaLocal={g.ControladorDisco.ColaLocal.Cantidad} (frente=#{g.ControladorDisco.ColaLocal.Ver()?.Id})",
            "A finaliza por IRQ1; la ISR desencola B inmediatamente (Ocupado) y deja 1 elemento (C) en la cola."
        );
    }

    // CP-004: Interrupciones simultáneas (Teclado y Disco terminan en el mismo tick)
    private static ResultadoCasoPrueba EjecutarCP004()
    {
        var g = new GestorES();
        g.ControladorTeclado.RecibirPulsacionUsuario('Z');

        // Disco dura 10 ticks (inicia en tick 0 -> termina en tick 10)
        g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 50, procesoId: 1);
        g.Despachar();

        // Avanzamos hasta el tick 7
        for (int i = 0; i < 7; i++) g.Tick();

        // En el tick 7 creamos lectura de teclado (dura 3 ticks -> 7 + 3 = 10)
        g.SolicitarES(TipoDispositivo.Teclado, TipoOperacion.Lectura, procesoId: 2);
        g.Despachar();

        // Avanzamos 3 ticks (8, 9, 10)
        for (int i = 0; i < 3; i++) g.Tick();

        var regs = g.Bitacora.Registros;
        bool ok = regs.Count == 2 &&
                  regs[0].Dispositivo == TipoDispositivo.Teclado && regs[0].TickFin == 10 &&
                  regs[1].Dispositivo == TipoDispositivo.Disco && regs[1].TickFin == 10 &&
                  g.Estadisticas.InterrupcionesAtendidas == 2;

        return new ResultadoCasoPrueba(
            "CP-004",
            "Interrupciones simultáneas (IRQ0 Teclado e IRQ1 Disco en el mismo tick)",
            ok,
            $"Atendidas={g.Estadisticas.InterrupcionesAtendidas}; Orden=[1º {regs[0].Dispositivo} (t={regs[0].TickFin}), 2º {regs[1].Dispositivo} (t={regs[1].TickFin})]",
            "Ambas banderas se atienden en el mismo tick por orden de prioridad (primero IRQ0 Teclado, luego IRQ1 Disco)."
        );
    }

    // CP-005: Manejo de hardware fuera de servicio (E01)
    private static ResultadoCasoPrueba EjecutarCP005()
    {
        var g = new GestorES();
        g.Impresora.InyectarFalla(CodigoError.E07_SinPapel); // Impresora Fuera de servicio (sin papel)
        var s = g.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, "Contrato.pdf", procesoId: 4);
        g.Despachar();

        bool ok = s.Estado == EstadoSolicitud.Fallida &&
                  s.Error == CodigoError.E01_FueraDeServicio &&
                  g.Estadisticas.ErroresDetectados == 1 &&
                  g.Estadisticas.InterrupcionesAtendidas == 0;

        return new ResultadoCasoPrueba(
            "CP-005",
            "Manejo de hardware fuera de servicio (Rechazo con E01)",
            ok,
            $"Estado={s.Estado}, Código={s.Error}, Errores={g.Estadisticas.ErroresDetectados}, IRQs={g.Estadisticas.InterrupcionesAtendidas}",
            "Rechazada en despacho con E01, Estado=Fallida, sin interrupción y errores + 1."
        );
    }

    // CP-006: Lectura del teclado (Prueba obligatoria 1)
    private static ResultadoCasoPrueba EjecutarCP006()
    {
        var g = new GestorES();
        g.ControladorTeclado.RecibirPulsacionUsuario('A');
        var s = g.SolicitarES(TipoDispositivo.Teclado, TipoOperacion.Lectura, procesoId: 1);
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = s.Estado == EstadoSolicitud.Finalizada &&
                  s.Datos == "A" &&
                  s.TickFin == 3 &&
                  g.Teclado.Estado == EstadoDispositivo.Disponible;

        return new ResultadoCasoPrueba(
            "CP-006",
            "Lectura del teclado (Prueba 1)",
            ok,
            $"Datos=\"{s.Datos}\", TickFin={s.TickFin}, Estado={s.Estado}, Teclado={g.Teclado.Estado}",
            "Tras 3 ticks levanta IRQ0, RegistroDatos=\"A\", solicitud Finalizada y teclado Disponible."
        );
    }

    // CP-007: Escritura y lectura del disco en bloque 205 (Prueba obligatoria 2)
    private static ResultadoCasoPrueba EjecutarCP007()
    {
        var g = new GestorES();
        var esc = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Escritura, datos: "HOLA", bloque: 205, procesoId: 1);
        var lec = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 205, procesoId: 1);
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = esc.Estado == EstadoSolicitud.Finalizada && esc.TickFin == 10 &&
                  lec.Estado == EstadoSolicitud.Finalizada && lec.TickFin == 20 &&
                  lec.Datos == "HOLA";

        return new ResultadoCasoPrueba(
            "CP-007",
            "Escritura y lectura del disco en bloque 205 (Prueba 2)",
            ok,
            $"Escritura TickFin={esc.TickFin}; Lectura TickFin={lec.TickFin}, DatosLeídos=\"{lec.Datos}\"",
            "Escritura finaliza en t+10 y guarda \"HOLA\" en bloque 205; lectura finaliza en t+20 y devuelve \"HOLA\"."
        );
    }

    // CP-008: Envío de datos a la impresora (Prueba obligatoria 3)
    private static ResultadoCasoPrueba EjecutarCP008()
    {
        var g = new GestorES();
        var s = g.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, datos: "Tesis_Final.pdf", procesoId: 1);
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = s.Estado == EstadoSolicitud.Finalizada &&
                  s.TickFin == 8 &&
                  g.Impresora.Hojas == 19 &&
                  g.Impresora.Estado == EstadoDispositivo.Disponible;

        return new ResultadoCasoPrueba(
            "CP-008",
            "Envío de datos a la impresora (Prueba 3)",
            ok,
            $"Estado={s.Estado}, TickFin={s.TickFin}, Hojas restantes={g.Impresora.Hojas}",
            "Ocupado (2 ticks) -> Procesando -> en t+8 levanta IRQ2, Finalizada y hojas bajan de 20 a 19."
        );
    }

    // CP-009: Bloque inválido (E03)
    private static ResultadoCasoPrueba EjecutarCP009()
    {
        var g = new GestorES();
        var sInvalida = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 2048, procesoId: 1);
        var sValida = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 10, procesoId: 2);
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = sInvalida.Estado == EstadoSolicitud.Fallida &&
                  sInvalida.Error == CodigoError.E03_BloqueInvalido &&
                  sValida.Estado == EstadoSolicitud.Finalizada &&
                  g.Estadisticas.ErroresDetectados == 1;

        return new ResultadoCasoPrueba(
            "CP-009",
            "Bloque inválido en Disco (Bloque 2048 -> E03)",
            ok,
            $"Solicitud 1={sInvalida.Estado} ({sInvalida.Error}), Solicitud 2={sValida.Estado}, Errores={g.Estadisticas.ErroresDetectados}",
            "Se rechaza de inmediato con E03, errores + 1, y la siguiente petición válida se atiende con normalidad."
        );
    }

    // CP-010: Cola local llena (E02)
    private static ResultadoCasoPrueba EjecutarCP010()
    {
        var g = new GestorES();
        var creadas = new List<SolicitudES>();
        for (int i = 0; i < 7; i++)
        {
            creadas.Add(g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: i, procesoId: i + 1));
        }
        g.Despachar();

        var septima = creadas[6];
        bool rechazoSeptima = septima.Estado == EstadoSolicitud.Fallida &&
                              septima.Error == CodigoError.E02_ColaLlena &&
                              g.ControladorDisco.ColaLocal.Cantidad == 5 &&
                              g.ControladorDisco.Actual?.Id == creadas[0].Id;

        while (g.TieneTrabajoPendiente()) g.Tick();

        bool seisCompletadas = creadas.Take(6).All(x => x.Estado == EstadoSolicitud.Finalizada);
        bool ok = rechazoSeptima && seisCompletadas && g.Estadisticas.ErroresDetectados == 1;

        return new ResultadoCasoPrueba(
            "CP-010",
            "Cola local llena en Disco (Capacidad 5 -> 7ª rechazada con E02)",
            ok,
            $"7ª Solicitud={septima.Estado} ({septima.Error}), Primeras 6 finalizadas={seisCompletadas}",
            "1 en curso y 5 en cola se conservan; la 7ª se rechaza con E02 (Fallida) y se registra en bitácora."
        );
    }

    // CP-011: Operación no soportada (E05)
    private static ResultadoCasoPrueba EjecutarCP011()
    {
        var g = new GestorES();
        var s = g.SolicitarES(TipoDispositivo.Teclado, TipoOperacion.Escritura, datos: "Invalido", procesoId: 1);
        g.Despachar();

        bool ok = s.Estado == EstadoSolicitud.Fallida &&
                  s.Error == CodigoError.E05_OperacionNoSoportada &&
                  g.Teclado.Estado == EstadoDispositivo.Disponible &&
                  g.Estadisticas.InterrupcionesAtendidas == 0;

        return new ResultadoCasoPrueba(
            "CP-011",
            "Operación no soportada (Escritura en Teclado -> E05)",
            ok,
            $"Estado={s.Estado}, Error={s.Error}, Teclado={g.Teclado.Estado}, IRQs={g.Estadisticas.InterrupcionesAtendidas}",
            "Rechazada con E05 en validación; el teclado permanece Disponible y no genera interrupción."
        );
    }

    // CP-012: Falla de comunicación recuperable (2 fallos <= 3 reintentos)
    private static ResultadoCasoPrueba EjecutarCP012()
    {
        var g = new GestorES();
        g.Disco.InyectarFalla(CodigoError.E04_ReintentosAgotados, repeticiones: 2);
        var s = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 10, procesoId: 1);

        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = s.Estado == EstadoSolicitud.Finalizada &&
                  s.TickFin == 12 &&
                  g.Disco.Estado == EstadoDispositivo.Disponible &&
                  g.Estadisticas.ErroresDetectados == 0;

        return new ResultadoCasoPrueba(
            "CP-012",
            "Falla de comunicación recuperable en Disco (2 reintentos <= 3)",
            ok,
            $"Estado={s.Estado}, TickFin={s.TickFin}, Disco={g.Disco.Estado}, Errores={g.Estadisticas.ErroresDetectados}",
            "Se pierden 2 ticks por reintentos y la lectura finaliza en t+12 sin pasar a Fuera de servicio."
        );
    }

    // CP-013: Reintentos agotados (4 fallos > 3) y reinicio
    private static ResultadoCasoPrueba EjecutarCP013()
    {
        var g = new GestorES();
        g.Disco.InyectarFalla(CodigoError.E04_ReintentosAgotados, repeticiones: 4);
        var s1 = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 10, procesoId: 1);

        for (int i = 0; i < 4; i++) g.Tick();

        bool fueraServicioEn4 = g.Disco.Estado == EstadoDispositivo.FueraDeServicio &&
                                s1.Estado == EstadoSolicitud.Fallida &&
                                s1.Error == CodigoError.E04_ReintentosAgotados;

        // Solicitud posterior mientras está Fuera de servicio -> E01
        var s2 = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 11, procesoId: 2);
        g.Despachar();
        bool rechazadaConE01 = s2.Estado == EstadoSolicitud.Fallida && s2.Error == CodigoError.E01_FueraDeServicio;

        // Reiniciar controlador y enviar nueva solicitud exitosa
        g.ControladorDisco.Reiniciar();
        var s3 = g.SolicitarES(TipoDispositivo.Disco, TipoOperacion.Lectura, bloque: 12, procesoId: 3);
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = fueraServicioEn4 && rechazadaConE01 &&
                  s3.Estado == EstadoSolicitud.Finalizada &&
                  g.Disco.Estado == EstadoDispositivo.Disponible;

        return new ResultadoCasoPrueba(
            "CP-013",
            "Reintentos agotados (4 fallos -> E04), rechazo E01 y recuperación con Reiniciar()",
            ok,
            $"s1={s1.Error}, s2={s2.Error}, tras Reiniciar() s3={s3.Estado}",
            "Al 4º fallo pasa a Fuera de servicio (E04); rechaza nuevas con E01; tras Reiniciar() vuelve a Disponible."
        );
    }

    // CP-014: Sin papel durante la impresión (E07) y reanudación tras Reiniciar()
    private static ResultadoCasoPrueba EjecutarCP014()
    {
        var g = new GestorES();
        var s1 = g.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, datos: "Libro.pdf", procesoId: 1);
        g.Despachar();
        g.Impresora.InyectarFalla(CodigoError.E07_SinPapel);

        // Avanzamos 3 ticks (2 de arranque + 1 donde inicia transferencia y detecta falta de papel)
        for (int i = 0; i < 3; i++) g.Tick();

        bool retenidaEnFalla = g.Impresora.Estado == EstadoDispositivo.FueraDeServicio &&
                               s1.Estado == EstadoSolicitud.EnProceso &&
                               g.ControladorImpresora.Actual?.Id == s1.Id;

        // Nueva solicitud mientras está sin papel se rechaza con E01
        var sNueva = g.SolicitarES(TipoDispositivo.Impresora, TipoOperacion.Escritura, datos: "Extra.pdf", procesoId: 2);
        g.Despachar();
        bool rechazoNueva = sNueva.Estado == EstadoSolicitud.Fallida && sNueva.Error == CodigoError.E01_FueraDeServicio;

        // Recargar papel con Reiniciar() y completar los 8 ticks
        int tickReinicio = g.Reloj.Tick;
        g.ControladorImpresora.Reiniciar();
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = retenidaEnFalla && rechazoNueva &&
                  s1.Estado == EstadoSolicitud.Finalizada &&
                  (s1.TickFin - tickReinicio) == 8 &&
                  g.Impresora.Hojas == 19;

        return new ResultadoCasoPrueba(
            "CP-014",
            "Sin papel durante impresión (E07), solicitud retenida y reanudación con Reiniciar()",
            ok,
            $"Retenida={retenidaEnFalla}, NuevaRechazada={ sNueva.Error}, s1 Finalizada en +{s1.TickFin - tickReinicio} ticks tras reinicio",
            "En transferencia pasa a Fuera de servicio (E07) con solicitud retenida; tras Reiniciar() recarga papel y finaliza en 8 ticks."
        );
    }

    // CP-015: Desbordamiento del buffer del teclado (E06)
    private static ResultadoCasoPrueba EjecutarCP015()
    {
        var g = new GestorES();
        for (int i = 0; i < 16; i++)
        {
            g.ControladorTeclado.RecibirPulsacionUsuario((char)('A' + i));
        }

        // 17ª pulsación -> overflow E06
        bool acepto17 = g.ControladorTeclado.RecibirPulsacionUsuario('Z');
        bool estadoErrorE06 = !acepto17 &&
                              g.Teclado.RegistroError == (int)CodigoError.E06_BufferTecladoLleno &&
                              g.Teclado.Estado == EstadoDispositivo.Disponible &&
                              g.Estadisticas.ErroresDetectados == 1;

        // Verificar que la primera pulsación entregada sigue siendo 'A'
        var sLectura = g.SolicitarES(TipoDispositivo.Teclado, TipoOperacion.Lectura, procesoId: 1);
        while (g.TieneTrabajoPendiente()) g.Tick();

        bool ok = estadoErrorE06 && sLectura.Datos == "A" && g.Teclado.PulsacionesEnBuffer == 15;

        return new ResultadoCasoPrueba(
            "CP-015",
            "Desbordamiento del buffer del teclado (17ª pulsación -> E06)",
            ok,
            $"17ª aceptada={acepto17}, RegError=E0{g.Teclado.RegistroError}, 1ª tecla leída=\"{sLectura.Datos}\", Restantes={g.Teclado.PulsacionesEnBuffer}",
            "La pulsación 17 se descarta con E06; las 16 almacenadas no se alteran y se leen en orden FIFO."
        );
    }

    // CP-016: Consistencia de las estadísticas y determinismo con semilla fija
    private static ResultadoCasoPrueba EjecutarCP016()
    {
        var g1 = EjecutarCargaAleatoria(ticksGeneracion: 25, semilla: 42, mostrarSalida: false);
        var g2 = EjecutarCargaAleatoria(ticksGeneracion: 25, semilla: 42, mostrarSalida: false);

        bool bitacorasIdenticas = g1.Bitacora.Registros.Count == g2.Bitacora.Registros.Count &&
                                  g1.Bitacora.Registros.Zip(g2.Bitacora.Registros)
                                    .All(p => p.First.SolicitudId == p.Second.SolicitudId &&
                                              p.First.TickFin == p.Second.TickFin &&
                                              p.First.Estado == p.Second.Estado);

        var e = g1.Estadisticas;
        int totalContabilizado = e.SolicitudesFinalizadas + e.SolicitudesFallidas +
                                 g1.ContarSolicitudesPendientes() + g1.ContarSolicitudesEnProceso();

        bool invariantes = e.SolicitudesCreadas == totalContabilizado &&
                           e.InterrupcionesAtendidas == e.SolicitudesFinalizadas;

        bool ok = bitacorasIdenticas && invariantes;

        return new ResultadoCasoPrueba(
            "CP-016",
            "Consistencia de estadísticas y determinismo con semilla fija",
            ok,
            $"Determinista={bitacorasIdenticas}, Creadas={e.SolicitudesCreadas} == Contabilizadas={totalContabilizado}, IRQs={e.InterrupcionesAtendidas} == Finalizadas={e.SolicitudesFinalizadas}",
            "Dos ejecuciones con igual semilla producen bitácoras idénticas; Creadas = Finalizadas+Fallidas+Pendientes+EnProceso e IRQs = Finalizadas."
        );
    }
}
