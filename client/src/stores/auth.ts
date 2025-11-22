import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import apiClient from '@/services/api';
import { useRouter } from 'vue-router'
import type {
  LoginUsuarioDto, RegistroUsuarioDto, PerfilUsuarioDto, ActualizarPerfilDto,
  CambiarPasswordDto, JwtPayload, ForgotPasswordDto, ResetPasswordDto,
  GoogleCredentialResponse, ApiErrorResponse
} from "@/types/dto"
import { toast } from 'vue-sonner'
import { jwtDecode } from 'jwt-decode'
import type { AxiosError } from 'axios';

/**
 * Store de autenticación usando Pinia.
 * Implementa la estrategia de seguridad: Access Token en memoria + Refresh Token en Cookie HttpOnly.
 */
export const useAuthStore = defineStore('auth', () => {

  // --- State ---

  // Access Token: Se mantiene solo en memoria (RAM). Se pierde al recargar.
  const token = ref<string | null>(null)

  // Estado de carga y errores
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)

  // Datos del usuario (Persistibles)
  const userProfile = ref<PerfilUsuarioDto | null>(null)
  const userRole = ref<string | null>(null)

  const router = useRouter()

  // --- Getters ---

  const isAuthenticated = computed<boolean>(() => !!token.value)
  const isAdmin = computed<boolean>(() => userRole.value === 'Admin')

  // --- Actions ---

  /**
   * Inicia sesión.
   * El backend debe devolver solo { accessToken }. La cookie refreshToken se setea automáticamente.
   */
  async function login(credentials: LoginUsuarioDto): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      const response = await apiClient.post<{ accessToken: string }>('/v1/auth/login', credentials)

      const { accessToken } = response.data
      setAuthState(accessToken)

      toast.success('Sesión iniciada correctamente');
      router.push({ name: 'profile' })
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al iniciar sesión.'
      toast.error(message);
      error.value = message
    } finally {
      isLoading.value = false
    }
  }

  async function register(userData: RegistroUsuarioDto): Promise<boolean> {
    isLoading.value = true
    error.value = null
    try {
      await apiClient.post('/v1/auth/register', userData)
      toast.success('Registro exitoso', {
        description: '¡Ahora puedes iniciar sesión!'
      });
      return true
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al registrarse.'
      toast.error(message);
      error.value = message
      return false
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Limpia el estado local (Memoria).
   */
  function logoutLocally(): void {
    token.value = null
    userProfile.value = null
    userRole.value = null
    isLoading.value = false
    error.value = null
  }

  /**
   * Cierra sesión en el servidor (borra cookie) y localmente.
   */
  async function logout(): Promise<void> {
    try {
      // Petición al backend para que elimine la cookie HttpOnly
      await apiClient.post('/v1/auth/logout');
    } catch (e) {
      // Ignoramos errores de red al salir
      console.warn("No se pudo notificar al servidor el logout", e);
    }

    logoutLocally()
    router.push({ name: 'login' })
  }

  async function fetchProfile(): Promise<void> {
    if (!token.value) return
    isLoading.value = true
    error.value = null
    try {
      const response = await apiClient.get<PerfilUsuarioDto>('/v1/profile/me')
      userProfile.value = response.data
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al cargar el perfil.'
      error.value = message
    } finally {
      isLoading.value = false
    }
  }

  async function updateProfile(profileData: ActualizarPerfilDto): Promise<boolean> {
    if (!token.value) {
      error.value = "No estás autenticado.";
      return false
    }
    isLoading.value = true;
    error.value = null;
    try {
      await apiClient.put('/v1/profile/me', profileData);
      if (userProfile.value) {
        userProfile.value = {
          ...userProfile.value,
          nombreCompleto: profileData.nombreCompleto,
          numeroTelefono: profileData.numeroTelefono
        };
      } else {
        await fetchProfile();
      }
      toast.success('Perfil actualizado correctamente')
      return true;
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al actualizar el perfil.'
      toast.error(message)
      error.value = message;
      return false;
    } finally {
      isLoading.value = false
    }
  }

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
      const message = axiosError.response?.data?.message || 'Error al cambiar la contraseña.';
      toast.error(message);
      error.value = message;
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  async function uploadAvatar(file: File) {
    if (!token.value) {
      toast.error("No estás autenticado.");
      return;
    }
    isLoading.value = true;
    error.value = null;

    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await apiClient.post<{ avatarUrl: string }>('/v1/profile/avatar', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
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
   * Establece el token en memoria y decodifica el rol.
   */
  function setAuthState(accessToken: string) {
    token.value = accessToken
    try {
      const decoded = jwtDecode<JwtPayload>(accessToken)
      userRole.value = decoded.role
    } catch (e) {
      console.error("Error decodificando el token:", e)
      userRole.value = null
    }
  }

  /**
   * Solicita un nuevo Access Token usando la cookie HttpOnly.
   */
  async function refreshAccessToken(): Promise<boolean> {
    try {
      // Petición sin body. El navegador envía la cookie 'refreshToken' automáticamente.
      const response = await apiClient.post<{ accessToken: string }>('/v1/auth/refresh')
      const { accessToken } = response.data
      setAuthState(accessToken)
      return true
    } catch (error) {
      console.error("No se pudo refrescar el token (Cookie inválida o expirada)", error)
      return false
    }
  }

  /**
   * Lógica de inicio: Si no hay token en memoria, intenta obtener uno via Cookie.
   */
  async function checkAuthOnStart(): Promise<void> {
    if (!token.value) {
      try {
        await refreshAccessToken(); // Si la cookie es válida, recuperamos la sesión
      } catch {
        // Si falla, el usuario permanece deslogueado, no es un error crítico
      }
    } else {
      // Validación extra: si por alguna razón el token persistió pero expiró
      try {
        const decoded = jwtDecode<JwtPayload>(token.value);
        const currentTime = Date.now() / 1000;
        if (decoded.exp < currentTime + 10) {
          await refreshAccessToken();
        }
      } catch {
        // Token corrupto
        token.value = null;
      }
    }
  }

  async function handleGoogleLogin(response: GoogleCredentialResponse) {
    isLoading.value = true
    error.value = null
    const idToken = response.credential;
    if (!idToken) {
      toast.error("No se recibió la credencial de Google.");
      isLoading.value = false;
      return;
    }
    try {
      const tokenResponse = await apiClient.post<{ accessToken: string }>('/v1/auth/external-login', {
        provider: 'Google',
        idToken: idToken
      })
      const { accessToken } = tokenResponse.data
      setAuthState(accessToken)
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

  async function forgotPassword(dto: ForgotPasswordDto): Promise<boolean> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await apiClient.post('/v1/auth/forgot-password', dto);
      toast.success(response.data.message || "Correo enviado.");
      return true;
    } catch (err: unknown) {
      const axiosError = err as AxiosError<ApiErrorResponse>;
      const message = axiosError.response?.data?.message || 'Error al enviar el correo.';
      toast.error(message);
      error.value = message;
      return false;
    } finally {
      isLoading.value = false;
    }
  }

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
    token, isLoading, error, isAuthenticated, userProfile, userRole, isAdmin,
    login, logout, logoutLocally, register, fetchProfile, updateProfile, changePassword, uploadAvatar,
    refreshAccessToken, handleGoogleLogin, forgotPassword, resetPassword, checkAuthOnStart
  }
},
  {
    // Configuración de Persistencia
    persist: {
      // IMPORTANTE: Solo guardamos datos de UI, nunca el Token
      pick: ['userProfile', 'userRole']
    }
  })
