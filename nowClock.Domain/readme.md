# Capa de Dominio (api-team.Domain)

Esta es la capa central de la Clean Architecture. Representa el núcleo del negocio y **no debe tener dependencias de ninguna otra capa ni de frameworks externos** (como Entity Framework, Dapper, Npgsql, etc.). Solo debe depender de las bibliotecas estándar de .NET.

## Estructura de Carpetas

### `/Entities`
Contiene las clases fundamentales del negocio.
*   **Qué va aquí:** Clases con propiedades, validaciones de estado interno y comportamiento relacionado estrictamente con el dominio.
*   **Cómo usarlas:** Ejemplo: `Usuario.cs`, `Producto.cs`. No deben tener decoradores de bases de datos (como `[Table]` o `[Column]`).

### `/Interfaces`
Contiene los contratos que definen cómo se debe acceder a los datos o recursos, pero sin implementarlos.
*   **Qué va aquí:** Interfaces de repositorios.
*   **Cómo usarlas:** Ejemplo: `IUsuarioRepository.cs`. La capa de infraestructura será la encargada de implementar esta interfaz.

### `/ValueObjects`
Contiene objetos inmutables que no tienen una identidad única (como un ID), sino que se definen por sus atributos.
*   **Qué va aquí:** Clases como `Direccion`, `Moneda`, `Coordenada`.
*   **Cómo usarlas:** Se instancian y, si dos Value Objects tienen los mismos valores en sus propiedades, se consideran iguales.

### `/Exceptions`
Contiene las excepciones personalizadas del dominio.
*   **Qué va aquí:** Errores específicos de las reglas de negocio.
*   **Cómo usarlas:** Ejemplo: `SaldoInsuficienteException.cs` o `UsuarioInactivoException.cs`. Estas son lanzadas por las Entidades cuando se viola una regla de negocio.

### `/Enums`
Contiene las enumeraciones utilizadas en todo el dominio.
*   **Qué va aquí:** Estados, tipos, categorías fijas.
*   **Cómo usarlas:** Ejemplo: `EstadoUsuario.cs` (Activo, Inactivo, Suspendido).