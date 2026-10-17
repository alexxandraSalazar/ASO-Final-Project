namespace Simulador.Nucleo;

public sealed class RelojLogico
{
    public int Tick { get; private set; }

    public void Avanzar() => Tick++;
    public void Reiniciar() => Tick = 0;
}
