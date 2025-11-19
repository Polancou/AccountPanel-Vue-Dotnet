# Etapa 1: Construcción
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar el archivo de solución y los proyectos
COPY ["AccountPanel.sln", "./"]
COPY ["AccountPanel.Api/AccountPanel.Api.csproj", "AccountPanel.Api/"]
COPY ["AccountPanel.Application/AccountPanel.Application.csproj", "AccountPanel.Application/"]
COPY ["AccountPanel.Domain/AccountPanel.Domain.csproj", "AccountPanel.Domain/"]
COPY ["AccountPanel.Infrastructure/AccountPanel.Infrastructure.csproj", "AccountPanel.Infrastructure/"]
COPY ["AccountPanel.Api.IntegrationTests/AccountPanel.Api.IntegrationTests.csproj", "AccountPanel.Api.IntegrationTests/"]
COPY ["AccountPanel.Application.UnitTests/AccountPanel.Application.UnitTests.csproj", "AccountPanel.Application.UnitTests/"]

# Restaurar dependencias
RUN dotnet restore "AccountPanel.sln"

# Copiar el resto del código fuente
COPY . .

# Compilar y publicar la API (Release)
WORKDIR "/src/AccountPanel.Api"
RUN dotnet publish "AccountPanel.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Runtime (Imagen final más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Crear carpeta para uploads (según tu FileStorageService)
# Esto asegura que los permisos sean correctos dentro del contenedor
RUN mkdir -p /app/wwwroot/uploads/avatars

COPY --from=build /app/publish .

# Exponer puerto 8080 (puerto por defecto en .NET 8/9 containers)
EXPOSE 8080

ENTRYPOINT ["dotnet", "AccountPanel.Api.dll"]