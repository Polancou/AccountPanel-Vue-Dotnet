using AutoMapper;
using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AccountPanel.Application.Services;
using AccountPanel.Domain.Models;
using FluentAssertions;
using Moq;

namespace AccountPanel.Application.UnitTests;

/// <summary>
/// Contiene las pruebas unitarias para la clase ProfileService.
/// El objetivo es verificar la lógica de negocio de ProfileService de forma aislada,
/// simulando sus dependencias (IApplicationDbContext e IMapper) para asegurar
/// que solo probamos la lógica interna del servicio.
/// </summary>
public class ProfileServiceTests
{
    // Mocks para los contratos (interfaces) que ProfileService necesita.
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IMapper> _mockMapper;

    // La instancia real del servicio que vamos a probar.
    private readonly IProfileService _profileService;

    public ProfileServiceTests()
    {
        // --- Arrange Global (Preparación antes de cada prueba) ---
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockMapper = new Mock<IMapper>();

        // Se inyectan los mocks en la implementación concreta del servicio.
        _profileService = new ProfileService(
            context: _mockDbContext.Object,
            mapper: _mockMapper.Object
        );
    }

    #region Pruebas para GetProfileByIdAsync

    /// <summary>
    /// Prueba el "camino feliz": cuando se solicita un usuario que existe,
    /// el servicio debe encontrarlo y mapearlo a un DTO de perfil.
    /// </summary>
    [Fact]
    public async Task GetProfileByIdAsync_WhenUserExists_ShouldReturnProfileDto()
    {
        // --- Arrange (Preparar) ---
        var userId = 1;
        var usuario = new Usuario(
            nombreCompleto: "Usuario de Prueba",
            email: "test@email.com",
            numeroTelefono: "123",
            rol: RolUsuario.User);
        var perfilDto = new PerfilUsuarioDto { Id = userId, NombreCompleto = "Usuario de Prueba" };
        var usuarios = new List<Usuario> { usuario };

        // 1. Se configura el mock del DbContext para que devuelva el usuario cuando se le busque por ID.
        // Usamos FindAsync, que es lo que el servicio utiliza internamente.
        _mockDbContext.Setup(expression: c => c.Usuarios.FindAsync(userId)).ReturnsAsync(value: usuario);

        // 2. Se configura el mock de AutoMapper para que devuelva el DTO esperado cuando mapee el usuario.
        _mockMapper.Setup(expression: m => m.Map<PerfilUsuarioDto>(usuario)).Returns(value: perfilDto);

        // --- Act (Actuar) ---
        var result = await _profileService.GetProfileByIdAsync(userId: userId);

        // --- Assert (Verificar) ---
        // Se comprueba que el resultado no sea nulo y que sea el DTO que configuramos.
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectation: perfilDto);
    }

    /// <summary>
    /// Prueba el caso de error: cuando se solicita un usuario que no existe,
    /// el servicio debe devolver null.
    /// </summary>
    [Fact]
    public async Task GetProfileByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // --- Arrange (Preparar) ---
        var userId = 99; // Un ID que sabemos que no existirá.

        // Se configura el mock del DbContext para que devuelva null al buscar el usuario.
        _mockDbContext.Setup(expression: c => c.Usuarios.FindAsync(userId)).ReturnsAsync(value: (Usuario)null);

        // --- Act (Actuar) ---
        var result = await _profileService.GetProfileByIdAsync(userId: userId);

        // --- Assert (Verificar) ---
        // Se comprueba que el resultado sea nulo.
        result.Should().BeNull();
    }

    #endregion

    #region Pruebas para ActualizarPerfilAsync

    /// <summary>
    /// Prueba el "camino feliz": si el usuario existe, el servicio debe actualizar
    /// sus propiedades y guardar los cambios.
    /// </summary>
    [Fact]
    public async Task ActualizarPerfilAsync_WhenUserExists_ShouldUpdateAndSaveChanges()
    {
        // --- Arrange (Preparar) ---
        var userId = 1;
        var usuario = new Usuario(nombreCompleto: "Nombre Original",
            email: "test@email.com",
            numeroTelefono: "123",
            rol: RolUsuario.User);
        var updateDto = new ActualizarPerfilDto { NombreCompleto = "Nombre Actualizado", NumeroTelefono = "987" };

        // Se configura el mock para que devuelva el usuario existente.
        _mockDbContext.Setup(expression: c => c.Usuarios.FindAsync(userId)).ReturnsAsync(value: usuario);

        // --- Act (Actuar) ---
        var result = await _profileService.ActualizarPerfilAsync(userId: userId,
            perfilDto: updateDto);

        // --- Assert (Verificar) ---
        // 1. Se comprueba que el método devuelva 'true', indicando éxito.
        result.Should().BeTrue();

        // 2. Se comprueba que las propiedades del objeto 'usuario' en memoria hayan sido actualizadas.
        usuario.NombreCompleto.Should().Be(expected: updateDto.NombreCompleto);
        usuario.NumeroTelefono.Should().Be(expected: updateDto.NumeroTelefono);

        // 3. Se verifica que se intentó guardar los cambios en la base de datos,
        // lo cual es una parte crucial de la lógica del servicio.
        _mockDbContext.Verify(expression: c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            times: Times.Once);
    }

    /// <summary>
    /// Prueba el caso de error: si se intenta actualizar un usuario que no existe,
    /// el servicio debe devolver 'false'.
    /// </summary>
    [Fact]
    public async Task ActualizarPerfilAsync_WhenUserDoesNotExist_ShouldReturnFalse()
    {
        // --- Arrange (Preparar) ---
        var userId = 99;
        var updateDto = new ActualizarPerfilDto { NombreCompleto = "Test", NumeroTelefono = "123" };

        // Se configura el mock para que no encuentre al usuario.
        _mockDbContext.Setup(expression: c => c.Usuarios.FindAsync(userId)).ReturnsAsync(value: (Usuario)null);

        // --- Act (Actuar) ---
        var result = await _profileService.ActualizarPerfilAsync(userId: userId,
            perfilDto: updateDto);

        // --- Assert (Verificar) ---
        // 1. Se comprueba que el método devuelva 'false'.
        result.Should().BeFalse();

        // 2. Se verifica que NUNCA se intentó guardar cambios, ya que no se encontró ningún usuario.
        _mockDbContext.Verify(expression: c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            times: Times.Never);
    }

    #endregion

    #region Pruebas para CambiarPasswordAsync

    /// <summary>
    /// Prueba el "camino feliz": un usuario existente con la contraseña antigua correcta
    /// debería poder actualizar su contraseña.
    /// </summary>
    [Fact]
    public async Task CambiarPasswordAsync_WhenOldPasswordIsCorrect_ShouldUpdatePassword()
    {
        // --- Arrange (Preparar) ---
        var userId = 1;
        var oldPassword = "PasswordAntigua123!";
        var newPassword = "PasswordNueva456!";
        var user = new Usuario("Test User", "test@email.com", "123", RolUsuario.User);

        // Establece el hash de la contraseña antigua en la entidad de usuario
        user.EstablecerPasswordHash(BCrypt.Net.BCrypt.HashPassword(oldPassword));

        var dto = new CambiarPasswordDto
        {
            OldPassword = oldPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        // Configura el mock para que devuelva el usuario
        _mockDbContext.Setup(c => c.Usuarios.FindAsync(userId)).ReturnsAsync(user);

        // --- Act (Actuar) ---
        var result = await _profileService.CambiarPasswordAsync(userId, dto);

        // --- Assert (Verificar) ---
        // 1. Verifica que la operación fue exitosa
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Contraseña actualizada exitosamente.");

        // 2. Verifica que el hash de la contraseña en la entidad fue actualizado
        user.PasswordHash.Should().NotBe(null);
        BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash).Should().BeTrue();

        // 3. Verifica que se llamó a SaveChanges
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Prueba el caso de error: si el usuario proporciona una contraseña antigua incorrecta,
    /// la operación debe fallar y no debe guardar cambios.
    /// </summary>
    [Fact]
    public async Task CambiarPasswordAsync_WhenOldPasswordIsIncorrect_ShouldReturnFail()
    {
        // --- Arrange (Preparar) ---
        var userId = 1;
        var correctOldPassword = "PasswordAntigua123!";
        var wrongOldPassword = "PasswordEquivocadaXXX";

        var user = new Usuario("Test User", "test@email.com", "123", RolUsuario.User);
        user.EstablecerPasswordHash(BCrypt.Net.BCrypt.HashPassword(correctOldPassword));

        var dto = new CambiarPasswordDto
        {
            OldPassword = wrongOldPassword, // <- Contraseña incorrecta
            NewPassword = "newPassword456!",
            ConfirmPassword = "newPassword456!"
        };

        _mockDbContext.Setup(c => c.Usuarios.FindAsync(userId)).ReturnsAsync(user);

        // --- Act (Actuar) ---
        var result = await _profileService.CambiarPasswordAsync(userId, dto);

        // --- Assert (Verificar) ---
        // 1. Verifica que la operación falló con el mensaje correcto
        result.Success.Should().BeFalse();
        result.Message.Should().Be(expected: "La contraseña actual es incorrecta.");

        // 2. Verifica que NO se llamó a SaveChanges
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}