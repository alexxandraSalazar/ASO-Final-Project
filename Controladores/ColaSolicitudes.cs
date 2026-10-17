using Simulador.Modelo;

namespace Simulador.Controladores;

public sealed class ColaSolicitudes
{
    private readonly Queue<SolicitudES> _items = new();
    public int Capacidad { get; }
    public int Cantidad => _items.Count;
    public bool EstaVacia => _items.Count == 0;
    public bool EstaLlena => _items.Count >= Capacidad;

    public ColaSolicitudes(int capacidad = int.MaxValue) => Capacidad = capacidad;

    // Añade al final (cola). Devuelve false si no hay espacio.
    public bool Encolar(SolicitudES s)
    {
        if (EstaLlena) return false;
        _items.Enqueue(s);
        return true;
    }

    // Extrae el elemento del frente (cabeza) o null si está vacía.
    public SolicitudES? Desencolar() => _items.Count > 0 ? _items.Dequeue() : null;

    public SolicitudES? Ver() => _items.Count > 0 ? _items.Peek() : null;

    // Copia para mostrar el contenido en pantalla sin alterar la cola
    public IReadOnlyList<SolicitudES> Instantanea() => _items.ToArray();

    public void Limpiar() => _items.Clear();
}
