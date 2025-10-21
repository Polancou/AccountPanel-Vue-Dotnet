using AutoMapper;
using BaseApp.Application.DTOs;
using BaseApp.Domain.Models;

namespace BaseApp.Application.Mappings;

/// <summary>
/// Define todas las configuraciones de mapeo de AutoMapper relacionadas con la entidad 'Usuario'.
/// Esta clase agrupa de forma lógica todas las transformaciones entre la entidad Usuario y sus DTOs.
/// Hereda de Profile, que es la clase base de AutoMapper para organizar la configuración.
/// </summary>
public class UserProfile : Profile
{
    public UserProfile()
    {
        // --- Mapeo de DTO de Registro a Entidad ---

        // Define el mapeo desde el DTO de registro (RegistroUsuarioDto) hacia la entidad (Usuario).
        // Esto es útil para convertir los datos de entrada de la API directamente en un objeto
        // que puede ser procesado por la capa de negocio.
        CreateMap<RegistroUsuarioDto, Usuario>()
            // Ignoramos la propiedad 'Password' del DTO, ya que no queremos mapearla directamente.
            // El hasheo y asignación de la contraseña se maneja de forma explícita en la capa de servicio
            // por razones de seguridad y claridad.
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());


        // --- Mapeo de Entidad a DTO de Perfil ---

        // <summary>
        // Define el mapeo desde la entidad (Usuario) hacia el DTO de perfil público (PerfilUsuarioDto).
        // Esto se utiliza para transformar el objeto de la base de datos en un objeto seguro
        // que puede ser devuelto al cliente, sin exponer datos sensibles como el PasswordHash.
        CreateMap<Usuario, PerfilUsuarioDto>()
            // Para propiedades que no coinciden directamente o requieren una transformación,
            // como el enum 'Rol' que queremos convertir a un string, especificamos una regla personalizada.
            .ForMember(
                dest => dest.Rol,
                opt => opt.MapFrom(src => src.Rol.ToString())
            );
    }
}