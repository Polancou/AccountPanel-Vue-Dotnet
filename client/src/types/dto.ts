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
  avatarUrl?: string;
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
// DTO para la paginación de resultados
export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
// DTO para el token de acceso y refresco
export interface TokenResponseDto {
  accessToken: string;
  refreshToken: string;
}

// DTO para actualizar el rol de un usuario (debería ir al final del archivo)
export interface ActualizarRolUsuarioDto {
  rol: string; // "Admin" o "User"
}

// DTO para solicitar el reseteo de contraseña
export interface ForgotPasswordDto {
  email: string;
}

// DTO para ejecutar el reseteo de contraseña
export interface ResetPasswordDto {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

// Tipo para la respuesta de Google
export interface GoogleCredentialResponse {
  credential: string;
}

export interface AdminUserQueryParams {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  rol?: string | number;
}

export interface ApiErrorResponse {
  message: string;
  statusCode: number;
  traceId: string;
}
