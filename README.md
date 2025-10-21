# BaseApp: Plantilla de API Profesional con ASP.NET Core y Arquitectura Limpia

`BaseApp` es una plantilla de proyecto de alto rendimiento, diseñada como un punto de partida robusto y escalable para construir APIs web modernas y seguras con ASP.NET Core. Incorpora un conjunto de las mejores prácticas de la industria, incluyendo una estricta **Arquitectura Limpia (Clean Architecture)** para garantizar la máxima separación de responsabilidades, mantenibilidad y testeabilidad.

## ✨ Características Principales

Esta plantilla no es solo un "Hola Mundo", es una base lista para producción que incluye:

-   **Arquitectura Limpia y Escalable:**
    -   **Proyecto Domain:** Contiene las entidades y la lógica de negocio más pura, sin dependencias externas.
    -   **Proyecto Application:** Orquesta los casos de uso a través de servicios (`AuthService`, `ProfileService`) y define los contratos (interfaces) que el resto de la aplicación debe seguir.
    -   **Proyecto Infrastructure:** Implementa los detalles técnicos como el acceso a la base de datos (Entity Framework Core) y la comunicación con servicios externos (Google Auth, JWT).
    -   **Proyecto Api:** Expone la lógica de la aplicación al mundo exterior a través de controladores delgados y endpoints HTTP.
    -   **Inyección de Dependencias:** Totalmente configurada para conectar las interfaces de `Application` con las implementaciones de `Infrastructure`.
    -   **.NET 9:** Construida sobre la última versión del framework.

-   **Sistema de Autenticación y Perfil de Usuario:**
    -   **Login Local:** Registro y login con email/contraseña, usando BCrypt.net para el hashing seguro.
    -   **Login con Google:** Implementación lista para usar de inicio de sesión con proveedores externos.
    -   **Seguridad con JWT:** Todos los flujos de autenticación generan un JSON Web Token para proteger los endpoints.
    -   **Gestión de Perfil:** Endpoints RESTful para que los usuarios autenticados puedan obtener y actualizar su información de perfil.

-   **Base de Datos y Migraciones Robustas:**
    -   **Entity Framework Core:** Configurado con SQLite para un arranque rápido y fácil.
    -   **`IDesignTimeDbContextFactory`:** Implementado para que las migraciones (`dotnet ef migrations add`) funcionen de manera fiable.

-   **Estrategia de Pruebas Completa:**
    -   **Pruebas de Integración:** Un proyecto (`BaseApp.Api.IntegrationTests`) que verifica flujos completos de la API contra una base de datos en memoria.
    -   **Pruebas Unitarias:** Un proyecto (`BaseApp.Application.UnitTests`) que prueba la lógica de negocio en total aislamiento, utilizando **Moq** para simular dependencias.

-   **Versionado de API:**
    -   Implementación de versionado a través de la URL (ej. `/api/v1/...`).
    -   Permite la evolución segura de la API sin romper la compatibilidad con clientes existentes.

-   **Validación Avanzada con FluentValidation:**
    -   Las reglas de validación son potentes, expresivas y están separadas de los DTOs, manteniéndolos limpios.
    -   Integrado con Swagger para mostrar las reglas en la documentación de la API.

-   **Manejo de Errores y Logging Centralizado:**
    -   Un **middleware global de excepciones** captura todos los errores y devuelve una respuesta JSON estandarizada con un `TraceId`.
    -   **Logging Estructurado con Serilog**, configurado para escribir en la consola y en archivos diarios rotativos.

-   **Mapeo de Objetos con AutoMapper:**
    -   Automatiza la conversión entre entidades y DTOs mediante perfiles explícitos para mayor claridad.

-   **Gestión Segura de Secretos:**
    -   Configurado para usar **"User Secrets"** en desarrollo, manteniendo las claves sensibles fuera del control de versiones.

-   **Documentación de API:**
    -   Integración con **Swagger** y **Scalar** para una documentación interactiva y moderna.

## 🚀 Cómo Empezar

### Prerrequisitos

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) o superior.
-   Un editor de código como Visual Studio, JetBrains Rider o VS Code.

### Pasos

1.  **Clonar el repositorio.**

2.  **Configurar Secretos de Usuario:**
    * Abre una terminal en la carpeta del proyecto `BaseApp/BaseApp.Api`.
    * Inicializa los secretos de usuario:
        ```bash
        dotnet user-secrets init
        ```
    * Establece los secretos necesarios (reemplaza los valores de ejemplo):
        ```bash
        dotnet user-secrets set "Jwt:Key" "UNA_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA_GENERADA_POR_TI"
        dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID_DE_GOOGLE.apps.googleusercontent.com"
        dotnet user-secrets set "Authentication:Google:ClientSecret" "TU_CLIENT_SECRET_DE_GOOGLE"
        dotnet user-secrets set "AutoMapper:Key" "TU_CLAVE_DE_LICENCIA_DE_AUTOMAPPER"
        ```

3.  **Restaurar dependencias y ejecutar la aplicación:**
    * Navega a la raíz de la solución (`BaseApp/`).
        ```bash
        dotnet restore
        dotnet run --project BaseApp.Api/BaseApp.Api.csproj
        ```
    Los endpoints ahora están versionados, por ejemplo: `.../api/v1/auth/login`.

4.  **Explorar la API:**
    Navega a `/scalar` o `/swagger` en tu navegador para ver la documentación interactiva.

5.  **Ejecutar las pruebas:**
    Para ejecutar todos los tests (unitarios y de integración), navega a la raíz de la solución (`BaseApp/`) y ejecuta:
    ```bash
    dotnet test
    ```

## 📜 Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo `LICENSE` para más detalles.