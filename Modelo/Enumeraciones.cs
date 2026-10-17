namespace Simulador.Modelo;

// Estados posibles de un dispositivo (antes DEV_IDLE / DEV_BUSY / DEV_ERROR)
public enum EstadoDispositivo
{
    Disponible,
    Ocupado,
    Procesando,
    FueraDeServicio
}

// Tipos de operaciones de E/S
public enum TipoOperacion
{
    Lectura,
    Escritura,
    ConsultarEstado
}

// Ciclo de vida de una solicitud
public enum EstadoSolicitud
{
    Pendiente,
    EnProceso,
    Finalizada,
    Fallida
}

// El valor numérico coincide con el número de IRQ
public enum TipoDispositivo
{
    Teclado = 0,
    Disco = 1,
    Impresora = 2
}

// Códigos de error del simulador
public enum CodigoError
{
    Ninguno = 0,
    E01_FueraDeServicio = 1,
    E02_ColaLlena = 2,
    E03_BloqueInvalido = 3,
    E04_ReintentosAgotados = 4,
    E05_OperacionNoSoportada = 5,
    E06_BufferTecladoLleno = 6,
    E07_SinPapel = 7
}
