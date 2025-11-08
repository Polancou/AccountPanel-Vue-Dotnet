// DTOs (Data Transfer Objects) para la aplicación

// DTO para el login de usuario
export interface LoginUsuarioDto {
  email: string;
  password?: string;
}
// DTO para el registro de usuario
export interface RegistroUsuarioDto {
  nombreCompleto: string;
  email: string;
  password?: string;
  numeroTelefono: string;
}
// DTO para el perfil de usuario
export interface PerfilUsuarioDto {
  id: number;
  nombreCompleto: string;
  email: string;
  numeroTelefono: string;
  rol: string;
  fechaRegistro: string;
}
// DTO para actualizar el perfil de usuario
export interface ActualizarPerfilDto {
  nombreCompleto: string;
  numeroTelefono: string;
}
// DTO para el cambio de contraseña
export interface CambiarPasswordDto {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}
// DTO para el token JWT
export interface JwtPayload {
  nameid: string;
  email: string;
  role: string;
  nbf: number;
  iat: number;
  exp: number;
  iss: string;
}