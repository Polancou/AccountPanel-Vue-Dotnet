// Estos tipos deben coincidir con los DTOs de tu backend en .NET

export interface LoginUsuarioDto {
  email: string;
  password?: string; // Password puede ser opcional si usas login externo
}

export interface RegistroUsuarioDto {
  nombreCompleto: string;
  email: string;
  password?: string;
  numeroTelefono: string;
}

export interface PerfilUsuarioDto {
  id: number;
  nombreCompleto: string;
  email: string;
  numeroTelefono: string;
  rol: string;
  fechaRegistro: string; // O Date si lo parseas
}

export interface ActualizarPerfilDto {
  nombreCompleto: string;
  numeroTelefono: string;
}

// Puedes añadir ExternalLoginDto aquí también si lo necesitas
