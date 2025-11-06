<script setup lang="ts">
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import type { LoginUsuarioDto } from '@/types/dto'
import BaseInput from '@/components/common/BaseInput.vue'
import BaseButton from '@/components/common/BaseButton.vue'

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
        <BaseInput v-model="credentials.email" label="Email" id="email" type="email"
          placeholder="Ingresar correo electrónico" required />
        <BaseInput v-model="credentials.password" label="Contraseña" id="password" type="password"
          placeholder="Ingresar contraseña" required />

        <div>
          <LoadingSpinner v-if="authStore.isLoading" />
          <BaseButton type="submit" :disabled="authStore.isLoading" :fullWidth="true">
            Iniciar Sesión
          </BaseButton>
        </div>

        <p v-if="authStore.error" class="text-sm text-center text-red-600 dark:text-red-400">
          {{ authStore.error }}
        </p>
      </form>

      <p class="text-sm text-center text-gray-600 dark:text-gray-400">
        ¿No tienes cuenta?
        <button type="button" @click="goToRegister"
          class="font-medium cursor-pointer text-indigo-600 hover:text-indigo-500 dark:text-blue-500 dark:hover:text-blue-400">
          Regístrate aquí
        </button>
      </p>
    </div>
  </div>
</template>

<style scoped></style>
