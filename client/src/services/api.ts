import axios, {type AxiosError, type InternalAxiosRequestConfig} from 'axios';
import {useAuthStore} from '@/stores/auth';
import {toast} from 'vue-sonner';
import router from '@/router';

// Crear instancia de Axios
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5272/api'
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

// Variable para prevenir bucles de refresh
let isRefreshing = false;
// Cola para peticiones fallidas mientras se refresca el token
let failedQueue: Array<{
  resolve: (value: unknown) => void,
  reject: (reason?: unknown) => void
}> = [];
const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Interceptor de Respuesta (Response)
apiClient.interceptors.response.use(
  (response) => {
    // Si la respuesta es exitosa, solo la devolvemos
    return response;
  },
  async (error: AxiosError) => {
    // Casting para acceder a propiedades internas de config si es necesario
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    const authStore = useAuthStore();
    // Si la petición que falló (con 401) era la de REFRESH,
    // significa que el refresh token es inválido. No hay nada que hacer.
    // Cerramos sesión y rechazamos la promesa para detener el bucle.
    if (originalRequest && originalRequest.url?.endsWith('/v1/auth/refresh')) {
      authStore.logout(); // Cierra la sesión
      // Notificamos al usuario
      if (router.currentRoute.value.name !== 'login') {
        toast.error('Tu sesión ha expirado. Por favor, inicia sesión de nuevo.');
      }
      // Rechazamos para detener cualquier otra acción
      return Promise.reject(error);
    }


    // Si el error es 401 (Token Expirado) en CUALQUIER OTRA RUTA
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      // Si ya se está refrescando, ponemos la petición en cola
      if (isRefreshing) {
        return new Promise(function (resolve, reject) {
          failedQueue.push({resolve, reject});
        }).then(token => {
          originalRequest.headers['Authorization'] = 'Bearer ' + token;
          return apiClient(originalRequest);
        }).catch(err => {
          return Promise.reject(err);
        });
      }

      // Marcamos esta petición como reintento y empezamos a refrescar
      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Intentamos refrescar el token
        const success = await authStore.refreshAccessToken();

        if (success) {
          // Si el refresh fue exitoso, reintentamos la petición original
          apiClient.defaults.headers.common['Authorization'] = 'Bearer ' + authStore.token;
          originalRequest.headers['Authorization'] = 'Bearer ' + authStore.token;
          processQueue(null, authStore.token);
          return apiClient(originalRequest);
        } else {
          // Si el refresh falló (refreshAccessToken ya llama a logout)
          processQueue(new Error("Refresh token failed"), null);
          return Promise.reject(error);
        }
      } catch (e) {
        processQueue(e, null);
        return Promise.reject(e);
      } finally {
        isRefreshing = false;
      }
    }

    // Para otros errores (400, 404, 500, etc.), simplemente los rechazamos
    return Promise.reject(error);
  }
);

export default apiClient;
