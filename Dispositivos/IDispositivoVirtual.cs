using Simulador.Modelo;

namespace Simulador.Dispositivos;

public interface IDispositivoVirtual
{
    EstadoDispositivo Estado { get; } // registro de estado
    int RegistroControl { get; }      // comando en curso (1 = leer, 2 = escribir, 3 = consultar)
    int RegistroError { get; }        // 0 = sin error; 1..7 = E01..E07
    string RegistroDatos { get; }     // buffer de datos de la operación
    int CiclosRestantes { get; }      // ticks que faltan para terminar

    void Comenzar(SolicitudES s);     // carga registros e inicia la operación
    bool Avanzar();                   // consume un tick; true si terminó
    void InyectarFalla(CodigoError codigo, int repeticiones = 1); // pruebas
    void Reiniciar();                 // registros a cero; estado Disponible
}
