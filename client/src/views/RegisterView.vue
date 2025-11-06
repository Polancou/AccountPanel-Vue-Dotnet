<script setup lang="ts">
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import type { RegistroUsuarioDto } from '@/types/dto'
import { useRouter } from 'vue-router'
import BaseInput from '@/components/common/BaseInput.vue'
import BaseButton from '@/components/common/BaseButton.vue'

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
        <BaseInput v-model="userData.nombreCompleto" label="Nombre Completo" id="nombreCompleto" type="text"
          placeholder="Ingresar nombre" required />
        <BaseInput v-model="userData.email" label="Email" id="email" type="email"
          placeholder="Ingresar correo electrónico" required />
        <BaseInput v-model="userData.numeroTelefono" label="Teléfono" id="numeroTelefono" type="tel"
          placeholder="Ingresar teléfono" required />
        <BaseInput v-model="userData.password" label="Contraseña" id="password" type="password"
          placeholder="Ingresar contraseña" required>
          <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
            Mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 número.
          </p>
        </BaseInput>
        <div>
          <LoadingSpinner v-if="authStore.isLoading" />
          <BaseButton type="submit" :disabled="authStore.isLoading" :fullWidth="true">
            Registrar
          </BaseButton>
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
          class="font-medium cursor-pointer text-indigo-600 hover:text-indigo-500 dark:text-blue-500 dark:hover:text-blue-400">
          Inicia sesión
        </button>
      </p>
    </div>
  </div>
</template>

<style scoped></style>
