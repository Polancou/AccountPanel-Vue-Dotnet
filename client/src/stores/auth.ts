// Importaciones de Vue y Pinia
import {computed, ref} from 'vue' // 'ref' para estado reactivo, 'computed' para getters
import {defineStore} from 'pinia' // La función principal para definir un store
// Importaciones para llamadas a API y enrutamiento
import axios from 'axios' // Para hacer las peticiones HTTP
import {useRouter} from 'vue-router' // Para redirigir al usuario (ej. después del login)
// Importaciones para DTO
import type {LoginUsuarioDto, RegistroUsuarioDto} from "@/types/dto.ts";

// Define el ID único del store. Por convención, se usa 'auth'.
export const useAuthStore = defineStore('auth', () => {
// --- State ---

  // El token JWT. Lo inicializamos intentando leerlo desde localStorage.
  // Si existe un token guardado, el usuario ya podría estar logueado.
  // Usamos <string | null> para indicar a TypeScript que puede ser una cadena o nulo.
  const token = ref<string | null>(localStorage.getItem('authToken'))
  // Aplicamos lo mismo para el correo del usuario
  const userEmail = ref<string | null>(localStorage.getItem('userEmail'))
  // Para mostrar indicadores de carga en la UI mientras se hacen llamadas a la API.
  const isLoading = ref<boolean>(false)
  // Para mostrar mensajes de error al usuario.
  const error = ref<string | null>(null)
  // Obtenemos una instancia del router para usarla en las acciones.
  const router = useRouter()

  // --- Getters ---

  // Un getter computado que devuelve true si existe un token, false si no.
  // Es reactivo: si 'token.value' cambia, 'isAuthenticated' se recalcula automáticamente.
  const isAuthenticated = computed<boolean>(() => !!token.value)
  // El !! convierte un valor (string o null) a su equivalente booleano (true o false).

  // --- Actions ---

  // Acción para iniciar sesión
  async function login(credentials: LoginUsuarioDto): Promise<void> {
    isLoading.value = true // Inicia el estado de carga
    error.value = null // Limpia errores previos
    try {
      // Llamada a la API (POST a /api/v1/auth/login)
      const response = await axios.post<{ token: string }>('/api/v1/auth/login', credentials)
      // Si la respuesta es exitosa
      const newToken = response.data.token
      token.value = newToken // Actualiza el estado reactivo del token
      // Guarda el valor del token en el localstorage para persistir
      localStorage.setItem('token', newToken)
      // Redirige a la página de perfil
      router.push({name: 'profile'})
    } catch (err: any) {
      // Cuando ocurre una exception, se actualiza el mensaje del usuario
      error.value = err.response?.data?.message || 'Error al iniciar sesión.'
    } finally {
      // Finaliza el loop de carga, independientemente del resultado
      isLoading.value = false
    }
  }

  // Acción para registrar usuario
  async function register(userData: RegistroUsuarioDto): Promise<boolean> {
    isLoading.value = true // Inicia el estado de carga
    error.value = null // Limpia errores previos
    try {
      // Llamada a la API (/api/v1/auth/register)
      await axios.post('/api/v1/auth/register', userData)
      // Indica que el registro fue exitoso
      return true
    } catch (err: any) {
      // Cuando ocurre una exception, se actualiza el mensaje del usuario
      error.value = err.response?.data?.message || 'Error al registrarse.'
      // Indica que el registro falló
      return false
    } finally {
      // Finaliza el loop de carga, independientemente del resultado
      isLoading.value = false
    }
  }

  // Función auxiliar para limpiar el estado y el localstorage
  function logoutLocally(): void{
    // Reinicia el valor del token
    token.value = null
    // Remueve los datos del usuario
    localStorage.removeItem('authToken')
    localStorage.removeItem('userEmail')
  }

  // Acción que finaliza la sesión del usuario
  async function logout(): Promise<void> {
    logoutLocally() // Limpia el estado actual
    // Redirige a la página de login
    router.push({name: 'login'})
  }

  // Al final, retornaremos estas refs
  return {
    // props
    token, isLoading, error, userEmail, isAuthenticated,
    // actions
    login, logout, register
  }
})
