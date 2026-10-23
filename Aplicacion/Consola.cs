using Simulador.Controladores;
using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Aplicacion;

public static class Consola
{
    public static void MostrarEncabezado(string titulo)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('═', 78));
        Console.WriteLine($"  {titulo}");
        Console.WriteLine(new string('═', 78));
        Console.ResetColor();
    }

    public static void MostrarColaGeneral(ColaSolicitudes cola, string titulo = "COLA DE E/S")
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{titulo}");
        Console.ResetColor();

        var items = cola.Instantanea();
        if (items.Count == 0)
        {
            Console.WriteLine("  (vacía)");
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var s = items[i];
            Console.WriteLine($"  {i + 1}. {s.Dispositivo} -> {s.Operacion} (Solicitud #{s.Id}, Proceso P{s.ProcesoId})");
        }
    }

    public static void MostrarEstadoSistema(GestorES gestor)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"--- ESTADO DEL SISTEMA (Tick actual: {gestor.Reloj.Tick}) ---");
        Console.ResetColor();

        Console.WriteLine($"{"Dispositivo",-12} | {"Estado",-17} | {"Ctrl",-4} | {"Err",-3} | {"Ciclos",-6} | {"Solicitud Actual",-24} | {"Cola Local",-10} | {"Recurso"}");
        Console.WriteLine(new string('-', 105));

        foreach (var ctrl in gestor.Controladores)
        {
            var d = ctrl.Dispositivo;
            string actualStr = ctrl.Actual is not null
                ? $"#{ctrl.Actual.Id} ({ctrl.Actual.Operacion})"
                : "-";
            string colaStr = $"{ctrl.ColaLocal.Cantidad}/{ctrl.ColaLocal.Capacidad}";
            string recurso = ctrl.Tipo switch
            {
                TipoDispositivo.Teclado => $"Buffer: {((TecladoVirtual)d).PulsacionesEnBuffer}/16",
                TipoDispositivo.Disco => "1024 bloques",
                TipoDispositivo.Impresora => $"Papel: {((ImpresoraVirtual)d).Hojas} hojas",
                TipoDispositivo.Mouse => $"Pos: ({((MouseVirtual)d).PosicionX},{((MouseVirtual)d).PosicionY}) Buffer: {((MouseVirtual)d).EventosEnBuffer}/16",
                _ => ""
            };

            Console.WriteLine($"{ctrl.Tipo,-12} | {d.Estado,-17} | {d.RegistroControl,4} | {d.RegistroError,3} | {d.CiclosRestantes,6} | {actualStr,-24} | {colaStr,-10} | {recurso}");
        }
    }

    public static void MostrarBitacora(Bitacora bitacora)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("--- BITÁCORA DE OPERACIONES ---");
        Console.ResetColor();

        if (bitacora.Registros.Count == 0)
        {
            Console.WriteLine("  No hay operaciones registradas en la bitácora.");
            return;
        }

        Console.WriteLine($"{"No.",-4} | {"Solicitud",-9} | {"Dispositivo",-11} | {"Operación",-15} | {"Estado",-10} | {"Interrupción",-12} | {"Tick Fin",-8} | {"Error / Datos"}");
        Console.WriteLine(new string('-', 98));

        foreach (var r in bitacora.Registros)
        {
            string irq = r.HuboInterrupcion ? "Sí" : "No";
            string detalle = r.Error != CodigoError.Ninguno
                ? r.Error.ToString()
                : (!string.IsNullOrEmpty(r.Datos) ? $"\"{r.Datos}\"" : "-");

            Console.WriteLine($"{r.Numero,-4} | #{r.SolicitudId,-8} | {r.Dispositivo,-11} | {r.Operacion,-15} | {r.Estado,-10} | {irq,-12} | {r.TickFin,8} | {detalle}");
        }
    }

    public static void MostrarEventosRecientes(Bitacora bitacora, int ultimos = 25)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("--- TRAZA DE EVENTOS ---");
        Console.ResetColor();

        var lista = bitacora.Eventos.TakeLast(ultimos).ToList();
        if (lista.Count == 0)
        {
            Console.WriteLine("  (sin eventos)");
            return;
        }

        foreach (var ev in lista)
            EscribirEvento(ev);
    }

    /// <summary>
    /// Imprime solo los eventos registrados a partir del índice indicado (traza en vivo).
    /// Devuelve el nuevo índice para la siguiente llamada.
    /// </summary>
    public static int MostrarEventosDesde(Bitacora bitacora, int desde)
    {
        for (int i = desde; i < bitacora.Eventos.Count; i++)
            EscribirEvento(bitacora.Eventos[i]);
        return bitacora.Eventos.Count;
    }

    private static void EscribirEvento(string ev)
    {
        if (ev.Contains("[ERROR]") || ev.Contains("[WARNING]") || ev.Contains("[BEEP"))
            Console.ForegroundColor = ConsoleColor.Red;
        else if (ev.Contains("[INTERRUPCIÓN]"))
            Console.ForegroundColor = ConsoleColor.Yellow;
        else if (ev.Contains("[MOUSE]") || ev.Contains("[ENTRADA REAL]"))
            Console.ForegroundColor = ConsoleColor.Blue;
        else if (ev.Contains("[SO]") && ev.Contains("finalizada"))
            Console.ForegroundColor = ConsoleColor.Green;
        else
            Console.ResetColor();

        Console.WriteLine("  " + ev);
        Console.ResetColor();
    }

    public static void MostrarEstadisticas(GestorES gestor)
    {
        var est = gestor.Estadisticas;
        var masUsado = est.ObtenerDispositivoMasUtilizado();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- ESTADÍSTICAS DE LA SIMULACIÓN ---");
        Console.ResetColor();

        Console.WriteLine($"  Solicitudes creadas           : {est.SolicitudesCreadas}");
        Console.WriteLine($"  Operaciones realizadas        : {est.SolicitudesFinalizadas}");
        Console.WriteLine($"  Solicitudes pendientes        : {gestor.ContarSolicitudesPendientes()}");
        Console.WriteLine($"  Solicitudes en proceso        : {gestor.ContarSolicitudesEnProceso()}");
        Console.WriteLine($"  Solicitudes finalizadas       : {est.SolicitudesFinalizadas}");
        Console.WriteLine($"  Solicitudes fallidas          : {est.SolicitudesFallidas}");
        Console.WriteLine($"  Interrupciones atendidas      : {est.InterrupcionesAtendidas}");
        Console.WriteLine($"  Errores detectados            : {est.ErroresDetectados}");
        Console.WriteLine($"  Tiempo promedio de atención   : {est.TiempoPromedioAtencion:F2} ticks");
        Console.WriteLine($"  Dispositivo más utilizado     : {masUsado.Dispositivo} ({masUsado.Operaciones} operaciones; {masUsado.Ticks} ticks en operación)");
    }
}
