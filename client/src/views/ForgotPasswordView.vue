<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import BaseInput from '@/components/common/BaseInput.vue'
import BaseButton from '@/components/common/BaseButton.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { Form } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import type { ForgotPasswordDto } from '@/types/dto';

// Store y Router
const authStore = useAuthStore()
const router = useRouter()

// Estado para el mensaje de éxito
const successMessage = ref<string | null>(null)

// Esquema de validación (solo email)
const emailSchema = toTypedSchema(
    z.object({
        email: z.string()
            .nonempty('El email es obligatorio.')
            .email('El formato del email no es válido.'),
    })
)

/**
 * Maneja el envío del formulario
 */
const handleSubmit = async (values: Record<string, unknown>) => {
    successMessage.value = null
  const dto = values as unknown as ForgotPasswordDto;
    const success = await authStore.forgotPassword(dto);

    if (success) {
        // Mostramos el mensaje de éxito que viene del backend
        // (Ej. "Si existe una cuenta con ese correo...")
        successMessage.value = authStore.error ? null : (authStore.token ? authStore.token : "Si existe una cuenta con ese correo, se ha enviado un enlace para restablecer la contraseña.");
    }
}

/**
 * Navega de vuelta al login
 */
const goToLogin = () => {
    router.push({ name: 'login' })
}
</script>

<template>
    <div class="flex items-center justify-center min-h-screen bg-gray-100 dark:bg-gray-900">
        <div class="w-full max-w-md p-8 space-y-6 bg-white rounded shadow-md dark:bg-gray-800">
            <h2 class="text-2xl font-bold text-center text-gray-900 dark:text-gray-100">
                Restablecer Contraseña
            </h2>
            <p class="text-sm text-center text-gray-600 dark:text-gray-400">
                Ingresa tu email y te enviaremos un enlace para restablecer tu contraseña.
            </p>

            <Form @submit="handleSubmit" :validation-schema="emailSchema" v-slot="{ meta }" class="space-y-6">

                <BaseInput label="Email" id="email" name="email" type="email"
                    placeholder="Ingresar correo electrónico" />

                <div>
                    <LoadingSpinner v-if="authStore.isLoading" />
                    <BaseButton type="submit" :disabled="!meta.valid || authStore.isLoading || !!successMessage"
                        :fullWidth="true">
                        Enviar Enlace
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
                ¿Recordaste tu contraseña?
                <button type="button" @click="goToLogin"
                    class="font-medium cursor-pointer text-indigo-600 hover:text-indigo-500 dark:text-blue-500 dark:hover:text-blue-400">
                    Inicia sesión
                </button>
            </p>
        </div>
    </div>
</template>
