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
- **Pruebas Unitarias** (`xUnit`, `Moq`)
- **Pruebas de Integración** (`WebApplicationFactory`)
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
- **ESLint** y **Prettier** para la calidad y formato del código

---

## 🚀 Cómo Empezar

Sigue estos pasos para configurar y ejecutar el proyecto completo localmente.

### 🔧 Prerrequisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) o superior
- [Node.js](https://nodejs.org/) (versión 20+ recomendada)
- Un editor de código como Visual Studio, JetBrains Rider o VS Code

---

### 1️⃣ Clonar el Repositorio

```bash
git clone https://github.com/polancou/AccountPanel-Vue-Dotnet.git
cd AccountPanel-Vue-Dotnet
```

---

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
dotnet user-secrets set "Jwt:Key" "UNA_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA_GENERADA_POR_TI"
dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID_DE_GOOGLE.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "TU_CLIENT_SECRET_DE_GOOGLE"
dotnet user-secrets set "AutoMapper:Key" "TU_CLAVE_DE_LICENCIA_DE_AUTOMAPPER"
```

Vuelve a la raíz del repositorio:

```bash
cd ../..
```

#### 📦 Restaurar Dependencias del Backend

```bash
dotnet restore AccountPanel/AccountPanel.sln
```

#### 🗃️ Crear la Base de Datos (Migraciones)

Ejecuta el siguiente comando desde la raíz del repositorio para crear la migración inicial:

```bash
dotnet ef migrations add InitialCreate --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

Aplica la migración para crear la base de datos `sampleDb.db`:

```bash
dotnet ef database update --project AccountPanel/AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj --startup-project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

---

### 3️⃣ Configurar el Frontend (Vue.js)

Navega al directorio del cliente:

```bash
cd client
```

Instala las dependencias de npm:

```bash
npm install
```

Configura el **proxy**:  
El archivo `client/vite.config.ts` redirige las peticiones `/api` a `http://localhost:5272`.  
Asegúrate de que coincida con el perfil `http` en tu `launchSettings.json`.

---

## 🏃‍♂️ Ejecución en Desarrollo

Para trabajar en el proyecto, abre **dos terminales** simultáneamente en la raíz del repositorio.

### 🧩 Terminal 1: Ejecutar el Backend

```bash
dotnet run --project AccountPanel/AccountPanel.Api/AccountPanel.Api.csproj
```

La API estará disponible en:
- `http://localhost:5272`
- `https://localhost:7092`

### 🧩 Terminal 2: Ejecutar el Frontend

```bash
cd client
npm run dev
```

La aplicación Vue estará disponible en:  
👉 [http://localhost:5173](http://localhost:5173) (o un puerto similar)

Abre esta dirección en tu navegador para usar la aplicación.

---

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

---

## 📜 Licencia

Este proyecto está bajo la **Licencia MIT**.  
Consulta el archivo [`LICENSE`](./LICENSE) para más detalles.
