<script setup lang="ts">
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import BaseInput from '@/components/common/BaseInput.vue'
import BaseButton from '@/components/common/BaseButton.vue'
import { Form } from 'vee-validate'
import { z } from 'zod'
import { toTypedSchema } from '@vee-validate/zod'
import type { RegistroUsuarioDto} from '@/types/dto';

// Accedemos al store de autenticación
const authStore = useAuthStore()
// Accedemos al router para navegación
const router = useRouter()
// Define el esquema de validación con Zod
const registerSchema = toTypedSchema(
  z.object({
    nombreCompleto: z.string()
      .nonempty('El nombre es obligatorio.')
      .min(3, 'El nombre debe tener entre 3 y 100 caracteres.')
      .max(100, 'El nombre debe tener entre 3 y 100 caracteres.'),
    email: z.string()
      .nonempty('El email es obligatorio.')
      .email('El formato del email no es válido.'),
    numeroTelefono: z.string()
      .nonempty('El número de teléfono es obligatorio.'),
    password: z.string()
      .nonempty("La contraseña es obligatoria.")
      .min(8, "La contraseña debe tener al menos 8 caracteres.")
      .regex(/[A-Z]/, "La contraseña debe contener al menos una mayúscula.")
      .regex(/[a-z]/, "La contraseña debe contener al menos una minúscula.")
      .regex(/[0-9]/, "La contraseña debe contener al menos un número.")
  })
)

// Mensaje de éxito
const successMessage = ref<string | null>(null)

/**
 * Función para manejar el registro de usuario
 */
const handleRegister = async (values: Record<string, unknown>) => {
  // Limpia mensajes previos
  successMessage.value = null
  // Llama al store para registrar
  const userData = values as unknown as RegistroUsuarioDto;
  const success = await authStore.register(userData)
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

      <Form @submit="handleRegister" :validation-schema="registerSchema" v-slot="{ meta }" class="space-y-4">
        <BaseInput name="nombreCompleto" label="Nombre Completo" id="nombreCompleto" type="text"
          placeholder="Ingresar nombre" />
        <BaseInput name="email" label="Email" id="email" type="email" placeholder="Ingresar correo electrónico" />
        <BaseInput name="numeroTelefono" label="Teléfono" id="numeroTelefono" type="tel"
          placeholder="Ingresar teléfono" />
        <BaseInput name="password" label="Contraseña" id="password" type="password" placeholder="Ingresar contraseña">
          <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
            Mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 número.
          </p>
        </BaseInput>
        <div>
          <LoadingSpinner v-if="authStore.isLoading" />
          <BaseButton type="submit" :disabled="!meta.valid || authStore.isLoading" :fullWidth="true">
            Registrar
          </BaseButton>
        </div>

        <p v-if="successMessage" class="text-sm text-center text-green-600 dark:text-green-400">
          {{ successMessage }}
        </p>

        <p v-if="authStore.error && !successMessage" class="text-sm text-center text-red-600 dark:text-red-400">
          {{ authStore.error }}
        </p>
      </Form>

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
