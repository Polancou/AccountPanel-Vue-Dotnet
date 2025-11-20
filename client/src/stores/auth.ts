// Importaciones de Vue y Pinia
import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
// Importaciones para llamadas a API y enrutamiento
import apiClient from '@/services/api';
import { useRouter } from 'vue-router'
// Importaciones para DTO
import type {
  LoginUsuarioDto, RegistroUsuarioDto, PerfilUsuarioDto, ActualizarPerfilDto,
  CambiarPasswordDto, JwtPayload, TokenResponseDto, ForgotPasswordDto, ResetPasswordDto,
  GoogleCredentialResponse, ApiErrorResponse
} from "@/types/dto.ts"
// Importación de Toast de Vue Sonner
import { toast } from 'vue-sonner'
// Importación de jwtDecode para decodificar el token
import { jwtDecode } from 'jwt-decode'
import type { AxiosError } from 'axios';

/**
 * Store de autenticación usando Pinia.
 * Maneja el estado del usuario, acciones de login, logout y registro.
 */
export const useAuthStore = defineStore('auth', () => {

  // --- State ---

  // Definimos el token,
  const token = ref<string | null>(null)
  // Definimos el token de refresco
  const refreshToken = ref<string | null>(null)
  // Para mostrar indicadores de carga en la UI mientras se hacen llamadas a la API.
  const isLoading = ref<boolean>(false)
  // Para mostrar mensajes de error al usuario.
  const error = ref<string | null>(null)
  // Estado para almacenar el perfil del usuario
  const userProfile = ref<PerfilUsuarioDto | null>(null)
  // Estado para definir el rol del usuario
  const userRole = ref<string | null>(null)

  // Obtenemos una instancia del router para usarla en las acciones.
  const router = useRouter()

  // --- Getters ---

  // Un getter computado que devuelve true si existe un token, false si no.
  const isAuthenticated = computed<boolean>(() => !!token.value)
  // Un getter computado que determina si el usuario es un administrador
  const isAdmin = computed<boolean>(() => userRole.value === 'Admin')

  // --- Actions ---

  /**
   * Acción para iniciar sesión del usuario
   * @param credentials Credenciales de login (email y password)
   */
  async function login(credentials: LoginUsuarioDto): Promise<void> {
    // Inicia el estado de carga
    isLoading.value = true
    // Limpia errores previos
    error.value = null
    try {
      // Llamada a la API (POST a /api/v1/auth/login)
      const response = await apiClient.post<TokenResponseDto>('/v1/auth/login', credentials)
      // Obtenemos los tokens de acceso y refresco
      const { accessToken, refreshToken: newRefreshToken } = response.data
      // Guardamos ambos tokens
      setAuthState(accessToken, newRefreshToken)
      // Muestra un toast de vue sonner indicando que la sesión se inició correctamente
      toast.success('Sesión iniciada correctamente');
      // Redirige a la página de perfil
      router.push({ name: 'profile' })
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      // Obtiene el mensaje de error
      const message = axiosError.response?.data?.message || 'Error al iniciar sesión.'
      // Muestra un toast de vue sonner indicando que ocurrió un error
      toast.error(message);
      // Cuando ocurre una exception, se actualiza el mensaje del usuario
      error.value = message
    } finally {
      // Finaliza el loop de carga, independientemente del resultado
      isLoading.value = false
    }
  }

  /**
   * Acción para registrar usuario
   * @param userData Datos del usuario para registro
   * @returns boolean que indica si el registro fue exitoso
   */
  async function register(userData: RegistroUsuarioDto): Promise<boolean> {
    // Inicia el estado de carga
    isLoading.value = true
    // Limpia errores previos
    error.value = null
    try {
      // Llamada a la API (/api/v1/auth/register)
      await apiClient.post('/v1/auth/register', userData)
      // Muestra un toast de vue sonner indicando que el registro fue exitoso
      toast.success('Registro exitoso', {
        description: '¡Ahora puedes iniciar sesión!'
      });
      // Indica que el registro fue exitoso
      return true
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      // Obtiene el mensaje de error
      const message = axiosError.response?.data?.message || 'Error al registrarse.'
      // Muestra un toast de vue sonner indicando que ocurrió un error
      toast.error(message);
      // Cuando ocurre una exception, se actualiza el mensaje del usuario
      error.value = message
      // Indica que el registro falló
      return false
    } finally {
      // Finaliza el loop de carga, independientemente del resultado
      isLoading.value = false
    }
  }

  /**
   * Función interna para limpiar el estado local al hacer logout
   */
  function logoutLocally(): void {
    // Reinicia el valor del token
    token.value = null
    // Reinicia el estado de perfil
    userProfile.value = null
    // Reinicia el estado de refresco
    refreshToken.value = null
    // Reinicia el estado de la carga
    isLoading.value = false
    // Reinicia el mensaje de error
    error.value = null
  }

  /**
   * Acción para cerrar la sesión del usuario
   */
  async function logout(): Promise<void> {
    logoutLocally() // Limpia el estado actual
    // Redirige a la página de login
    router.push({ name: 'login' })
  }

  /**
   * Acción para obtener el perfil del usuario autenticado
   */
  async function fetchProfile(): Promise<void> {
    // Solo intenta obtener el perfil si hay un token
    if (!token.value) return
    // Inicia el estado de carga
    isLoading.value = true
    // Limpia errores previos
    error.value = null
    try {
      // Llamada a la API para obtener el perfil del usuario
      const response = await apiClient.get<PerfilUsuarioDto>('/v1/profile/me')
      // Actualiza el estado reactivo del perfil del usuario
      userProfile.value = response.data
    } catch (err: unknown) {
      // Actualiza el mensaje de error para el usuario
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al actualizar el perfil.'
      toast.error(message)
      error.value = message;
    } finally {
      // Finaliza el loop de carga
      isLoading.value = false
    }
  }

  /**
   * Actualiza los datos del perfil del usuario autenticado en la API.
   * @param profileData - DTO con los nuevos datos (nombre, teléfono).
   * @returns boolean - True si la actualización fue exitosa, false si falló.
   */
  async function updateProfile(profileData: ActualizarPerfilDto): Promise<boolean> {
    // Verifica si hay token antes de intentar la llamada
    if (!token.value) {
      error.value = "No estás autenticado.";
      return false
    }

    isLoading.value = true;
    error.value = null; // Limpia errores previos
    try {
      // Realiza la petición PUT al endpoint /api/v1/profile/me
      await apiClient.put('/v1/profile/me', profileData);
      // Si la petición PUT fue exitosa:
      // Actualiza el estado local del perfil con los nuevos datos.
      if (userProfile.value) {
        userProfile.value = {
          ...userProfile.value, // Mantiene los datos existentes (ID, Email, Rol, Fecha...)
          nombreCompleto: profileData.nombreCompleto, // Actualiza el nombre
          numeroTelefono: profileData.numeroTelefono // Actualiza el teléfono
        };
      } else {
        // Se llama el fetchProfile para obtener el estado completo y actualizado.
        await fetchProfile();
      }

      // Muestra un toast de vue sonner indicando que el perfil se actualizó correctamente
      toast.success('Perfil actualizado correctamente')
      // Indica éxito
      return true;

    } catch (err: unknown) {
      // Obtiene el mensaje de error
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al actualizar el perfil.'
      // Muestra un toast de vue sonner indicando que ocurrió un error
      toast.error(message)

      error.value = message;
      // Si el error es 401 (token inválido), cierra sesión localmente

      return false; // Indica fallo
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Función para cambiar la contraseña del usuario autenticado.
   * @param passwordData - DTO con la nueva contraseña.
   * @returns boolean - True si la operación fue exitosa, false si falló.
   */
  async function changePassword(passwordData: CambiarPasswordDto): Promise<boolean> {
    if (!token.value) {
      toast.error("No estás autenticado.");
      return false;
    }

    isLoading.value = true;
    error.value = null;
    try {
      const response = await apiClient.put('/v1/profile/change-password', passwordData);

      toast.success(response.data.message || 'Contraseña actualizada con éxito');
      return true;

    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al actualizar el perfil.'
      toast.error(message);
      error.value = message;
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Sube un nuevo avatar para el usuario.
   * @param file El archivo que llega desde el input.
   */
  async function uploadAvatar(file: File) {
    if (!token.value) {
      toast.error("No estás autenticado.");
      return;
    }

    isLoading.value = true;
    error.value = null;

    // 1. Prepara el FormData
    const formData = new FormData();
    // El debe ser el mismo que el nombre del param en el backend
    formData.append('file', file);

    try {
      // 2. Envía la petición POST como 'multipart/form-data'
      const response = await apiClient.post<{ avatarUrl: string }>('/v1/profile/avatar', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
      // 3. Actualiza el estado local con la nueva URL
      if (userProfile.value) {
        userProfile.value.avatarUrl = response.data.avatarUrl;
      }
      toast.success("Foto de perfil actualizada.");

    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al subir la imagen.';
      toast.error(message);
      error.value = message;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Función interna para establecer el estado de autenticación
   */
  function setAuthState(accessToken: string, newRefreshToken?: string | null) {
    // Actualiza el estado local con el token
    token.value = accessToken
    try {
      // Decodifica el token para extraer el rol y el nombre del usuario
      const decoded = jwtDecode<JwtPayload>(accessToken)
      userRole.value = decoded.role
    } catch (e) {
      console.error("Error decodificando el token:", e)
      userRole.value = null
    }

    // Si nos pasan un nuevo refresh token, lo guardamos
    // (si es undefined, no cambia el valor existente)
    if (newRefreshToken !== undefined) {
      refreshToken.value = newRefreshToken
    }
  }

  /**
   * Intenta refrescar el token de acceso usando el refresh token.
   * @returns boolean - True si fue exitoso, false si falló.
   */
  async function refreshAccessToken(): Promise<boolean> {
    if (!refreshToken.value) {
      return false // No hay refresh token, no se puede refrescar
    }

    try {
      const response = await apiClient.post<TokenResponseDto>('/v1/auth/refresh', {
        refreshToken: refreshToken.value
      })

      const { accessToken, refreshToken: newRefreshToken } = response.data
      // Guardamos los nuevos tokens
      setAuthState(accessToken, newRefreshToken)
      return true
    } catch (error) {
      console.error("No se pudo refrescar el token", error)
      // Si el refresh falla (ej. token expirado), cerramos sesión
      logout()
      return false
    }
  }

  /**
   * Maneja el callback de Google Login.
   * @param response El objeto de credenciales devuelto por Google.
   */
  async function handleGoogleLogin(response: GoogleCredentialResponse) {
    isLoading.value = true
    error.value = null

    const idToken = response.credential; // El IdToken se llama 'credential'
    if (!idToken) {
      toast.error("No se recibió la credencial de Google.");
      isLoading.value = false;
      return;
    }

    try {
      // 1. Llama al endpoint del backend
      const tokenResponse = await apiClient.post<TokenResponseDto>('/v1/auth/external-login', {
        provider: 'Google',
        idToken: idToken
      })

      // 2. Guarda los tokens (Access + Refresh)
      const { accessToken, refreshToken: newRefreshToken } = tokenResponse.data
      setAuthState(accessToken, newRefreshToken)

      toast.success('Sesión iniciada con Google');
      router.push({ name: 'profile' })

    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al iniciar sesión con Google.'
      toast.error(message);
      error.value = message
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Solicita un email para restablecer la contraseña.
   */
  async function forgotPassword(dto: ForgotPasswordDto): Promise<boolean> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await apiClient.post('/v1/auth/forgot-password', dto);
      toast.success(response.data.message || "Correo enviado.");
      return true;
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al restablecer la contraseña.';
      toast.error(message);
      error.value = message;
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Restablece la contraseña usando un token.
   */
  async function resetPassword(dto: ResetPasswordDto): Promise<boolean> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await apiClient.post('/v1/auth/reset-password', dto);
      toast.success(response.data.message || "Contraseña restablecida.");
      return true;
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al restablecer la contraseña.';
      toast.error(message);
      error.value = message;
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  return {
    // Exporta las props
    token, isLoading, error, isAuthenticated, userProfile, userRole, isAdmin, refreshToken,
    // Exporta los actions
    login, logout, register, fetchProfile, updateProfile, changePassword, uploadAvatar,
    refreshAccessToken, handleGoogleLogin, forgotPassword, resetPassword
  }
},
  {
    // Habilita la persistencia automática del store
    persist: true
  })
