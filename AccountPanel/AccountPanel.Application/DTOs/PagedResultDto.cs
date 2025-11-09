namespace AccountPanel.Application.DTOs;

/// <summary>
/// DTO para la paginación de resultados.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Lista de elementos.
    /// </summary>
    public List<T> Items { get; set; } = [];
    /// <summary>
    /// Número total de elementos.
    /// </summary>
    public int TotalCount { get; set; }
    /// <summary>
    /// Tamaño de página.
    /// </summary>
    public int PageSize { get; set; }
    /// <summary>
    /// Número de página actual.
    /// </summary>
    public int PageNumber { get; set; }
    /// <summary>
    /// Número de páginas totales.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    /// <summary>
    /// Indica si hay una página anterior.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    /// <summary>
    /// Indica si hay una página siguiente.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
