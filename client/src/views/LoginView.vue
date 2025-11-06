<script setup lang="ts">
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import BaseInput from '@/components/common/BaseInput.vue'
import BaseButton from '@/components/common/BaseButton.vue'
import { Form } from 'vee-validate'
import { z } from 'zod'
import { toTypedSchema } from '@vee-validate/zod'

// Accedemos al store de autenticación
const authStore = useAuthStore()
// Accedemos al router para navegación
const router = useRouter()

// Define el esquema de validación con Zod
const loginSchema = toTypedSchema(
  z.object({
    // Coincide con las reglas de validación para el inicio de sesión
    email: z.string().nonempty('El email es obligatorio.').email('El formato del email no es válido.'),
    password: z.string().nonempty('La contraseña es obligatoria.')
  })
)

/**
 * Función para manejar el inicio de sesión
 */
const handleLogin = async (values: any) => {
  console.log("Intentando procesar inciar sesion")
  await authStore.login(values)
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

      <Form @submit="handleLogin" :validation-schema="loginSchema" v-slot="{ meta }" class="space-y-6">
        <BaseInput label="Email" id="email" name="email" type="email" placeholder="Ingresar correo electrónico" />
        <BaseInput label="Contraseña" id="password" name="password" type="password" placeholder="Ingresar contraseña" />
        <div>
          <LoadingSpinner v-if="authStore.isLoading" />
          <BaseButton type="submit" :disabled="authStore.isLoading" :fullWidth="true">
            Iniciar Sesión
          </BaseButton>
        </div>

        <p v-if="authStore.error" class="text-sm text-center text-red-600 dark:text-red-400">
          {{ authStore.error }}
        </p>
      </Form>

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
