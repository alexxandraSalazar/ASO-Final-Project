# Simulador del Subsistema de Entrada/Salida (E/S) en C#

**Universidad Nacional de Ingeniería (UNI)**  
**Recinto Universitario Simón Bolívar — DACTIC**  
**Asignatura:** Arquitectura de Sistemas Operativos  
**Grupo:** 4S1-COM  
**Autores:**

* David Gabriel Álvarez Delgadillo (2023-0847U)
* Alexa Nicolle Reynosa Velásquez (2022-0883U)
* Alexandra Daniela Rodríguez Salazar (2023-0613U)

\---

## Arquitectura del Proyecto

El proyecto implementa fielmente el diseño en capas y espacios de nombre:

* **`Simulador.Modelo`**

  * `Enumeraciones.cs`: `EstadoDispositivo`, `TipoOperacion`, `EstadoSolicitud`, `TipoDispositivo`, `CodigoError` (`E01` a `E07`).
  * `SolicitudES.cs`: Paquete de solicitud de E/S (IRP) con ciclo de vida (`Pendiente`, `EnProceso`, `Finalizada`, `Fallida`) y marcas de tiempo (`TickCreacion`, `TickInicio`, `TickFin`).
* **`Simulador.Dispositivos`**

  * `IDispositivoVirtual.cs`: Interfaz común con registros de `Estado`, `RegistroControl`, `RegistroError`, `RegistroDatos` y `CiclosRestantes`.
  * `TecladoVirtual.cs`: Buffer circular de 16 pulsaciones, 1 ciclo de arranque, 3 ciclos totales, detección de desbordamiento (`E06\_BufferTecladoLleno`).
  * `DiscoVirtual.cs`: 1024 bloques direccionables (0 a 1023), 4 ciclos de arranque, 10 ciclos totales, simulación de fallos de comunicación y hasta 3 reintentos (`E04\_ReintentosAgotados`).
  * `MouseVirtual.cs` / `EventoMouse.cs`: Buffer de 16 eventos (clics izquierdo/derecho/central y rueda), registro de posición del puntero, 1 ciclo de arranque, 2 ciclos totales, detección de desbordamiento (`E08\_BufferMouseLleno`).
  * `ImpresoraVirtual.cs`: 20 hojas iniciales configurables, 2 ciclos de arranque, 8 ciclos totales, control de falta de papel (`E07\_SinPapel`) y recarga.
* **`Simulador.Controladores`**

  * `ColaSolicitudes.cs`: Cola FIFO genérica $O(1)$ con control de capacidad (ilimitada para la cola general; capacidad de 5 para cada cola local).
  * `ControladorDispositivo.cs`: Clase abstracta con validación, despacho, manejo de fallas, inicio de operaciones y Rutina de Servicio de Interrupción (`AtenderInterrupcion()`).
  * `ControladorTeclado.cs`, `ControladorDisco.cs`, `ControladorImpresora.cs`, `ControladorMouse.cs`: Reglas específicas de validación (`E03`, `E05`) y recuperación (`Reiniciar()`).
* **`Simulador.Nucleo`**

  * `GestorES.cs`: Llamada al sistema `SolicitarES()`, despachador (`Despachar()`) y ciclo de reloj (`Tick()`).
  * `VectorInterrupciones.cs`: Tabla de vectores (`Action?\[]`) y banderas (`IRQ0` Teclado, `IRQ1` Disco, `IRQ2` Impresora, `IRQ3` Mouse) con prioridad fija de menor a mayor IRQ.
  * `RelojLogico.cs`, `Bitacora.cs`, `Estadisticas.cs`: Trazabilidad completa y métricas de rendimiento.
* **`Simulador.EntradaReal`** (solo Windows)

  * `CapturaEntradaGlobal.cs`: Captura **global** del teclado y mouse físicos mediante hooks de bajo nivel (`WH\_KEYBOARD\_LL` / `WH\_MOUSE\_LL`) en un hilo dedicado con bucle de mensajes. Los eventos se depositan en una cola concurrente que el hilo principal entrega a los controladores. La entrada no se bloquea: las demás aplicaciones la siguen recibiendo.
  * `Win32.cs`: Declaraciones P/Invoke de `user32` / `kernel32`.
  * `EventoEntrada.cs`: Eventos capturados (`Tecla`, `Mouse`, `Movimiento`, `Escape`).
* **`Simulador.Aplicacion`**

  * `Programa.cs`: Menú interactivo y modo de demostración automática (`--all`).
  * `GeneradorCarga.cs`: Implementación del Escenario Integrado (Tablas 19 y 20), los **16 Casos de Prueba (`CP-001` a `CP-016`)** y el generador de carga aleatoria determinista con semilla fija.
  * `Consola.cs`: Presentación formateada de colas, registros, trazas de interrupción, bitácora y estadísticas.

\---

## Requisitos y Ejecución

1. Tener instalado **.NET 8.0 SDK** (o superior).
2. En la carpeta del proyecto (`SimuladorES`), ejecutar:

```bash
# Modo interactivo con menú completo y laboratorio paso a paso:
dotnet run

# Ejecutar automáticamente el Escenario Integrado + los 16 Casos de Prueba + Carga Aleatoria:
dotnet run -- --all
```

### Modo Entrada Real (teclado y mouse físicos)

En el Laboratorio Interactivo (opción `4` del menú) elija **`[E] Modo Entrada Real`**:

* Se pide el intervalo del reloj lógico en milisegundos (por defecto 250) y si se debe crear automáticamente una solicitud de `Lectura` por cada evento.
* Cada tecla pulsada (en cualquier ventana) entra al buffer del Teclado (IRQ0) y cada clic o giro de rueda entra al buffer del Mouse (IRQ3); el movimiento actualiza la posición del puntero.
* Sin lectura automática, los buffers se llenan y se pueden observar los errores `E06` (teclado) y `E08` (mouse).
* Presione **ESC** para salir; los hooks se liberan al terminar.

Los casos de prueba, el escenario integrado y la carga aleatoria siguen usando la entrada simulada para mantener resultados deterministas.
