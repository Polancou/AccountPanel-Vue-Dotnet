<script setup lang="ts">
import LoadingSpinner from '@/components/LoadingSpinner.vue' // Componente de carga
import { ref } from 'vue' // ref para variables reactivas
import { useAuthStore } from '@/stores/auth' // Nuestro store de Pinia
import { useRouter } from 'vue-router' // Para navegar
import type { LoginUsuarioDto } from '@/types/dto' // Tipo para las credenciales

// Accedemos al store de autenticación
const authStore = useAuthStore()
// Accedemos al router para navegación
const router = useRouter()
// Variables reactivas para el formulario de login
const credentials = ref<LoginUsuarioDto>({
  email: "",
  password: "",
})

/**
 * Función para manejar el inicio de sesión
 */
const handleLogin = async () => {
  console.log("Intentando procesar inciar sesion")
  await authStore.login(credentials.value)
  console.log("Proceso de inicio de sesion finalizado")
}

/**
 * Función para redirigir a la página de registro
 */
const goToRegister = () => {
  router.push({ name: "register" })
}

</script>

<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-100 dark:bg-gray-900">
    <div class="w-full max-w-md p-8 space-y-6 bg-white rounded shadow-md dark:bg-gray-800">
      <h2 class="text-2xl font-bold text-center text-gray-900 dark:text-gray-100">
        Iniciar Sesión
      </h2>

      <form @submit.prevent="handleLogin" class="space-y-6">
        <div>
          <label for="email" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Email
          </label>
          <input v-model="credentials.email" id="email" name="email" type="email" required
            class="block w-full px-3 py-2 mt-1 text-gray-900 bg-gray-50 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 dark:focus:ring-blue-500 dark:focus:border-blue-500"
            placeholder="Ingresar correo electrónico" />
        </div>

        <div>
          <label for="password" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Contraseña
          </label>
          <input v-model="credentials.password" id="password" name="password" type="password" required
            class="block w-full px-3 py-2 mt-1 text-gray-900 bg-gray-50 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 dark:focus:ring-blue-500 dark:focus:border-blue-500"
            placeholder="Ingresar contraseña" />
        </div>

        <div>
          <LoadingSpinner v-if="authStore.isLoading" />
          <button type="submit" :disabled="authStore.isLoading"
            class="w-full px-4 py-2 text-sm font-medium text-white bg-indigo-600 border border-transparent rounded-md shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">
            Iniciar Sesión
          </button>
        </div>

        <p v-if="authStore.error" class="text-sm text-center text-red-600 dark:text-red-400">
          {{ authStore.error }}
        </p>
      </form>

      <p class="text-sm text-center text-gray-600 dark:text-gray-400">
        ¿No tienes cuenta?
        <button @click="goToRegister"
          class="font-medium text-indigo-600 hover:text-indigo-500 dark:text-blue-500 dark:hover:text-blue-400">
          Regístrate aquí
        </button>
      </p>
    </div>
  </div>
</template>

<style scoped></style>
