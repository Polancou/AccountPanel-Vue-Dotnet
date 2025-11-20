<script setup lang="ts">
import LoadingSpinner from '@/components/LoadingSpinner.vue';
import { onMounted, ref, computed } from 'vue';
import { useAuthStore } from '@/stores/auth'
import BaseInput from '@/components/common/BaseInput.vue';
import BaseButton from '@/components/common/BaseButton.vue';
import { Form } from 'vee-validate';
import { z } from 'zod';
import { toTypedSchema } from '@vee-validate/zod';
import type { ActualizarPerfilDto } from '@/types/dto';

// Obtenemos la instancia de la auth store.
const authStore = useAuthStore();
// Define la ruta de la imagen de perfil
const uploadsBaseUrl = import.meta.env.VITE_UPLOADS_BASE_URL;
// Variable para controlar el modo de edición del perfil
const isEditing = ref(false);

// Define el esquema de validación con Zod
const updateProfileSchema = toTypedSchema(
  z.object({
    nombreCompleto: z.string()
      .nonempty('El nombre es obligatorio.')
      .min(3, 'El nombre debe tener al menos 3 caracteres.')
      .max(100, 'El nombre no puede tener más de 100 caracteres.'),
    numeroTelefono: z.string()
      .nonempty('El número de teléfono es obligatorio.')
  })
);

// Se define el estado inicial de los datos editables del perfil
const editableProfile = ref({
  nombreCompleto: '',
  numeroTelefono: ''
});

// El input donde se carga la imagen de perfil
const fileInput = ref<HTMLInputElement | null>(null);

// Función para seleccionar el archivo
const onFileSelected = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files.length > 0) {
    const file = target.files[0]!;
    // Llama a la acción del store
    authStore.uploadAvatar(file);
    // Resetea el input por si el usuario quiere subir el mismo archivo de nuevo
    if (fileInput.value) {
      fileInput.value.value = '';
    }
  }
}

// Función para activar el input de archivo
const triggerFileInput = () => {
  fileInput.value?.click();
}

/**
 * Acción que se ejecuta antes de reenderizar la vista.
 */
onMounted(async () => {
  // Solamente carga los datos si el usuario está autenticado pero los datos son null.
  if (authStore.isAuthenticated && !authStore.userProfile)
    await authStore.fetchProfile();
});

/**
 * Función para habilitar el modo de edición del perfil.
 */
const enableEditing = () => {
  if (authStore.userProfile) {
    // Inicializa los datos editables con los datos actuales del perfil
    editableProfile.value.nombreCompleto = authStore.userProfile.nombreCompleto;
    editableProfile.value.numeroTelefono = authStore.userProfile.numeroTelefono;
  }
  isEditing.value = true; // Activa el modo edición
};

/**
 * Función para manejar el guardado de los cambios en el perfil.
 */
const handleSaveChanges = async (values: Record<string, unknown>) => {
  // Seguridad adicional: solo se puede guardar si el perfil está cargado
  if (!authStore.userProfile) return
  // Guardamos los cambios en el estado local
  const profileData = values as unknown as ActualizarPerfilDto;
  await authStore.updateProfile(profileData)
  // Salimos del modo de edición
  isEditing.value = false
};

/**
 * Función para cancelar la edición del perfil.
 */
const cancelEdit = () => {
  isEditing.value = false;
  authStore.error = null;
};

const profileImageUrl = computed(() => {
  const avatarUrl = authStore.userProfile?.avatarUrl;
  const genericAvatar = 'https://www.phoenixptsp.com/wp-content/uploads/2019/01/generic-profile-icon-10.jpg.png';

  if (!avatarUrl) {
    // 1. Si no hay URL, usa la genérica
    return genericAvatar;
  }

  if (avatarUrl.startsWith('http://') || avatarUrl.startsWith('https://')) {
    // 2. Si es una URL absoluta (como la de Google), úsala directamente
    return avatarUrl;
  }

  // 3. Si es una URL relativa (como /uploads/...), prepende el host
  return `${uploadsBaseUrl}${avatarUrl}`;
});
</script>

<template>
  <div>
    <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-6">Mi Perfil</h1>

    <LoadingSpinner v-if="authStore.isLoading" />

    <div v-else-if="authStore.error"
      class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded relative dark:bg-red-900 dark:border-red-700 dark:text-red-300"
      role="alert">
      <strong class="font-bold">Error:</strong>
      <span class="block sm:inline">{{ authStore.error }}</span>
    </div>

    <div v-else-if="authStore.userProfile" class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-lg">
      <div class="md:col-span-1">
        <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6 text-center">

          <input type="file" ref="fileInput" @change="onFileSelected" accept="image/png, image/jpeg, image/webp"
            class="hidden" />

          <div class="relative w-32 h-32 mx-auto mb-4 group">
            <img
              :src="profileImageUrl"
              alt="Foto de perfil"
              class="w-32 h-32 rounded-full object-cover border-4 border-white dark:border-gray-700 shadow-md" />
            <div @click="triggerFileInput"
              class="absolute inset-0 w-full h-full flex items-center justify-center bg-black bg-opacity-50 rounded-full cursor-pointer opacity-0 group-hover:opacity-100 transition-opacity">
              <span class="text-white text-sm font-medium">Cambiar</span>
            </div>
            <div v-if="authStore.isLoading"
              class="absolute inset-0 w-full h-full flex items-center justify-center bg-black bg-opacity-70 rounded-full">
              <LoadingSpinner />
            </div>
          </div>

          <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100">{{ authStore.userProfile.nombreCompleto }}
          </h2>
          <p class="text-sm text-gray-500 dark:text-gray-400">{{ authStore.userProfile.email }}</p>
        </div>
      </div>
      <div class="md:col-span-2">
        <div class="px-4 py-5 sm:px-6">
          <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">
            Información Personal
          </h3>
          <p class="mt-1 max-w-2xl text-sm text-gray-500 dark:text-gray-400">
            Detalles de tu cuenta
          </p>
        </div>

        <div v-if="!isEditing" class="border-t border-gray-200 dark:border-gray-700">
          <dl>
            <div class="bg-gray-50 dark:bg-gray-700 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
              <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">
                Nombre Completo
              </dt>
              <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100 sm:mt-0 sm:col-span-2">
                {{ authStore.userProfile.nombreCompleto }}
              </dd>
            </div>
            <div class="bg-white dark:bg-gray-800 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
              <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">
                Email
              </dt>
              <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100 sm:mt-0 sm:col-span-2">
                {{ authStore.userProfile.email }}
              </dd>
            </div>
            <div class="bg-gray-50 dark:bg-gray-700 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
              <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">
                Teléfono
              </dt>
              <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100 sm:mt-0 sm:col-span-2">
                {{ authStore.userProfile.numeroTelefono }}
              </dd>
            </div>
            <div class="bg-white dark:bg-gray-800 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
              <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">
                Rol
              </dt>
              <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100 sm:mt-0 sm:col-span-2">
                {{ authStore.userProfile.rol }}
              </dd>
            </div>
            <div class="bg-gray-50 dark:bg-gray-700 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
              <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">
                Fecha Registro
              </dt>
              <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100 sm:mt-0 sm:col-span-2">
                {{ new Date(authStore.userProfile.fechaRegistro).toLocaleDateString() }}
              </dd>
            </div>
            <div class="bg-white dark:bg-gray-800 px-4 py-5 sm:px-6 text-right">
              <BaseButton @click="enableEditing">
                Editar Perfil
              </BaseButton>
            </div>
          </dl>
        </div>
        <div v-else class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-lg">
          <div class="px-4 py-5 sm:px-6">
            <Form @submit="handleSaveChanges" :validation-schema="updateProfileSchema" :initial-values="editableProfile" class="space-y-4">
              <BaseInput name="nombreCompleto" label="Nombre Completo" id="editNombreCompleto" type="text" required />
              <BaseInput name="numeroTelefono" label="Número de Teléfono" id="editNumeroTelefono" type="tel" required />

              <div class="flex justify-end space-x-3 pt-4">

                <BaseButton type="button" @click="cancelEdit" :disabled="authStore.isLoading" variant="secondary">
                  Cancelar
                </BaseButton>
                <BaseButton type="submit" :disabled="authStore.isLoading" variant="primary">
                  {{ authStore.isLoading ? 'Guardando...' : 'Guardar Cambios' }}
                </BaseButton>

              </div>

              <p v-if="authStore.error" class="text-sm text-center text-red-600 dark:text-red-400">
                {{ authStore.error }}
              </p>

            </Form>
          </div>
        </div>
      </div>

    </div>

    <div v-else class="text-center py-10">
      <p class="text-gray-600 dark:text-gray-400">
        No se pudo cargar la información del perfil.
      </p>
    </div>
  </div>
</template>

<style scoped></style>
