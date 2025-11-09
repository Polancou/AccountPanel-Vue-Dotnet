using AutoMapper;
using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AccountPanel.Application.Services;
using AccountPanel.Domain.Models;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;

namespace AccountPanel.Application.UnitTests;

/// <summary>
/// Contiene las pruebas unitarias para la clase AdminService.
/// </summary>
public class AdminServiceTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IMapper> _mockMapper;
    private readonly IAdminService _adminService;

    public AdminServiceTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockMapper = new Mock<IMapper>();

        _adminService = new AdminService(
            context: _mockDbContext.Object,
            mapper: _mockMapper.Object
        );
    }

    /// <summary>
    /// Prueba que el método GetUsersPaginatedAsync devuelva correctamente
    /// una lista mapeada y paginada de todos los usuarios.
    /// </summary>
    [Fact]
    public async Task GetUsersPaginatedAsync_ShouldReturnCorrectPage()
    {
        // --- Arrange (Preparar) ---
        var pageNumber = 1;
        var pageSize = 10;

        // 1. Crea tu lista completa de 150 usuarios falsos
        var allUsers = new List<Usuario>();
        for (int i = 0; i < 150; i++)
        {
            allUsers.Add(new Usuario($"User {i}", $"user{i}@test.com", "123", RolUsuario.User));
        }

        // 2. Crea la lista de 10 DTOs que esperas
        var expectedDtos = new List<PerfilUsuarioDto>();
        for (int i = 0; i < 10; i++)
        {
            expectedDtos.Add(new PerfilUsuarioDto { Email = $"user{i}@test.com" });
        }

        // 3. Configura el mock del DbContext
        // Moq.EntityFrameworkCore maneja 'Skip' y 'Take' por ti
        _mockDbContext.Setup(c => c.Usuarios)
            .ReturnsDbSet(allUsers);

        // 4. Configura el mock del Mapper
        // Espera recibir una lista de 10 usuarios
        _mockMapper.Setup(m => m.Map<List<PerfilUsuarioDto>>(It.Is<List<Usuario>>(list => list.Count == 10)))
            .Returns(expectedDtos);

        // --- Act (Actuar) ---
        var result = await _adminService.GetUsersPaginatedAsync(pageNumber: pageNumber, pageSize: pageSize);

        // --- Assert (Verificar) ---
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(expected: 10); // Verifica que los items mapeados sean 10
        result.TotalCount.Should().Be(expected: 150);  // Verifica el conteo total
        result.PageNumber.Should().Be(expected: 1);
        result.PageSize.Should().Be(expected: 10);

        // Verifica que el mapper fue llamado con una lista de 10
        _mockMapper.Verify(m => m.Map<List<PerfilUsuarioDto>>(It.Is<List<Usuario>>(list => list.Count == 10)), Times.Once);
    }
}