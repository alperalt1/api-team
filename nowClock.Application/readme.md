# Capa de Aplicación (api-team.Application)

Esta capa orquesta la lógica del sistema. Define *qué* hace la aplicación (los Casos de Uso) utilizando las entidades y contratos del Dominio. **Solo depende de la capa `api-team.Domain`.**

## Estructura de Carpetas

### `/DTOs` (Data Transfer Objects)
Modelos utilizados para enviar o recibir información desde y hacia la capa de presentación.
*   **Qué va aquí:** Clases planas sin lógica de negocio.
*   **Cómo usarlas:** Ejemplo: `UsuarioRegistroRequest.cs`, `UsuarioResponse.cs`. Evita exponer las entidades del Dominio directamente.

### `/Interfaces`
Contratos para servicios externos (cuyo detalle técnico se implementa en Infraestructura).
*   **Qué va aquí:** Interfaces como `IEmailService`, `IJwtProvider`.
*   **Cómo usarlas:** La capa de aplicación las inyecta y utiliza sin saber cómo se envían los correos o se generan los tokens.

### `/UseCases` o `/Services`
Contiene la lógica de aplicación. Si usas CQRS (ej. MediatR), puedes cambiar esta carpeta por `/Features` (dividida en `Commands` y `Queries`).
*   **Qué va aquí:** Clases que coordinan operaciones: validar datos, llamar al repositorio, aplicar lógica y devolver un resultado.
*   **Cómo usarlas:** Ejemplo: `RegistrarUsuarioService.cs`. Deben inyectar interfaces de repositorios (`IUsuarioRepository`).

### `/Validations`
Reglas de validación para los DTOs de entrada.
*   **Qué va aquí:** Clases que validan que los datos entrantes sean correctos antes de procesarlos.
*   **Cómo usarlas:** Generalmente se utiliza FluentValidation. Ejemplo: `UsuarioRegistroValidator.cs`.

### `/Mappings`
Configuración para transformar DTOs a Entidades y viceversa.
*   **Qué va aquí:** Perfiles de mapeo (ej. AutoMapper o Mapster).
*   **Cómo usarlas:** Ejemplo: `UsuarioProfile.cs`.