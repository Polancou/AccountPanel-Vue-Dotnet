# 🧩 AccountPanel: Aplicación Full-Stack de Panel de Cuentas

Un proyecto full-stack. Combina un backend en **.NET 9** con **Arquitectura Limpia** y un frontend **SPA (Single Page Application)** reactivo construido con **Vue.js 3**, **TypeScript** y **Tailwind CSS**.

---

## ✨ Stack Tecnológico

Este proyecto contiene tecnologías modernas y demandadas para el desarrollo web full-stack.

### ⚙️ **Backend (.NET / C#)**

- **.NET 9** (C# 13)
- **Arquitectura Limpia (Clean Architecture)**: separación estricta de responsabilidades (`Domain`, `Application`, `Infrastructure`, `Api`)
- **API RESTful** con versionado (`Asp.Versioning`)
- **Entity Framework Core 9** con SQLite para persistencia de datos
- **Autenticación JWT** para seguridad de endpoints
- **Autorización Basada en Roles (RBAC)** con claims y el atributo `[Authorize(Roles = "Admin")]`
- **Manejo de Subida de Archivos** con `IFormFile` para avatares de perfil.
- **Data Seeding** para la creación automática del usuario administrador al inicio
- **Pruebas Unitarias** (`xUnit`, `Moq`) para la lógica de negocio
- **Pruebas de Integración** (`WebApplicationFactory`) para los endpoints de la API
- **Servicios Externos**: login con Google (`Google.Apis.Auth`)
- **Inyección de Dependencias** (`Program.cs`)
- **Logging Estructurado** con `Serilog`
- **Mapeo de Objetos** con `AutoMapper`
- **Validación Avanzada** con `FluentValidation`

### 🖥️ **Frontend (Vue.js / TypeScript)**

- **Vue.js 3** (con Composition API y `<script setup>`)
- **Vite** como herramienta de construcción y servidor de desarrollo
- **TypeScript** para un tipado estático robusto
- **Vue Router** para enrutamiento del lado del cliente y guardias de navegación
- **Pinia** para la gestión de estado global
- **`pinia-plugin-persistedstate`** para persistir la sesión de autenticación en `localStorage`
- **Tailwind CSS v4** para diseño *utility-first* moderno y minimalista
- **Axios** para comunicación con la API
- **Validación de Formularios** en tiempo real con `VeeValidate` y `Zod`
- **Notificaciones (Toasts)** elegantes con `Vue-Sonner` para feedback de API.
- **Subida de Archivos de Avatar** con `FormData` y vista previa de imagen.
- **Componentes Reutilizables** (`BaseTable`, `BaseInput`, `BaseButton` con variantes).
- **UI y Rutas Condicionales** basadas en el rol del usuario (Admin vs User).
- **ESLint** y **Prettier** para la calidad y formato del código.

---

## 🚀 Cómo Empezar

Sigue estos pasos para configurar y ejecutar el proyecto completo localmente.

### 🔧 Prerrequisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) o superior.
- [Node.js](https://nodejs.org/) (versión 20+ recomendada) y npm.
- Un editor de código como Visual Studio, JetBrains Rider o VS Code.

---

### 1️⃣ Clonar el Repositorio

```bash
git clone [https://github.com/polancou/AccountPanel-Vue-Dotnet.git](https://github.com/polancou/AccountPanel-Vue-Dotnet.git)
cd AccountPanel-Vue-Dotnet
````

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

Establece los secretos necesarios (reemplaza los valores de ejemplo). **Las credenciales de AdminUser son usadas para el sembrado automático de la base de datos**:

```bash
# Claves de la aplicación
dotnet user-secrets set "Jwt:Key" "UNA_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA_GENERADA_POR_TI"
dotnet user-secrets set "AutoMapper:Key" "TU_CLAVE_DE_LICENCIA_DE_AUTOMAPPER"

# Credenciales de Google Auth
dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID_DE_GOOGLE.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "TU_CLIENT_SECRET_DE_GOOGLE"

# Credenciales del Administrador para el Seeding
dotnet user-secrets set "AdminUser:Email" "admin@tu-dominio.com"
dotnet user-secrets set "AdminUser:Password" "UnaContraseñaMuySegura123!"
```

Vuelve a la raíz del repositorio:

```bash
cd ../..
```

#### 📦 Restaurar Dependencias del Backend

```bash
dotnet restore AccountPanel/AccountPanel.sln
```

#### 🗃️ Crear la Base de Datos y el Admin

1.  **Crear las Migraciones:** Ejecuta el siguiente comando desde la raíz del repositorio para crear todas las migraciones necesarias (incluyendo la del avatar):

    ```bash
    dotnet ef migrations add InitialCreate --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

    *Si ya existe `InitialCreate`, añade la siguiente:*

    ```bash
    dotnet ef migrations add AddAvatarUrlToUsuario --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

2.  **Aplicar las Migraciones:** Esto creará el archivo `sampleDb.db`:

    ```bash
    dotnet ef database update --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
    ```

3.  **Sembrar el Admin:** La lógica en `Program.cs` creará automáticamente el usuario administrador (usando tus secretos) la **primera vez que ejecutes el backend**.

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

**Configuración del Proxy:**
El archivo `client/vite.config.ts` está configurado para usar un proxy que redirige las peticiones `/api` y `/uploads` a `http://localhost:5272`. Asegúrate de que esto coincida con el perfil `http` en tu `launchSettings.json`.

-----

## 🏃‍♂️ Ejecución en Desarrollo

Para trabajar en el proyecto, abre **dos terminales** simultáneamente en la raíz del repositorio.

### 🧩 Terminal 1: Ejecutar el Backend

```bash
dotnet run --project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

*(La API estará disponible en `http://localhost:5272` y `https://localhost:7092`. La primera ejecución aplicará migraciones y creará el usuario admin)*

### 🧩 Terminal 2: Ejecutar el Frontend

```bash
cd client
npm run dev
```

*(La aplicación Vue estará disponible en `http://localhost:5173` (o un puerto similar))*

Abre la dirección del frontend (`http://localhost:5173`) en tu navegador para usar la aplicación. Puedes iniciar sesión con las credenciales de administrador que definiste en los secretos.

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
Consulta el archivo [`LICENSE`](https://www.google.com/search?q=./LICENSE) para más detalles.
