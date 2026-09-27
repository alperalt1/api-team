# Capa de Infraestructura (api-team.Infrastructure)

Esta capa contiene todos los detalles técnicos del sistema. Aquí es donde nos conectamos a la base de datos (PostgreSQL), ejecutamos los Procedimientos Almacenados (con Dapper), y nos comunicamos con servicios externos. **Depende de `api-team.Application` y `api-team.Domain`.**

## Estructura de Carpetas

### `/Data` o `/Connection`
Gestión de la conexión a la base de datos.
*   **Qué va aquí:** Clases que proveen la cadena de conexión o manejan transacciones globales.
*   **Cómo usarlas:** Ejemplo: Un `DbConnectionFactory` que retorna instancias de `NpgsqlConnection`.

### `/Repositories`
Implementación concreta de los contratos definidos en la capa de Dominio.
*   **Qué va aquí:** Clases que acceden directamente a PostgreSQL.
*   **Cómo usarlas:** Ejemplo: `UsuarioRepository.cs`. Aquí es donde utilizas Dapper para llamar a tus Stored Procedures (`QueryAsync`, `ExecuteAsync`).

### `/Services`
Implementación concreta de los contratos definidos en la capa de Aplicación.
*   **Qué va aquí:** Integraciones con tecnologías específicas.
*   **Cómo usarlas:** Ejemplo: `SmtpEmailService.cs` (para enviar correos), `AwsS3StorageService.cs` (para guardar archivos).

### `/Extensions`
Configuración de Inyección de Dependencias.
*   **Qué va aquí:** Métodos de extensión para registrar repositorios y servicios.
*   **Cómo usarlas:** Ejemplo: `DependencyInjection.cs`. Contiene un método `AddInfrastructureServices` que se llamará desde la capa de Presentación.