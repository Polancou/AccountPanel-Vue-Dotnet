# 🧩 AccountPanel: Aplicación Full-Stack de Panel de Cuentas

Un proyecto full-stack que combina un backend en **.NET 9** con **Arquitectura Limpia** y un frontend **SPA (Single Page Application)** reactivo construido con **Vue.js 3**, **TypeScript** y **Tailwind CSS**.

Este proyecto incluye un flujo de autenticación completo con verificación de email, reseteo de contraseña, y un robusto panel de administrador con capacidades CRUD.

> **Novedad:** El proyecto ahora está totalmente **Dockerizado**. Puedes levantar toda la infraestructura (Backend, Frontend y Base de Datos) con un solo comando.

---

## 🐳 Ejecución Rápida con Docker Compose (Recomendado)

Esta es la forma más sencilla de levantar todo el entorno sin instalar dependencias locales (excepto Docker).

### 1. Prerrequisitos
- **Docker Desktop** instalado y ejecutándose.

### 2. Configuración de Entorno
El proyecto utiliza un archivo `.env` en la raíz para manejar secretos de forma segura en los contenedores.

1.  Copia el archivo de plantilla:
    ```bash
    cp .env.example .env
    ```
2.  Abre el archivo `.env` y establece tus credenciales.
    * **Importante:** Debes definir una contraseña fuerte para `SA_PASSWORD` o SQL Server no iniciará.
    * Configura tus credenciales de Google y SMTP (Gmail, Mailtrap, AWS) si deseas probar esas funcionalidades.

### 3. Levantar la Aplicación
Desde la raíz del proyecto, ejecuta:

```bash
docker-compose up --build
````

Una vez que los contenedores estén listos, podrás acceder a:

- **Frontend:** [http://localhost:5173](https://www.google.com/search?q=http://localhost:5173)
- **Backend API (Swagger):** [http://localhost:5272/swagger](https://www.google.com/search?q=http://localhost:5272/swagger)
- **Base de Datos:** `localhost:1433`

### 4\. Base de Datos (Primera Ejecución)

Si es la primera vez que levantas el proyecto, la base de datos estará vacía. Las migraciones se aplicarán automáticamente al iniciar el contenedor de la API gracias a la configuración en `Program.cs`.

-----

## ✨ Stack Tecnológico

### ⚙️ **Backend (.NET / C\#)**

- **.NET 9** (C\# 13)
- **Arquitectura Limpia (Clean Architecture)**: Separación estricta de responsabilidades (`Domain`, `Application`, `Infrastructure`, `Api`).
- **API RESTful** con versionado (`Asp.Versioning`).
- **Entity Framework Core 9** con **SQL Server** como proveedor de base de datos.
- **Control de Concurrencia Optimista** con `[Timestamp]` (`RowVersion`).
- **Autenticación JWT Avanzada** con **Refresh Tokens** y rotación de tokens.
- **Flujo de Autenticación Completo** con **verificación de email** y **reseteo de contraseña** usando plantillas HTML.
- **Servicio de Email Agnóstico** (`SmtpEmailService`) compatible con cualquier proveedor SMTP (Gmail, AWS SES, Mailtrap).
- **Manejo de Excepciones Global** con middleware personalizado.
- **CRUD de Administrador Completo** con filtros de búsqueda y paginación optimizada (`IQueryable`).
- **Pruebas Unitarias** (`xUnit`, `Moq`).
- **Pruebas de Integración** (`WebApplicationFactory`) contra una base de datos SQL real en contenedor.
- **Validación Avanzada** con `FluentValidation`.

### 🖥️ **Frontend (Vue.js / TypeScript)**

- **Vue.js 3** (Composition API y `<script setup>`)
- **Vite** como herramienta de construcción.
- **Docker Multi-stage build** con **Nginx** para producción.
- **TypeScript** para tipado estático robusto.
- **Google OAuth 2.0** (`vue3-google-login`).
- **Pinia** para la gestión de estado global y persistencia (`pinia-plugin-persistedstate`).
- **Tailwind CSS v4** con `@tailwindcss/forms`.
- **Tema Oscuro (Dark Mode)** automático y manual.
- **Servicio API centralizado** con **interceptores de Axios** (manejo automático de 401 y *refresh tokens*).

-----

## 🚀 Ejecución Manual (Desarrollo Local)

Sigue estos pasos si prefieres ejecutar y depurar los servicios individualmente en tu máquina (sin Docker Compose).

### 🔧 Requisitos Previos

- **.NET 9 SDK** o superior.
- **Node.js** (versión 20+ recomendada).
- **Docker Desktop** (necesario para el contenedor de base de datos individual).

### 1️⃣ Clonar el Repositorio

```bash
git clone [https://github.com/polancou/AccountPanel-Vue-Dotnet.git](https://github.com/polancou/AccountPanel-Vue-Dotnet.git)
cd AccountPanel-Vue-Dotnet
```

### 2️⃣ Configurar el Backend (.NET)

#### 1\. Iniciar la Base de Datos

Inicia un contenedor de **Azure SQL Edge** individualmente:

```bash
docker run --cap-add SYS_PTRACE -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=TuPasswordFuerte!" \
   -p 1433:1433 --name sql-edge-dev \
   -d [mcr.microsoft.com/azure-sql-edge:latest](https://mcr.microsoft.com/azure-sql-edge:latest)
```

#### 2\. Configurar Secretos de Usuario (User Secrets)

Para evitar guardar credenciales en el código, el proyecto utiliza **User Secrets**. Ejecuta los siguientes comandos dentro de la carpeta `AccountPanel/AccountPanel.Api`:

```bash
cd AccountPanel/AccountPanel.Api
dotnet user-secrets init

# --- Credenciales del Sistema ---
# Clave para firmar tokens (debe ser larga y segura)
dotnet user-secrets set "Jwt:Key" "SUPER_SECRET_KEY_MIN_32_CHARS_LONG"
# URL del frontend para enlaces en correos
dotnet user-secrets set "AppSettings:FrontendBaseUrl" "http://localhost:5173"
# (Opcional) Licencia de AutoMapper si aplica
dotnet user-secrets set "AutoMapper:Key" "dummy"

# --- Base de Datos ---
# Cadena de conexión para desarrollo
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=accountpanel_db;User Id=sa;Password=TuPasswordFuerte!;TrustServerCertificate=True;"
# Cadena de conexión para pruebas de integración
dotnet user-secrets set "ConnectionStrings:TestConnection" "Server=localhost,1433;Database=master;User Id=sa;Password=TuPasswordFuerte!;TrustServerCertificate=True;"

# --- Google OAuth ---
dotnet user-secrets set "Authentication:Google:ClientId" "TU_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "TU_GOOGLE_CLIENT_SECRET"

# --- Configuración SMTP (Email) ---
# Ejemplo con Mailtrap (o Gmail/AWS cambiando el host)
dotnet user-secrets set "SmtpSettings:Host" "smtp.mailtrap.io"
dotnet user-secrets set "SmtpSettings:Port" "587"
dotnet user-secrets set "SmtpSettings:Username" "TU_USUARIO_SMTP"
dotnet user-secrets set "SmtpSettings:Password" "TU_PASSWORD_SMTP"
dotnet user-secrets set "SmtpSettings:FromEmail" "no-reply@tuapp.com"
dotnet user-secrets set "SmtpSettings:FromName" "AccountPanel Dev"

cd ../..
```

#### 3\. Aplicar Migraciones

```bash
dotnet ef database update --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

### 3️⃣ Configurar el Frontend (Vue.js)

Navega al directorio `client/`, instala las dependencias y crea tu archivo de entorno local.

```bash
cd client
npm install
```

Crea el archivo `client/.env` basado en `client/.env.example` y define la URL de tu API local (normalmente `http://localhost:5272/api`).

### 4️⃣ Ejecutar

**Terminal 1 (Backend):**

```bash
dotnet run --project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

**Terminal 2 (Frontend):**

```bash
cd client
npm run dev
```

-----

## 🧪 Ejecutar las Pruebas

### ✅ Backend

Ejecuta todas las pruebas (unitarias e integración). Las pruebas de integración gestionarán automáticamente su propia base de datos en Docker.

```bash
cd AccountPanel
dotnet test
```

### ✅ Frontend

```bash
cd client
npm run test:unit
```
