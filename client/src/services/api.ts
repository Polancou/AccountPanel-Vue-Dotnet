import axios from 'axios';
import { useAuthStore } from '@/stores/auth';
import { toast } from 'vue-sonner';
import router from '@/router';

// Crear instancia de Axios
const apiClient = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5272/api'
});

// Interceptor de Petición (Request)
apiClient.interceptors.request.use(
    (config) => {
        // Obtenemos el store *dentro* del interceptor para evitar problemas de inicialización
        const authStore = useAuthStore();
        const token = authStore.token;

        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Interceptor de Respuesta (Response)
apiClient.interceptors.response.use(
    (response) => {
        // Si la respuesta es exitosa, solo la devolvemos
        return response;
    },
    (error) => {
        // Si hay un error de respuesta
        if (error.response) {
            const authStore = useAuthStore();

            // Manejo de error 401 (No autorizado)
            if (error.response.status === 401) {
                // Si no estábamos en la página de login, mostramos un toast
                if (router.currentRoute.value.name !== 'login') {
                    toast.error('Tu sesión ha expirado. Por favor, inicia sesión de nuevo.');
                }
                // Llamamos a la acción de logout
                authStore.logout();
            }
        }

        // Rechazamos la promesa para que el .catch() en el store se active
        return Promise.reject(error);
    }
);

export default apiClient;