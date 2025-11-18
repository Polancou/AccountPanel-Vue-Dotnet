-----

# 🧩 AccountPanel: Aplicación Full-Stack de Panel de Cuentas

Un proyecto full-stack. Combina un backend en **.NET 9** con **Arquitectura Limpia** y un frontend **SPA (Single Page Application)** reactivo construido con **Vue.js 3**, **TypeScript** y **Tailwind CSS**.

Este proyecto ha sido refactorizado para usar **SQL Server (Azure SQL Edge)** en lugar de SQLite, e incluye un flujo de autenticación completo con verificación de email, reseteo de contraseña, y un robusto panel de administrador con capacidades CRUD.

-----

## ✨ Stack Tecnológico

### ⚙️ **Backend (.NET / C\#)**

- **.NET 9** (C\# 13)
- **Arquitectura Limpia (Clean Architecture)**: Separación estricta de responsabilidades (`Domain`, `Application`, `Infrastructure`, `Api`).
- **API RESTful** con versionado (`Asp.Versioning`).
- **Entity Framework Core 9** con **SQL Server** como proveedor de base de datos de producción.
- **Control de Concurrencia Optimista** con `[Timestamp]` (`RowVersion`) para prevenir conflictos de edición simultánea.
- **Autenticación JWT Avanzada** con **Refresh Tokens** y rotación de tokens.
- **Flujo de Autenticación Completo** con **verificación de email** y **reseteo de contraseña** usando plantillas HTML.
- **Servicio de Email** con abstracción (`IEmailService`) e implementación para **MailKit (Mailtrap)**.
- **Manejo de Excepciones Global** con middleware personalizado para respuestas de error limpias.
- **CRUD de Administrador Completo** (Eliminar Usuario, Actualizar Rol) con validaciones de seguridad.
- **Filtros de Búsqueda y Paginación** en el panel de admin usando `IQueryable` para eficiencia.
- **Pruebas Unitarias** (`xUnit`, `Moq`) para la lógica de negocio.
- **Pruebas de Integración de Alta Fidelidad** (`WebApplicationFactory`) que se ejecutan contra una base de datos SQL Server dedicada y se ejecutan secuencialmente usando **xUnit Collections**.
- **Servicios Externos**: Login con Google (`Google.Apis.Auth`).
- **Validación Avanzada** con `FluentValidation` (usada para todos los DTOs de entrada).

### 🖥️ **Frontend (Vue.js / TypeScript)**

- **Vue.js 3** (con Composition API y `<script setup>`)
- **Vite** como herramienta de construcción y servidor de desarrollo.
- **TypeScript** para un tipado estático robusto.
- **Inicio de sesión social con Google OAuth 2.0** (`vue3-google-login`).
- **Rutas de Autenticación Completas** (Verificar Email, Olvidé mi contraseña, Restablecer contraseña).
- **Vue Router** para enrutamiento del lado del cliente y guardias de navegación.
- **Pinia** para la gestión de estado global.
- **`pinia-plugin-persistedstate`** para persistir tokens (Access y Refresh) en `localStorage`.
- **Tailwind CSS v4** con **`@tailwindcss/forms`** para inputs consistentes.
- **Tema Oscuro (Dark Mode)** manual (con toggle) y automático (preferencia del SO) usando `useDark` de `@vueuse/core`.
- **Animaciones y Transiciones** de página (`<Transition>`) y de UI (`transition-colors`).
- **Servicio API centralizado** con **interceptores de Axios** (manejo automático de 401 y *refresh tokens*).
- **CRUD de Administrador en Frontend** con filtros de búsqueda, *debounce* (`@vueuse/core`) y diálogos de confirmación (`vue-sonner`).

-----

## 🚀 Cómo Empezar

Sigue estos pasos para configurar y ejecutar el proyecto completo localmente.

### 🔧 Requisitos Previos

- **.NET 9 SDK** o superior.
- **Node.js** (versión 20+ recomendada).
- **Docker Desktop**. El entorno de desarrollo depende de un contenedor de SQL Server.
- (Opcional) Una cuenta de [Mailtrap.io](https://mailtrap.io/) para pruebas de email.

-----

### 1️⃣ Clonar el Repositorio

```bash
git clone https://github.com/polancou/AccountPanel-Vue-Dotnet.git
cd AccountPanel-Vue-Dotnet
```

-----

### 2️⃣ Configurar el Backend (.NET)

#### 1\. Iniciar la Base de Datos (Docker)

Abre tu terminal y ejecuta el siguiente comando para iniciar un contenedor de **Azure SQL Edge**. (Es la versión de SQL Server compatible con Mac M1/ARM64 y gratuita para desarrollo).

```bash
docker run --cap-add SYS_PTRACE -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=TuPasswordFuerte!" \
   -p 1433:1433 --name sql-edge-dev \
   -d mcr.microsoft.com/azure-sql-edge:latest
```

**Nota:** Reemplaza `TuPasswordFuerte!` por una contraseña segura de tu elección.

#### 2\. Configurar Secretos de Usuario

Navega al proyecto de la API y configura tus `user-secrets`. **Debes** usar la misma contraseña que pusiste en el comando de Docker.

```bash
cd AccountPanel/AccountPanel.Api
dotnet user-secrets init

# Clave para la API (genera una clave larga y segura)
dotnet user-secrets set "Jwt:Key" "UNA_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA"

# Cadena de conexión de DESARROLLO (apunta a tu Docker)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=accountpanel_db;User Id=sa;Password=TuPasswordFuerte!;TrustServerCertificate=True;"

# Cadena de conexión de PRUEBAS (apunta a la BD 'master' en el mismo Docker)
dotnet user-secrets set "ConnectionStrings:TestConnection" "Server=localhost,1433;Database=master;User Id=sa;Password=TuPasswordFuerte!;TrustServerCertificate=True;"

# Claves de Servicios Externos
dotnet user-secrets set "AppSettings:FrontendBaseUrl" "http://localhost:5173"
dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID_DE_GOOGLE.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "TU_CLIENT_SECRET_DE_GOOGLE"
dotnet user-secrets set "MailtrapSettings:Host" "smtp.mailtrap.io"
dotnet user-secrets set "MailtrapSettings:Port" "587"
dotnet user-secrets set "MailtrapSettings:Username" "TU_USERNAME_DE_MAILTRAP"
dotnet user-secrets set "MailtrapSettings:Password" "TU_PASSWORD_DE_MAILTRAP"
dotnet user-secrets set "MailtrapSettings:FromEmail" "no-reply@tuapp.com"

# Vuelve a la raíz del repositorio
cd ../..
```

#### 3\. Crear la Base de Datos de Desarrollo

Entity Framework **no creará** la base de datos; solo las tablas. Debes crearla manualmente:

```bash
# 1. Crea la base de datos de DESARROLLO
docker exec -it sql-edge-dev /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "TuPasswordFuerte!" -Q "CREATE DATABASE accountpanel_db;"
```

*(La base de datos de prueba, `accountpanel_db_test`, será creada y destruida automáticamente por la `TestApiFactory`).*

#### 4\. Aplicar las Migraciones

Una vez creada la base de datos, ejecuta las migraciones para crear las tablas en tu base de datos de **desarrollo** (`accountpanel_db`):

```bash
dotnet ef database update --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

-----

### 3️⃣ Configurar el Frontend (Vue.js)

Navega al directorio `client/`, instala las dependencias y crea tu archivo `.env`.

```bash
cd client
npm install
```

**Crea el archivo `client/.env`** con el siguiente contenido:

```
VITE_API_BASE_URL=http://localhost:5272/api
VITE_UPLOADS_BASE_URL=http://localhost:5272
VITE_GOOGLE_CLIENT_ID=TU_MISMO_CLIENT_ID_DE_GOOGLE.apps.googleusercontent.com
```

-----

## 🏃‍♂️ Ejecución en Desarrollo

Abre **dos terminales** en la raíz del repositorio.

### 🧩 Terminal 1: Ejecutar el Backend

```bash
dotnet run --project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

*(La API estará disponible en `http://localhost:5272` y se conectará a tu base de datos SQL Edge)*

### 🧩 Terminal 2: Ejecutar el Frontend

```bash
cd client
npm run dev
```

*(La aplicación Vue estará disponible en `http://localhost:5173`)*

Abre `http://localhost:5173` en tu navegador para usar la aplicación.

-----

## 🧪 Ejecutar las Pruebas

### ✅ Backend

Ejecuta todas las pruebas de xUnit (unitarias y de integración). **Nota:** Esto creará y destruirá automáticamente una base de datos de prueba única (ej. `accountpanel_test_...`) en tu contenedor Docker gracias a la `TestApiFactory`.

```bash
cd AccountPanel
dotnet test
```

### ✅ Frontend

```bash
cd client
npm run test:unit
```

-----