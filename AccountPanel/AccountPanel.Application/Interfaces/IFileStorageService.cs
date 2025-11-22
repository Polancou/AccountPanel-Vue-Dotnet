namespace AccountPanel.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Carga un archivo desde el almacenamiento de archivos.
    /// </summary>
    /// <param name="fileName">Nombre del archivo a cargar.</param>
    /// <returns>El contenido del archivo.</returns>
    Task<string> SaveFileAsync(Stream fileStream, string fileName);
    void DeleteFile(string fileRoute);
}
