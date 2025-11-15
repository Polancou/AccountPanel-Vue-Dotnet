using AutoMapper;
using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AccountPanel.Application.Services;
using AccountPanel.Domain.Models;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using AccountPanel.Application.Exceptions;

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


    #region GetUsersPaginatedAsync
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

    #endregion

    #region DeleteUserAsync
    /// <summary>
    /// Prueba que el servicio lanza una ValidationException si el admin
    /// intenta eliminarse a sí mismo.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WhenAdminDeletesSelf_ShouldThrowValidationException()
    {
        // --- Arrange (Preparar) ---
        var adminId = 5;
        var userIdToDelete = 5; // Mismo ID

        // --- Act (Actuar) ---
        Func<Task> act = async () => await _adminService.DeleteUserAsync(userIdToDelete, adminId);

        // --- Assert (Verificar) ---
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("No se puede eliminar el administrador.");

        _mockDbContext.Verify(c => c.Usuarios.Remove(It.IsAny<Usuario>()), Times.Never);
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Prueba que el servicio lanza una NotFoundException si el usuario
    /// a eliminar no se encuentra en la base de datos.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        // --- Arrange (Preparar) ---
        var adminId = 5;
        var userIdToDelete = 99; // ID que no existe

        // Configura FindAsync para que devuelva null, que es lo que prueba el servicio.
        _mockDbContext.Setup(c => c.Usuarios.FindAsync(userIdToDelete))
                      .ReturnsAsync((Usuario)null);

        // --- Act (Actuar) ---
        Func<Task> act = async () => await _adminService.DeleteUserAsync(userIdToDelete, adminId);

        // --- Assert (Verificar) ---
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró el usuario.");

        _mockDbContext.Verify(c => c.Usuarios.Remove(It.IsAny<Usuario>()), Times.Never);
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Prueba el "camino feliz": un admin elimina a otro usuario exitosamente.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WhenUserExists_ShouldDeleteUserSuccessfully()
    {
        // --- Arrange (Preparar) ---
        var adminId = 2; // Admin
        var userIdToDelete = 1; // Usuario a eliminar

        // Creamos el objeto usuario que esperamos que FindAsync devuelva
        var usuarioParaBorrar = new Usuario("Usuario a Borrar", "test@email.com", "123", RolUsuario.User);

        // 1. Configura FindAsync para que devuelva nuestro usuario
        _mockDbContext.Setup(c => c.Usuarios.FindAsync(userIdToDelete))
                      .ReturnsAsync(usuarioParaBorrar);

        // 2. Configura Remove para que acepte cualquier objeto Usuario (necesario para Moq)
        _mockDbContext.Setup(c => c.Usuarios.Remove(It.IsAny<Usuario>()));

        // --- Act (Actuar) ---
        await _adminService.DeleteUserAsync(userIdToDelete, adminId);

        // --- Assert (Verificar) ---

        // 1. Verificamos que se llamó a Remove con ESE usuario específico
        _mockDbContext.Verify(c => c.Usuarios.Remove(usuarioParaBorrar), Times.Once);

        // 2. Verificamos que se guardaron los cambios
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SetUserRoleAsync

    /// <summary>
    /// Prueba que al llamar al método SetUserRoleAsync
    /// el usuario no puede cambiar su propio rol.
    /// </summary>
    [Fact]
    public async Task SetUserRoleAsync_WhenAdminChangeSelf_ShouldThrowValidationException()
    {
        // --- Arrange (Preparar) ---
        var adminId = 2;
        var userIdToUpdate = 2;
        var newRole = RolUsuario.Admin;

        // Creamos el objeto usuario que esperamos que FindAsync devuelva
        var usuarioParaActualizar = new Usuario(nombreCompleto: "Usuario a Actualizar", email: "test@email.com", numeroTelefono: "123", rol: RolUsuario.User);

        // 1. Configura FindAsync para que devuelva nuestro usuario
        _mockDbContext.Setup(c => c.Usuarios.FindAsync(userIdToUpdate))
                      .ReturnsAsync(usuarioParaActualizar);
        // --- Act (Actuar) ---
        Func<Task> act = async () => await _adminService.SetUserRoleAsync(userIdToUpdate, newRole, adminId);

        // --- Assert (Verificar) ---
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("No se puede actualizar el rol del administrador.");

        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Prueba que al llamar al método SetUserRoleAsync
    /// se actualiza el rol del usuario correctamente.
    /// </summary>
    [Fact]
    public async Task SetUserRoleAsync_ShouldUpdateUserRoleSuccessfully()
    {
        // --- Arrange (Preparar) ---
        var adminId = 2; 
        var userIdToUpdate = 1;
        var newRole = RolUsuario.Admin;

        // Creamos el objeto usuario que esperamos que FindAsync devuelva
        var usuarioParaActualizar = new Usuario(nombreCompleto: "Usuario a Actualizar", email: "test@email.com", numeroTelefono: "123", rol: RolUsuario.User);

        // 1. Configura FindAsync para que devuelva nuestro usuario
        _mockDbContext.Setup(c => c.Usuarios.FindAsync(userIdToUpdate))
                      .ReturnsAsync(usuarioParaActualizar);
        // --- Act (Actuar) ---
        await _adminService.SetUserRoleAsync(userIdToUpdate, newRole, adminId);

        // --- Assert (Verificar) ---
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task SetUserRoleAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        // --- Arrange (Preparar) ---
        var adminId = 2;
        var userIdToUpdate = 99;
        var newRole = RolUsuario.Admin;

        // Configura FindAsync para que devuelva null, que es lo que prueba el servicio.
        _mockDbContext.Setup(c => c.Usuarios.FindAsync(userIdToUpdate))
                      .ReturnsAsync((Usuario)null);

        // --- Act (Actuar) ---
        Func<Task> act = async () => await _adminService.SetUserRoleAsync(userIdToUpdate, newRole, adminId);

        // --- Assert (Verificar) ---
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró el usuario.");

        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

}