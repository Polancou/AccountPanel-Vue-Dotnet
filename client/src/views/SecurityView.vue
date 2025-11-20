<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import BaseInput from '@/components/common/BaseInput.vue'
import BaseButton from '@/components/common/BaseButton.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { Form } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import type {CambiarPasswordDto} from "@/types/dto.ts";

const authStore = useAuthStore()

// Define el esquema de validación para cambiar contraseña
const passwordSchema = toTypedSchema(
  z.object({
    oldPassword: z.string().nonempty('La contraseña actual es obligatoria.'),
    newPassword: z.string()
      .nonempty("La nueva contraseña es obligatoria.")
      .min(8, "La contraseña debe tener al menos 8 caracteres.")
      .regex(/[A-Z]/, "Debe contener al menos una mayúscula.")
      .regex(/[a-z]/, "Debe contener al menos una minúscula.")
      .regex(/[0-9]/, "Debe contener al menos un número."),
    confirmPassword: z.string()
      .nonempty('Confirma tu nueva contraseña.')
  })
    // 'refine' se usa para comparar dos campos
    .refine(data => data.newPassword === data.confirmPassword, {
      message: "Las contraseñas nuevas no coinciden.",
      path: ["confirmPassword"], // Indica qué campo mostrará este error
    })
)

// Función para manejar el envío
// 'resetForm' es una función que VeeValidate nos da
const handleChangePassword = async (values: Record<string, unknown>, { resetForm }: { resetForm: () => void }) => {
  const passwordData = values as unknown as CambiarPasswordDto;
  const success = await authStore.changePassword(passwordData);
  if (success) {
    // Limpia el formulario si la actualización fue exitosa
    resetForm();
  }
}
</script>

<template>
  <div>
    <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-6">Seguridad</h1>

    <div class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-lg max-w-lg">
      <div class="px-4 py-5 sm:px-6">
        <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">
          Cambiar Contraseña
        </h3>
        <p class="mt-1 max-w-2xl text-sm text-gray-500 dark:text-gray-400">
          Actualiza tu contraseña de inicio de sesión local.
        </p>
      </div>
      <div class="border-t border-gray-200 dark:border-gray-700 px-4 py-5 sm:px-6">

        <Form @submit="handleChangePassword" :validation-schema="passwordSchema" v-slot="{ meta }" class="space-y-4">

          <BaseInput name="oldPassword" label="Contraseña Actual" id="oldPassword" type="password"
            placeholder="Ingresa tu contraseña actual" />

          <BaseInput name="newPassword" label="Nueva Contraseña" id="newPassword" type="password"
            placeholder="Ingresa tu nueva contraseña" />

          <BaseInput name="confirmPassword" label="Confirmar Nueva Contraseña" id="confirmPassword" type="password"
            placeholder="Confirma tu nueva contraseña" />

          <div class="flex justify-end pt-2">
            <BaseButton type="submit" :disabled="!meta.valid || authStore.isLoading" variant="primary">
              <LoadingSpinner v-if="authStore.isLoading" class="h-5 w-5" />
              {{ authStore.isLoading ? 'Guardando...' : 'Cambiar Contraseña' }}
            </BaseButton>
          </div>

        </Form>

      </div>
    </div>
  </div>
</template>

<style scoped>
/* Pequeño ajuste para el spinner dentro del botón */
button:disabled {
  position: relative;
}

button .spinner-inline {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
}
</style>
