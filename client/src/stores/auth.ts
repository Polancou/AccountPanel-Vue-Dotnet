// Importaciones de Vue y Pinia
import { computed, ref } from 'vue' // 'ref' para estado reactivo, 'computed' para getters
import { defineStore } from 'pinia' // La función principal para definir un store
// Importaciones para llamadas a API y enrutamiento
import axios from 'axios' // Para hacer las peticiones HTTP
import { useRouter } from 'vue-router' // Para redirigir al usuario (ej. después del login)
// Importaciones para DTO
import type { LoginUsuarioDto, RegistroUsuarioDto, PerfilUsuarioDto, ActualizarPerfilDto, CambiarPasswordDto, JwtPayload } from "@/types/dto.ts"
// Importación de Toast de Vue Sonner
import { toast } from 'vue-sonner'
// Importación de jwtDecode para decodificar el token
import { jwtDecode } from 'jwt-decode'

/**
 * Store de autenticación usando Pinia.
 * Maneja el estado del usuario, acciones de login, logout y registro.
 */
export const useAuthStore = defineStore('auth', () => {

  // --- State ---

  // Definimos el token, usamos <string | null> para indicar que puede ser una cadena o nulo.
  const token = ref<string | null>(null)
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
      const response = await axios.post<{ token: string }>('/api/v1/auth/login', credentials)
      // Si la respuesta es exitosa
      const newToken = response.data.token
      // Actualiza el estado reactivo del token
      token.value = newToken
      // Actualiza el estado local con el rol
      setAuthState(newToken)
      // Muestra un toast de vue sonner indicando que la sesión se inició correctamente
      toast.success('Sesión iniciada correctamente');
      // Redirige a la página de perfil
      router.push({ name: 'profile' })
    } catch (err: any) {
      // Obtiene el mensaje de error
      const message = err.response?.data?.message || 'Error al iniciar sesión.'
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
      await axios.post('/api/v1/auth/register', userData)
      // Muestra un toast de vue sonner indicando que el registro fue exitoso
      toast.success('Registro exitoso', {
        description: '¡Ahora puedes iniciar sesión!'
      });
      // Indica que el registro fue exitoso
      return true
    } catch (err: any) {
      // Obtiene el mensaje de error
      const message = err.response?.data?.message || 'Error al registrarse.'
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
      const response = await axios.get<PerfilUsuarioDto>('/api/v1/profile/me', {
        headers: {
          Authorization: `Bearer ${token.value}` // Incluye el token en los headers
        }
      })
      // Actualiza el estado reactivo del perfil del usuario
      userProfile.value = response.data
    } catch (error: any) {
      // Actualiza el mensaje de error para el usuario
      error.value = error.response?.data?.message || 'Error al cargar el perfil.'
      // Si no está autorizado, cierra sesión localmente
      if (error.response?.status === 401) logoutLocally()
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
      // Incluye el token en la cabecera Authorization
      await axios.put('/api/v1/profile/me', profileData, {
        headers: {
          'Authorization': `Bearer ${token.value}`
        }
      });

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

    } catch (err: any) {
      // Obtiene el mensaje de error
      const message = err.response?.data?.message || 'Error al actualizar el perfil.'
      // Muestra un toast de vue sonner indicando que ocurrió un error
      toast.error(message)

      error.value = message;
      // Si el error es 401 (token inválido), cierra sesión localmente
      if (err.response?.status === 401) {
        logoutLocally();
      }
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
      const response = await axios.put('/api/v1/profile/change-password', passwordData, {
        headers: { 'Authorization': `Bearer ${token.value}` }
      });

      toast.success(response.data.message || 'Contraseña actualizada con éxito');
      return true;

    } catch (err: any) {
      const message = err.response?.data?.message || 'Error al cambiar la contraseña.';
      toast.error(message);
      error.value = message;
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  function setAuthState(newToken: string) {
    token.value = newToken
    try {
      // Decodifica el token para extraer el rol
      const decoded = jwtDecode<JwtPayload>(newToken)
      // Actualiza el estado local con el rol
      userRole.value = decoded.role
    } catch (e) {
      console.error("Error decodificando el token:", e)
      userRole.value = null
    }
  }

  return {
    // Exporta las props
    token, isLoading, error, isAuthenticated, userProfile, userRole, isAdmin,
    // Exporta los actions
    login, logout, register, fetchProfile, updateProfile, changePassword
  }
},
  {
    // Habilita la persistencia automática del store
    persist: true
  }) 
