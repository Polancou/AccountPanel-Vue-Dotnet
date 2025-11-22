import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/stores/auth';
import { toast } from 'vue-sonner';
import router from '@/router';

// Crear instancia de Axios
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5272/api',
  withCredentials: true
});

// Interceptor de Petición (Request)
apiClient.interceptors.request.use((config) => {
    const authStore = useAuthStore();
    const token = authStore.token; // Access Token

    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// --- LÓGICA DE REFRESH TOKEN Y COLA DE PETICIONES ---

// Bandera para saber si ya estamos refrescando el token
let isRefreshing = false;

// Cola para almacenar las peticiones que fallaron por 401 mientras se refrescaba el token
let failedQueue: Array<{
  resolve: (value: unknown) => void,
  reject: (reason?: unknown) => void
}> = [];

/**
 * Procesa la cola de peticiones fallidas.
 * Si se recibe un token, se reintentan (resolve).
 * Si se recibe un error, se cancelan (reject).
 */
const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  // Limpiamos la cola después de procesarla
  failedQueue = [];
};

// Interceptor de Respuesta (Response)
apiClient.interceptors.response.use(
  (response) => {
    // Si la respuesta es exitosa, solo la devolvemos
    return response;
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    const authStore = useAuthStore();

    // 1. Si la petición que falló (con 401) era la de REFRESH,
    // significa que el refresh token es inválido o expiró.
    // No hay nada más que hacer: cerramos sesión y rechazamos todo.
    if (originalRequest && originalRequest.url?.endsWith('/v1/auth/refresh')) {
      // El store ya hace logout en su catch, pero aseguramos limpieza aquí
      authStore.logoutLocally();

      if (router.currentRoute.value.name !== 'login') {
        toast.error('Tu sesión ha expirado. Por favor, inicia sesión de nuevo.');
        router.push({ name: 'login' });
      }

      // Rechazamos cualquier cola pendiente porque el refresh principal falló
      processQueue(new Error("La sesión ha expirado (Refresh fallido)"), null);

      return Promise.reject(error);
    }

    // 2. Si el error es 401 (Token Expirado) en CUALQUIER OTRA RUTA
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {

      // CASO A: Ya existe un proceso de refresco en curso.
      // Encolamos esta petición para que espere a que termine el primero.
      if (isRefreshing) {
        return new Promise(function (resolve, reject) {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          // Cuando se resuelva, reintentamos con el nuevo token
          originalRequest.headers['Authorization'] = 'Bearer ' + token;
          return apiClient(originalRequest);
        }).catch(err => {
          return Promise.reject(err);
        });
      }

      // CASO B: Somos la primera petición en fallar. Iniciamos el refresco.
      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Intentamos refrescar el token llamando al store
        const success = await authStore.refreshAccessToken();

        if (success && authStore.token) {
          // ÉXITO: Actualizamos cabeceras y procesamos la cola con el nuevo token
          apiClient.defaults.headers.common['Authorization'] = 'Bearer ' + authStore.token;
          originalRequest.headers['Authorization'] = 'Bearer ' + authStore.token;

          // Desbloqueamos a los que estaban esperando
          processQueue(null, authStore.token);

          // Reintentamos la petición original que disparó todo esto
          return apiClient(originalRequest);
        } else {
          // FALLO LÓGICO (ej. no había refresh token o el servidor lo rechazó)
          const err = new Error("No se pudo refrescar la sesión.");
          processQueue(err, null); // Rechazamos a todos los encolados
          return Promise.reject(err); // Rechazamos la original
        }
      } catch (e) {
        // FALLO TÉCNICO (ej. error de red al refrescar)
        const err = e instanceof Error ? e : new Error("Error desconocido al refrescar token");
        processQueue(err, null); // Rechazamos a todos
        return Promise.reject(err); // Rechazamos la original
      } finally {
        // Siempre liberamos el bloqueo al terminar
        isRefreshing = false;
      }
    }

    // Para otros errores (400, 404, 500, etc.), simplemente los rechazamos
    return Promise.reject(error);
  }
);

export default apiClient;
