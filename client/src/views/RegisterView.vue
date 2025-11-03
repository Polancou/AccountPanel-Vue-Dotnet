<script setup lang="ts">
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import type { RegistroUsuarioDto } from '@/types/dto'
import { useRouter } from 'vue-router'

// Accedemos al store de autenticación
const authStore = useAuthStore()
// Accedemos al router para navegación
const router = useRouter()
// Datos del formulario de registro
const userData = ref<RegistroUsuarioDto>({
  nombreCompleto: "",
  email: "",
  password: "",
  numeroTelefono: ""
})

// Mensaje de éxito
const successMessage = ref<string | null>(null)

/**
 * Función para manejar el registro de usuario
 */
const handleRegister = async () => {
  // Limpia mensajes previos
  successMessage.value = null
  // Llama al store para registrar
  const success = await authStore.register(userData.value)
  // Si el registro fue exitoso, muestra mensaje y redirige
  if (success) {
    successMessage.value = "Registro exitoso. Redirigiendo al inicio de sesión..."
    // Espera un momento y redirige a login
    setTimeout(() => {
      router.push({ name: 'login' })
    }, 2000) // Espera 2 segundos
  }
}

/**
 * Función para redirigir a la página de login
 */
const goToLogin = () => router.push({ name: "login" })

</script>

<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-100 dark:bg-gray-900">
    <div class="w-full max-w-md p-8 space-y-6 bg-white rounded shadow-md dark:bg-gray-800">
      <h2 class="text-2xl font-bold text-center text-gray-900 dark:text-gray-100">Crear Cuenta</h2>

      <form @submit.prevent="handleRegister" class="space-y-4">
        <div>
          <label for="nombreCompleto" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Nombre Completo
          </label>
          <input v-model="userData.nombreCompleto" id="nombreCompleto" type="text" required
            class="block w-full px-3 py-2 mt-1 text-gray-900 bg-gray-50 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 dark:focus:ring-blue-500 dark:focus:border-blue-500" />
        </div>
        <div>
          <label for="email" class="block text-sm font-medium text-gray-700 dark:text-gray-300">Email</label>
          <input v-model="userData.email" id="email" type="email" required
            class="block w-full px-3 py-2 mt-1 text-gray-900 bg-gray-50 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 dark:focus:ring-blue-500 dark:focus:border-blue-500" />
        </div>
        <div>
          <label for="numeroTelefono" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Teléfono
          </label>
          <input v-model="userData.numeroTelefono" id="numeroTelefono" type="tel" required
            class="block w-full px-3 py-2 mt-1 text-gray-900 bg-gray-50 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 dark:focus:ring-blue-500 dark:focus:border-blue-500" />
        </div>
        <div>
          <label for="password" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Contraseña
          </label>
          <input v-model="userData.password" id="password" type="password" required
            class="block w-full px-3 py-2 mt-1 text-gray-900 bg-gray-50 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 dark:focus:ring-blue-500 dark:focus:border-blue-500" />
          <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
            Mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 número.
          </p>
        </div>

        <div>
          <LoadingSpinner v-if="authStore.isLoading" />
          <button type="submit" :disabled="authStore.isLoading"
            class="w-full px-4 py-2 text-sm font-medium text-white bg-indigo-600 border border-transparent rounded-md shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">
            Registrar
          </button>
        </div>

        <p v-if="successMessage" class="text-sm text-center text-green-600 dark:text-green-400">
          {{ successMessage }}
        </p>
        <p v-if="authStore.error && !successMessage" class="text-sm text-center text-red-600 dark:text-red-400">
          {{ authStore.error }}
        </p>
      </form>

      <p class="text-sm text-center text-gray-600 dark:text-gray-400">
        ¿Ya tienes cuenta?
        <button @click="goToLogin"
          class="font-medium text-indigo-600 hover:text-indigo-500 dark:text-blue-500 dark:hover:text-blue-400">
          Inicia sesión
        </button>
      </p>
    </div>
  </div>
</template>

<style scoped></style>
