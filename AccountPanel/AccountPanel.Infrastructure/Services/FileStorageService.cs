using AccountPanel.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AccountPanel.Infrastructure.Services;

public class LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : IFileStorageService
{
    private const string ContainerName = "uploads/avatars";

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
    {
        // 1. Definir la ruta física en el servidor
        var uploadsPath = Path.Combine(env.WebRootPath, ContainerName);

        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        var filePath = Path.Combine(uploadsPath, fileName);

        // 2. Guardar el archivo físicamente
        using (var outputStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outputStream);
        }

        // 3. Retornar la URL relativa
        return $"/{ContainerName}/{fileName}";
    }
    
    /// <summary>
    /// Deletes a file from the file system based on its relative path.
    /// </summary>
    /// <param name="fileRoute">The relative path of the file to be deleted.</param>
    public Task DeleteFileAsync(string fileRoute)
    {
        if (string.IsNullOrEmpty(fileRoute)) return Task.CompletedTask;

        // Convertir la URL relativa ("/uploads/avatars/foto.jpg") a ruta física
        // Quitamos la barra inicial y reemplazamos los separadores web por los del SO
        var relativePath = fileRoute.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var filePath = Path.Combine(env.WebRootPath, relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
