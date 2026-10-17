using System.Text;
using Simulador.Controladores;
using Simulador.Dispositivos;
using Simulador.Modelo;
using Simulador.Nucleo;

namespace Simulador.Aplicacion;

public static class Programa
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Modo no interactivo por línea de comandos (ej. dotnet run -- --all)
        if (args.Contains("--all") || args.Contains("--demo"))
        {
            GeneradorCarga.EjecutarEscenarioIntegrado(mostrarSalida: true);
            GeneradorCarga.EjecutarTodosLosCasosDePrueba(mostrarDetalle: true);
            GeneradorCarga.EjecutarCargaAleatoria(ticksGeneracion: 15, semilla: 2026, mostrarSalida: true);
            return;
        }

        EjecutarMenuInteractivo();
    }

    private static void EjecutarMenuInteractivo()
    {
        var gestor = new GestorES();

        while (true)
        {
            Consola.MostrarEncabezado("SIMULADOR DEL SUBSISTEMA DE ENTRADA/SALIDA (E/S) - UNI (Grupo 4S1-COM)");
            Console.WriteLine("  1. Ejecutar Escenario Integrado del Proyecto (Tablas 19 y 20)");
            Console.WriteLine("  2. Ejecutar los 16 Casos de Prueba (CP-001 a CP-016) y Demostraciones 1 a 6");
            Console.WriteLine("  3. Ejecutar Simulación con Carga Aleatoria Determinista (Semilla fija)");
            Console.WriteLine("  4. Laboratorio Interactivo Paso a Paso (Crear solicitudes, Avanzar ticks, Fallas)");
            Console.WriteLine("  0. Salir");
            Console.Write("Seleccione una opción: ");

            string? opcion = Console.ReadLine()?.Trim();
            switch (opcion)
            {
                case "1":
                    GeneradorCarga.EjecutarEscenarioIntegrado(mostrarSalida: true);
                    Pausa();
                    break;
                case "2":
                    GeneradorCarga.EjecutarTodosLosCasosDePrueba(mostrarDetalle: true);
                    Pausa();
                    break;
                case "3":
                    GeneradorCarga.EjecutarCargaAleatoria(ticksGeneracion: 15, semilla: 2026, mostrarSalida: true);
                    Pausa();
                    break;
                case "4":
                    MenuLaboratorioInteractivo(gestor);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }

    private static void MenuLaboratorioInteractivo(GestorES gestor)
    {
        while (true)
        {
            Consola.MostrarEncabezado($"LABORATORIO INTERACTIVO DE E/S (Tick actual: {gestor.Reloj.Tick})");
            Consola.MostrarEstadoSistema(gestor);
            Consola.MostrarColaGeneral(gestor.ColaGeneral);

            Console.WriteLine("Acciones disponibles:");
            Console.WriteLine("  [1] Pulsar tecla en Teclado Virtual (usuario simulado)");
            Console.WriteLine("  [2] Solicitar E/S al Teclado (Lectura / ConsultarEstado)");
            Console.WriteLine("  [3] Solicitar E/S al Disco (Lectura / Escritura / ConsultarEstado)");
            Console.WriteLine("  [4] Solicitar E/S a la Impresora (Escritura / ConsultarEstado)");
            Console.WriteLine("  [5] Avanzar 1 Tick del Reloj Lógico");
            Console.WriteLine("  [6] Avanzar N Ticks (o hasta vaciar colas)");
            Console.WriteLine("  [7] Inyectar Falla en un Dispositivo (E01, E04, E07)");
            Console.WriteLine("  [8] Reiniciar Dispositivo (Reiniciar() / Recargar Papel)");
            Console.WriteLine("  [9] Ver Bitácora, Eventos y Estadísticas");
            Console.WriteLine("  [R] Reiniciar Simulador desde Tick 0");
            Console.WriteLine("  [0] Volver al menú principal");
            Console.Write("Opción: ");

            string? op = Console.ReadLine()?.Trim().ToUpperInvariant();
            switch (op)
            {
                case "1":
                    Console.Write("Ingrese el carácter a pulsar: ");
                    string? t = Console.ReadLine();
                    char c = !string.IsNullOrEmpty(t) ? t[0] : 'A';
                    gestor.ControladorTeclado.RecibirPulsacionUsuario(c);
                    Consola.MostrarEventosRecientes(gestor.Bitacora, 5);
                    break;

                case "2":
                    Console.Write("Operación ([1] Lectura, [2] Escritura (provoca E05), [3] ConsultarEstado): ");
                    var opTec = LeerOperacion(Console.ReadLine());
                    gestor.SolicitarES(TipoDispositivo.Teclado, opTec, procesoId: 1);
                    gestor.Despachar();
                    Consola.MostrarEventosRecientes(gestor.Bitacora, 5);
                    break;

                case "3":
                    Console.Write("Operación ([1] Lectura, [2] Escritura, [3] ConsultarEstado): ");
                    var opDis = LeerOperacion(Console.ReadLine());
                    int blq = 0;
                    string datosDis = "";
                    if (opDis != TipoOperacion.ConsultarEstado)
                    {
                        Console.Write("Número de bloque (0-1023, u otro para provocar E03): ");
                        int.TryParse(Console.ReadLine(), out blq);
                    }
                    if (opDis == TipoOperacion.Escritura)
                    {
                        Console.Write("Datos a escribir: ");
                        datosDis = Console.ReadLine() ?? "DATOS";
                    }
                    gestor.SolicitarES(TipoDispositivo.Disco, opDis, datosDis, blq, procesoId: 1);
                    gestor.Despachar();
                    Consola.MostrarEventosRecientes(gestor.Bitacora, 5);
                    break;

                case "4":
                    Console.Write("Operación ([1] Lectura (provoca E05), [2] Escritura, [3] ConsultarEstado): ");
                    var opImp = LeerOperacion(Console.ReadLine());
                    string doc = "";
                    if (opImp == TipoOperacion.Escritura)
                    {
                        Console.Write("Documento a imprimir: ");
                        doc = Console.ReadLine() ?? "Documento.txt";
                    }
                    gestor.SolicitarES(TipoDispositivo.Impresora, opImp, doc, procesoId: 1);
                    gestor.Despachar();
                    Consola.MostrarEventosRecientes(gestor.Bitacora, 5);
                    break;

                case "5":
                    gestor.Tick();
                    Consola.MostrarEventosRecientes(gestor.Bitacora, 8);
                    break;

                case "6":
                    Console.Write("Número de ticks a avanzar (Enter para completar todo el trabajo pendiente): ");
                    string? nStr = Console.ReadLine()?.Trim();
                    if (int.TryParse(nStr, out int nTicks) && nTicks > 0)
                    {
                        for (int i = 0; i < nTicks; i++) gestor.Tick();
                    }
                    else
                    {
                        int limite = 200;
                        while (gestor.TieneTrabajoPendiente() && limite-- > 0) gestor.Tick();
                    }
                    Consola.MostrarEventosRecientes(gestor.Bitacora, 15);
                    break;

                case "7":
                    InyectarFallaInteractiva(gestor);
                    break;

                case "8":
                    Console.Write("Dispositivo a reiniciar ([0] Teclado, [1] Disco, [2] Impresora): ");
                    if (int.TryParse(Console.ReadLine(), out int idx) && idx >= 0 && idx <= 2)
                    {
                        gestor.ObtenerControlador((TipoDispositivo)idx).Reiniciar();
                        Consola.MostrarEventosRecientes(gestor.Bitacora, 5);
                    }
                    break;

                case "9":
                    Consola.MostrarEventosRecientes(gestor.Bitacora, 30);
                    Consola.MostrarBitacora(gestor.Bitacora);
                    Consola.MostrarEstadisticas(gestor);
                    Pausa();
                    break;

                case "R":
                    gestor = new GestorES();
                    Console.WriteLine("Simulador reiniciado.");
                    break;

                case "0":
                    return;
            }
        }
    }

    private static TipoOperacion LeerOperacion(string? entrada) => entrada?.Trim() switch
    {
        "1" => TipoOperacion.Lectura,
        "2" => TipoOperacion.Escritura,
        "3" => TipoOperacion.ConsultarEstado,
        _ => TipoOperacion.Lectura
    };

    private static void InyectarFallaInteractiva(GestorES gestor)
    {
        Console.Write("Dispositivo ([0] Teclado, [1] Disco, [2] Impresora): ");
        if (!int.TryParse(Console.ReadLine(), out int dIdx) || dIdx < 0 || dIdx > 2) return;
        var disp = gestor.ObtenerControlador((TipoDispositivo)dIdx).Dispositivo;

        Console.WriteLine("Tipo de falla:");
        Console.WriteLine("  [1] E01 - Poner dispositivo Fuera de servicio inmediatamente");
        Console.WriteLine("  [2] E04 - Inyectar fallos de comunicación consecutivos en el arranque");
        Console.WriteLine("  [3] E07 - Sin papel en Impresora");
        Console.Write("Seleccione: ");
        string? fOp = Console.ReadLine()?.Trim();

        if (fOp == "1")
        {
            disp.InyectarFalla(CodigoError.E01_FueraDeServicio);
            gestor.Bitacora.RegistrarEvento($"[PRUEBA] Falla E01 inyectada en {(TipoDispositivo)dIdx}.");
        }
        else if (fOp == "2")
        {
            Console.Write("Cantidad de fallos consecutivos (<=3 recuperable, >=4 agota reintentos E04): ");
            int.TryParse(Console.ReadLine(), out int rep);
            disp.InyectarFalla(CodigoError.E04_ReintentosAgotados, Math.Max(1, rep));
            gestor.Bitacora.RegistrarEvento($"[PRUEBA] {Math.Max(1, rep)} fallo(s) de comunicación inyectados en {(TipoDispositivo)dIdx}.");
        }
        else if (fOp == "3")
        {
            gestor.Impresora.InyectarFalla(CodigoError.E07_SinPapel);
            gestor.Bitacora.RegistrarEvento("[PRUEBA] Falla E07 (Sin papel) inyectada en Impresora.");
        }
    }

    private static void Pausa()
    {
        Console.WriteLine("Presione Enter para continuar...");
        Console.ReadLine();
    }
}
