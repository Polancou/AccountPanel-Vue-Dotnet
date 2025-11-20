<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import BaseInput from '@/components/common/BaseInput.vue'
import BaseButton from '@/components/common/BaseButton.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { Form } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import type {ResetPasswordDto} from "@/types/dto.ts";

// Store y Router
const authStore = useAuthStore()
const router = useRouter()
const route = useRoute() // Para leer la URL

// Estado
const token = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const validationError = ref<string | null>(null)

// Esquema de validación
const resetSchema = toTypedSchema(
    z.object({
        newPassword: z.string()
            .nonempty("La nueva contraseña es obligatoria.")
            .min(8, "La contraseña debe tener al menos 8 caracteres.")
            .regex(/[A-Z]/, "Debe contener al menos una mayúscula.")
            .regex(/[a-z]/, "Debe contener al menos una minúscula.")
            .regex(/[0-9]/, "Debe contener al menos un número."),
        confirmPassword: z.string()
            .nonempty('Confirma tu nueva contraseña.')
    })
        .refine(data => data.newPassword === data.confirmPassword, {
            message: "Las contraseñas nuevas no coinciden.",
            path: ["confirmPassword"],
        })
)

/**
 * Al cargar el componente, lee el token de la URL
 */
onMounted(() => {
    const tokenFromUrl = route.query.token;
    if (typeof tokenFromUrl === 'string' && tokenFromUrl) {
        token.value = tokenFromUrl;
    } else {
        validationError.value = "Token inválido o no proporcionado. Por favor, solicita un nuevo enlace.";
    }
})

/**
 * Maneja el envío del formulario
 */
const handleSubmit = async (values: Record<string, unknown>) => {
  if (!token.value) return;

  successMessage.value = null;
  const formValues = values as { newPassword: string; confirmPassword: string };

  const dto: ResetPasswordDto = {
    token: token.value,
    newPassword: formValues.newPassword,
    confirmPassword: formValues.confirmPassword
  };
  const success = await authStore.resetPassword(dto);

  if (success) {
    successMessage.value = "¡Contraseña actualizada! Redirigiendo al inicio de sesión...";
    // Redirige al login después de 2 segundos
    setTimeout(() => {
      router.push({name: 'login'});
    }, 2000);
  }
}
</script>

<template>
    <div class="flex items-center justify-center min-h-screen bg-gray-100 dark:bg-gray-900">
        <div class="w-full max-w-md p-8 space-y-6 bg-white rounded shadow-md dark:bg-gray-800">
            <h2 class="text-2xl font-bold text-center text-gray-900 dark:text-gray-100">
                Crear Nueva Contraseña
            </h2>

            <div v-if="validationError" class="text-center">
                <p class="text-red-600 dark:text-red-400">{{ validationError }}</p>
                <BaseButton @click="router.push({ name: 'forgot-password' })" class="mt-4">
                    Solicitar de nuevo
                </BaseButton>
            </div>

            <Form v-else @submit="handleSubmit" :validation-schema="resetSchema" v-slot="{ meta }" class="space-y-6">

                <BaseInput name="newPassword" label="Nueva Contraseña" id="newPassword" type="password"
                    placeholder="Ingresa tu nueva contraseña" />

                <BaseInput name="confirmPassword" label="Confirmar Nueva Contraseña" id="confirmPassword"
                    type="password" placeholder="Confirma tu nueva contraseña" />

                <div>
                    <LoadingSpinner v-if="authStore.isLoading" />
                    <BaseButton type="submit" :disabled="!meta.valid || authStore.isLoading || !!successMessage"
                        :fullWidth="true">
                        Guardar Contraseña
                    </BaseButton>
                </div>

                <p v-if="successMessage" class="text-sm text-center text-green-600 dark:text-green-400">
                    {{ successMessage }}
                </p>
                <p v-if="authStore.error && !successMessage" class="text-sm text-center text-red-600 dark:text-red-400">
                    {{ authStore.error }}
                </p>
            </Form>
        </div>
    </div>
</template>
