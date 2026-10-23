namespace Simulador.Dispositivos;

// Tipos de evento que el mouse deposita en su buffer
public enum TipoEventoMouse
{
    ClicIzq,
    ClicDer,
    ClicCentral,
    Rueda
}

// Evento del mouse: coordenadas de pantalla y desplazamiento de la rueda (±120 por muesca)
public readonly record struct EventoMouse(TipoEventoMouse Tipo, int X, int Y, int Delta = 0)
{
    public override string ToString() => Tipo == TipoEventoMouse.Rueda
        ? $"Rueda {(Delta > 0 ? "arriba" : "abajo")} ({X},{Y})"
        : $"{Tipo} ({X},{Y})";
}
