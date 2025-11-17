# 🧩 AccountPanel: Aplicación Full-Stack de Panel de Cuentas

Un proyecto full-stack. Combina un backend en **.NET 9** con **Arquitectura Limpia** y un frontend **SPA (Single Page Application)** reactivo construido con **Vue.js 3**, **TypeScript** y **Tailwind CSS**.

-----

## ✨ Stack Tecnológico

Este proyecto contiene tecnologías modernas y demandadas para el desarrollo web full-stack.

### ⚙️ **Backend (.NET / C\#)**

  - **.NET 9** (C\# 13)
  - **Arquitectura Limpia (Clean Architecture)**: separación estricta de responsabilidades (`Domain`, `Application`, `Infrastructure`, `Api`)
  - **API RESTful** con versionado (`Asp.Versioning`)
  - **Entity Framework Core 9** con SQLite para persistencia de datos.
  - **Control de Concurrencia Optimista** con `[Timestamp]` (`RowVersion`) para prevenir conflictos de edición.
  - **Autenticación JWT Avanzada** con **Refresh Tokens** y rotación de tokens.
  - **Flujo de Autenticación Completo** con **verificación de email** y **reseteo de contraseña** usando plantillas HTML.
  - **Servicio de Email** con abstracción (`IEmailService`) e implementación para **MailKit (Mailtrap)**.
  - **Manejo de Excepciones Global** con middleware personalizado.
  - **Autorización Basada en Roles (RBAC)** con claims y el atributo `[Authorize(Roles = "Admin")]`.
  - **Manejo de Subida de Archivos** con `IFormFile` para avatares de perfil.
  - **Pruebas Unitarias** (`xUnit`, `Moq`) para la lógica de negocio.
  - **Pruebas de Integración** (`WebApplicationFactory`) para los endpoints de la API.
  - **Servicios Externos**: login con Google (`Google.Apis.Auth`).
  - **Configuración de CORS** para permitir el acceso del frontend.
  - **Inyección de Dependencias** (`Program.cs`).
  - **Logging Estructurado** con `Serilog`.
  - **Mapeo de Objetos** con `AutoMapper`.
  - **Validación Avanzada** con `FluentValidation`.

### 🖥️ **Frontend (Vue.js / TypeScript)**

  - **Vue.js 3** (con Composition API y `<script setup>`)
  - **Vite** como herramienta de construcción y servidor de desarrollo.
  - **TypeScript** para un tipado estático robusto.
  - **Inicio de sesión social con Google OAuth 2.0** (`vue3-google-login`).
  - **Rutas de Autenticación Completas** (Olvide mi contraseña, Restablecer contraseña).
  - **Vue Router** para enrutamiento del lado del cliente y guardias de navegación.
  - **Pinia** para la gestión de estado global.
  - **`pinia-plugin-persistedstate`** para persistir tokens (Access y Refresh) en `localStorage`.
  - **Tailwind CSS v4** con **`@tailwindcss/forms`** para inputs consistentes.
  - **Tema Oscuro (Dark Mode)** manual y automático con `useDark` de `@vueuse/core`.
  - **Animaciones y Transiciones** de página (`<Transition>`) y de UI (`transition-colors`).
  - **Servicio API centralizado** con **interceptores de Axios** (manejo automático de 401 y *refresh tokens*).
  - **CRUD de Administrador en Frontend** con filtros de búsqueda, *debounce* y diálogos de confirmación (`vue-sonner`).
  - **Notificaciones (Toasts)** elegantes con `Vue-Sonner` para feedback de API.
  - **Renderizado de imágenes condicional** (avatares locales vs. URLs externas).
  - **Componentes Reutilizables** (`BaseTable`, `BaseInput`, `BaseButton` con variantes).
  - **UI y Rutas Condicionales** basadas en el rol del usuario (Admin vs User).
  - **ESLint** y **Prettier** para la calidad y formato del código.

-----

## 🚀 Cómo Empezar

Sigue estos pasos para configurar y ejecutar el proyecto completo localmente.

### 🔧 Prerrequisitos

  - [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) o superior.
  - [Node.js](https://nodejs.org/) (versión 20+ recomendada) y npm.
  - Un editor de código como Visual Studio, JetBrains Rider o VS Code.
  - (Opcional pero recomendado para pruebas de email) Una cuenta de [Mailtrap.io](https://mailtrap.io/).

-----

### 1️⃣ Clonar el Repositorio

```bash
git clone [https://github.com/polancou/AccountPanel-Vue-Dotnet.git](https://github.com/polancou/AccountPanel-Vue-Dotnet.git)
cd AccountPanel-Vue-Dotnet
```

-----

### 2️⃣ Configurar el Backend (.NET)

#### 🔐 Configurar Secretos de Usuario

Navega al proyecto de la API:

```bash
cd AccountPanel/AccountPanel.Api
```

Inicializa los secretos de usuario:

```bash
dotnet user-secrets init
```

Establece los secretos necesarios (reemplaza los valores de ejemplo):

```bash
# Claves de la aplicación
dotnet user-secrets set "Jwt:Key" "UNA_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA_GENERADA_POR_TI"
dotnet user-secrets set "AutoMapper:Key" "TU_CLAVE_DE_LICENCIA_DE_AUTOMAPPER"
dotnet user-secrets set "AppSettings:FrontendBaseUrl" "http://localhost:5173"

# Credenciales de Google Auth
dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID_DE_GOOGLE.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "TU_CLIENT_SECRET_DE_GOOGLE"

# Credenciales de Mailtrap (para pruebas de email)
dotnet user-secrets set "MailtrapSettings:Host" "smtp.mailtrap.io"
dotnet user-secrets set "MailtrapSettings:Port" "587"
dotnet user-secrets set "MailtrapSettings:Username" "TU_USERNAME_DE_MAILTRAP"
dotnet user-secrets set "MailtrapSettings:Password" "TU_PASSWORD_DE_MAILTRAP"
dotnet user-secrets set "MailtrapSettings:FromEmail" "no-reply@tuapp.com"
```

Vuelve a la raíz del repositorio:

```bash
cd ../..
```

#### 📦 Restaurar Dependencias del Backend

```bash
dotnet restore AccountPanel/AccountPanel.sln
```

#### 🗃️ Crear la Base de Datos

1.  **Crear las Migraciones:** Ejecuta los siguientes comandos desde la raíz del repositorio para crear tus migraciones (si aún no existen en la carpeta `Infrastructure/Migrations`):

    ```bash
    dotnet ef migrations add InitialCreate --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

    ```bash
    dotnet ef migrations add AddAvatarUrlToUsuario --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

    ```bash
    dotnet ef migrations add AddRefreshTokensToUsuario --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

    ```bash
    dotnet ef migrations add AddRowVersionToUsuario --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

    ```bash
    dotnet ef migrations add AddEmailVerification --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

    ```bash
    dotnet ef migrations add AddPasswordResetToken --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

2.  **Aplicar las Migraciones:** Esto creará o actualizará el archivo `sampleDb.db`:

    ```bash
    dotnet ef database update --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

-----

### 3️⃣ Configurar el Frontend (Vue.js)

Navega al directorio del cliente:

```bash
cd client
```

Instala las dependencias de npm:

```bash
npm install
```

**Configuración del Entorno (.env):**
Este proyecto **no usa proxy**, sino **CORS** en el backend.

1.  Crea un archivo llamado `.env` en la raíz del directorio `client/`.

2.  Añade las siguientes variables de entorno. **`VITE_GOOGLE_CLIENT_ID` debe ser el mismo Client ID que usaste en los secretos del backend.**

    ```
    VITE_API_BASE_URL=http://localhost:5272/api
    VITE_UPLOADS_BASE_URL=http://localhost:5272
    VITE_GOOGLE_CLIENT_ID=TU_CLIENT_ID_DE_GOOGLE.apps.googleusercontent.com
    ```

3.  **IMPORTANTE (Google OAuth):** En tu [Consola de Google Cloud](https://console.cloud.google.com/), ve a **APIs y servicios \> Credenciales** y edita tu Client ID. Asegúrate de añadir lo siguiente a **Orígenes de JavaScript autorizados**:

      * `http://localhost:5173`
      * `http://127.0.0.1:5173`

-----

## 🏃‍♂️ Ejecución en Desarrollo

Para trabajar en el proyecto, abre **dos terminales** simultáneamente en la raíz del repositorio.

### 🧩 Terminal 1: Ejecutar el Backend

```bash
dotnet run --project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

*(La API estará disponible en `http://localhost:5272`. La primera ejecución aplicará migraciones)*

### 🧩 Terminal 2: Ejecutar el Frontend

```bash
cd client
npm run dev
```

*(La aplicación Vue estará disponible en `http://localhost:5173` (o un puerto similar))*

Abre la dirección del frontend (`http://localhost:5173`) en tu navegador para usar la aplicación.

-----

## 🧪 Ejecutar las Pruebas

### ✅ Backend

Ejecuta todas las pruebas de xUnit (unitarias y de integración):

```bash
cd AccountPanel
dotnet test
```

### ✅ Frontend

Ejecuta las pruebas unitarias con Vitest:

```bash
cd client
npm run test:unit
```

-----

## 📜 Licencia

Este proyecto está bajo la **Licencia MIT**.
Consulta el archivo [`LICENSE`](https://www.google.com/search?q=%5Bhttps://www.google.com/search%3Fq%3D./LICENSE%5D\(https://www.google.com/search%3Fq%3D./LICENSE\)) para más detalles.