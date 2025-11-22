using AccountPanel.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace AccountPanel.Infrastructure.Services;

public class FileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    /// <summary>
    /// Implementa la lógica de negocio para guardar un archivo en el sistema de archivos.
    /// </summary>
    /// <param name="fileStream">El stream del archivo a guardar.</param>
    /// <param name="fileName">El nombre del archivo a guardar.</param>
    /// <returns>La URL relativa que el frontend puede usar para mostrar el archivo.</returns>
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
    {
        // Define la ruta de la carpeta "uploads" dentro de wwwroot
        var uploadsPath = Path.Combine(env.WebRootPath, "uploads", "avatars");
        // Crea el directorio si no existe
        if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
        // Define la ruta completa del archivo
        var filePath = Path.Combine(uploadsPath, fileName);
        // Copia el stream del archivo al sistema de archivos
        await using (var outputStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outputStream);
        }
        // Devuelve la URL relativa que el frontend puede usar
        return $"/uploads/avatars/{fileName}";
    }

    /// <summary>
    /// Deletes a file from the file system based on its relative path.
    /// </summary>
    /// <param name="fileRoute">The relative path of the file to be deleted.</param>
    public void DeleteFile(string fileRoute)
    {
        if (string.IsNullOrEmpty(fileRoute)) return;

        // Quitamos la barra inicial si existe
        var relativePath = fileRoute.TrimStart('/');
        var filePath = Path.Combine(env.WebRootPath, relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
