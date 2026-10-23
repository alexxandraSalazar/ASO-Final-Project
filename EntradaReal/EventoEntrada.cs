using Simulador.Dispositivos;

namespace Simulador.EntradaReal;

// Evento capturado de un dispositivo físico, listo para entregarse al controlador correspondiente
public abstract record EventoEntrada
{
    public sealed record Tecla(char Caracter) : EventoEntrada;
    public sealed record Mouse(EventoMouse Evento) : EventoEntrada;
    public sealed record Movimiento(int X, int Y) : EventoEntrada;
    public sealed record Escape : EventoEntrada;
}
