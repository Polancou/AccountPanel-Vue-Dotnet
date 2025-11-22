# 🧩 AccountPanel: Aplicación Full-Stack de Panel de Cuentas

Un proyecto full-stack moderno y robusto que combina un backend en **.NET 9** con **Arquitectura Limpia** y un frontend **SPA** reactivo construido con **Vue.js 3**, **TypeScript** y **Tailwind CSS**.

Este proyecto va más allá de lo básico, implementando estándares de seguridad de grado empresarial, optimizaciones de rendimiento y una experiencia de desarrollador totalmente dockerizada.

---

## 🚀 Características Destacadas

### 🛡️ Seguridad Avanzada
* **Cookies HttpOnly & Secure:** Estrategia híbrida de autenticación. El `AccessToken` vive en memoria (para evitar CSRF) y el `RefreshToken` en una cookie `HttpOnly` (para evitar XSS).
* **Rate Limiting:** Protección contra fuerza bruta y DoS utilizando el middleware nativo de .NET 9 (`FixedWindowLimiter`) en endpoints críticos de autenticación.
* **Validación de Archivos Robusta:** Verificación de "Magic Numbers" (firmas hexadecimales) al subir imágenes para evitar la ejecución de scripts maliciosos disfrazados.
* **CORS Estricto:** Políticas de origen restringidas explícitamente para permitir credenciales de forma segura.
* **Anti-Enumeración:** Respuestas genéricas en el registro y recuperación de contraseña para evitar la enumeración de usuarios.

### 🏗️ Arquitectura & Backend
* **Arquitectura Limpia (Clean Architecture):** Separación estricta de responsabilidades (`Domain`, `Application`, `Infrastructure`, `Api`).
* **Abstracción de Servicios:** Implementación desacoplada para Almacenamiento (`IFileStorageService`) y Email (`IEmailService`), permitiendo cambiar entre Local/S3 o Mailtrap/AWS SES sin tocar la lógica de negocio.
* **Health Checks:** Endpoint `/health` para monitoreo de estado en orquestadores.
* **Entity Framework Core 9:** Con SQL Server y manejo de concurrencia optimista (`RowVersion`).

### 🎨 Frontend (UX/UI)
* **TypeScript Estricto:** Código tipado rigurosamente (sin `any`) para mayor mantenibilidad.
* **UX Optimizado:** Implementación de **Skeleton Loaders** para cargas suaves y validaciones de archivos en el cliente para ahorrar ancho de banda.
* **Gestión de Estado:** Uso de **Pinia** con persistencia selectiva (solo datos de perfil, nunca credenciales).
* **Interacción:** Notificaciones toast (`vue-sonner`) y manejo centralizado de errores de API con interceptores de Axios.

---

## 🐳 Ejecución Rápida con Docker Compose (Recomendado)

La forma más sencilla de levantar toda la infraestructura (BD, API, Frontend).

### 1. Prerrequisitos
- **Docker Desktop** instalado y ejecutándose.

### 2. Configuración de Entorno
El proyecto utiliza un archivo `.env` en la raíz para inyectar secretos en los contenedores.

1.  Copia el archivo de plantilla:
    ```bash
    cp .env.example .env
    ```
2.  Abre el archivo `.env` y define tus credenciales.
    * **SA_PASSWORD:** Debe ser fuerte (Mayúsculas, minúsculas, números) o SQL Server no iniciará.
    * **SMTP:** Configura tus credenciales (Gmail, AWS SES, Mailtrap) para probar los correos.

### 3. Levantar la Aplicación
Desde la raíz del proyecto:

```bash
docker-compose up --build
````

Una vez iniciados los contenedores:

- **Frontend:** [http://localhost:5173](https://www.google.com/search?q=http://localhost:5173)
- **Backend API (Swagger):** [http://localhost:5272/swagger](https://www.google.com/search?q=http://localhost:5272/swagger)
- **Health Check:** [http://localhost:5272/health](https://www.google.com/search?q=http://localhost:5272/health)

### 4\. Base de Datos

Las migraciones se aplican **automáticamente** al iniciar el contenedor de la API. No necesitas ejecutar comandos manuales.

-----

## ✨ Stack Tecnológico Detallado

### ⚙️ **Backend (.NET / C\#)**

- **Framework:** .NET 9 (C\# 13)
- **Base de Datos:** SQL Server (Azure SQL Edge en Docker)
- **ORM:** Entity Framework Core 9
- **Auth:** JWT (Bearer) + Cookies HttpOnly + Google OAuth 2.0
- **Validación:** FluentValidation
- **Logging:** Serilog
- **Testing:** xUnit, Moq, FluentAssertions, WebApplicationFactory (Integration Tests)

### 🖥️ **Frontend (Vue.js / TypeScript)**

- **Framework:** Vue.js 3 (Composition API + Script Setup)
- **Build Tool:** Vite
- **Estilos:** Tailwind CSS v4 + @tailwindcss/forms
- **Estado:** Pinia + pinia-plugin-persistedstate
- **HTTP:** Axios (con interceptores para Refresh Token automático)
- **Validación:** Vee-Validate + Zod
- **Testing:** Vitest

-----

## 🚀 Ejecución Manual (Desarrollo Local)

Si prefieres ejecutar los servicios individualmente en tu máquina para depurar.

### 1\. Configurar Backend

```bash
cd AccountPanel/AccountPanel.Api
dotnet user-secrets init
# Configura tus secretos (Ver sección de secretos abajo)
dotnet run
```

### 2\. Configurar Frontend

```bash
cd client
cp .env.example .env # Asegúrate de que VITE_API_BASE_URL apunte a tu localhost
npm install
npm run dev
```

### 🔐 Configuración de User Secrets (Backend Local)

Para que el backend funcione localmente sin Docker, configura los secretos:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=accountpanel_db;User Id=sa;Password=TuPasswordFuerte!;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "SUPER_SECRET_KEY_MIN_64_CHARS_LONG_FOR_HMAC_SHA512"
dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "TU_CLIENT_SECRET"
# Configuración SMTP (Ejemplo Gmail/Mailtrap)
dotnet user-secrets set "SmtpSettings:Host" "smtp.mailtrap.io"
dotnet user-secrets set "SmtpSettings:Port" "587"
dotnet user-secrets set "SmtpSettings:Username" "TU_USER"
dotnet user-secrets set "SmtpSettings:Password" "TU_PASS"
dotnet user-secrets set "SmtpSettings:FromEmail" "no-reply@tuapp.com"
```

-----

## 🧪 Ejecutar las Pruebas

El proyecto incluye una suite robusta de pruebas unitarias y de integración. Las pruebas de integración utilizan `Testcontainers` o bases de datos efímeras en Docker para garantizar un entorno real.

### ✅ Backend

```bash
cd AccountPanel
dotnet test
```

### ✅ Frontend

```bash
cd client
npm run test:unit
```