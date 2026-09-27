# Capa de Presentación (api-team - Web API)

Este proyecto es el punto de entrada de la aplicación. Se encarga de recibir las peticiones HTTP (Routing), autenticarlas, pasarlas a la capa de Aplicación y devolver respuestas HTTP (Status Codes). **Depende de `Application` (para ejecutar casos de uso) e `Infrastructure` (solo para inyectar dependencias en Program.cs).**

## Estructura de Carpetas

### `/Controllers` (o `/Endpoints` si usas Minimal APIs)
Controladores RESTful.
*   **Qué va aquí:** Clases que exponen los endpoints HTTP (GET, POST, PUT, DELETE).
*   **Cómo usarlas:** Ejemplo: `UsuariosController.cs`. Deben recibir DTOs, inyectar los servicios o casos de uso de la capa de `Application` y retornar respuestas estándar (ej. `Ok()`, `BadRequest()`). **No deben tener lógica de negocio.**

### `/Middlewares`
Filtros que interceptan la petición antes o después de llegar al controlador.
*   **Qué va aquí:** Manejo global de errores, logs de peticiones HTTP.
*   **Cómo usarlas:** Ejemplo: `GlobalExceptionMiddleware.cs`. Atrapa cualquier error no controlado y devuelve un JSON estructurado con estado 500.

### `/Extensions`
Clases estáticas para mantener el archivo `Program.cs` limpio.
*   **Qué va aquí:** Configuraciones modulares.
*   **Cómo usarlas:** Ejemplo: `SwaggerSetup.cs`, `CorsSetup.cs`, `AuthenticationSetup.cs`.

### Archivos Raíz
*   `appsettings.json`: Configuración de la aplicación (Cadenas de conexión de PostgreSQL, variables de entorno, claves secretas).
*   `Program.cs`: Ensambla todas las capas, registra la inyección de dependencias y configura el pipeline HTTP.