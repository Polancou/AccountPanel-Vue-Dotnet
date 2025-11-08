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
    /// Prueba que el método GetAllUsersAsync devuelva correctamente
    /// una lista mapeada de todos los usuarios.
    /// </summary>
    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllUsersAsDtos()
    {
        // --- Arrange (Preparar) ---
        // 1. Crea los datos falsos del dominio
        var usuarios = new List<Usuario>
        {
            new Usuario(nombreCompleto: "Admin User", email: "admin@test.com", numeroTelefono: "111", rol: RolUsuario.Admin),
            new Usuario(nombreCompleto: "Regular User", email: "user@test.com", numeroTelefono: "222", rol: RolUsuario.User)
        };

        // 2. Crea los DTOs que esperamos que el Mapper devuelva
        var perfilesDto = new List<PerfilUsuarioDto>
        {
            new PerfilUsuarioDto { Id = 1, Email = "admin@test.com", Rol = "Admin" },
            new PerfilUsuarioDto { Id = 2, Email = "user@test.com", Rol = "User" }
        };

        // 3. Configura el mock del DbContext
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(usuarios);

        // 4. Configura el mock del Mapper
        _mockMapper.Setup(m => m.Map<IEnumerable<PerfilUsuarioDto>>(It.IsAny<List<Usuario>>()))
            .Returns(perfilesDto);

        // --- Act (Actuar) ---
        var result = await _adminService.GetAllUsersAsync();

        // --- Assert (Verificar) ---
        // 1. Verifica que el resultado no sea nulo y tenga 2 elementos
        result.Should().NotBeNull();
        result.Should().HaveCount(expected: 2);

        // 2. Verifica que la propiedad Usuarios del DbContext fue accedida
        _mockDbContext.Verify(c => c.Usuarios, Times.Once);

        // 3. Verifica que el Mapper fue llamado
        _mockMapper.Verify(m => m.Map<IEnumerable<PerfilUsuarioDto>>(usuarios), Times.Once);
    }
}