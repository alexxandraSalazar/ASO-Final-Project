namespace Simulador.Nucleo;

public sealed class VectorInterrupciones
{
    // Vector de interrupciones: 0 = Teclado, 1 = Disco, 2 = Impresora
    public const int IrqTeclado = 0, IrqDisco = 1, IrqImpresora = 2;

    private readonly bool[] _pendientes = new bool[3];
    private readonly Action?[] _tablaIsr = new Action?[3];

    public void Registrar(int irq, Action isr) => _tablaIsr[irq] = isr;

    // Lo invoca el controlador cuando su dispositivo termina una operación
    public void Levantar(int irq) => _pendientes[irq] = true;

    public bool EstaPendiente(int irq) => irq >= 0 && irq < _pendientes.Length && _pendientes[irq];

    // Se invoca una vez por tick (Fase 2). Prioridad: menor número de IRQ primero.
    public int Atender()
    {
        int atendidas = 0;
        for (int irq = 0; irq < _pendientes.Length; irq++)
        {
            if (!_pendientes[irq]) continue;
            _pendientes[irq] = false; // acuse: se baja la bandera
            _tablaIsr[irq]?.Invoke();  // ejecuta la ISR del controlador
            atendidas++;
        }
        return atendidas;
    }

    public void Limpiar()
    {
        for (int i = 0; i < _pendientes.Length; i++)
            _pendientes[i] = false;
    }
}
