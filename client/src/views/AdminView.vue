<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiClient from '@/services/api';
import type { ActualizarRolUsuarioDto, PagedResultDto, PerfilUsuarioDto } from '@/types/dto';
import BaseTable from '@/components/common/BaseTable.vue'
import type { TableColumn } from '@/components/common/BaseTable.vue'
import BasePagination from '@/components/common/BasePagination.vue'
import BaseButton from '@/components/common/BaseButton.vue'
import { toast } from 'vue-sonner'

// Obtenemos la instancia de la auth store.
const authStore = useAuthStore()
// Define un array con los datos de los usuarios
const users = ref<PerfilUsuarioDto[]>([])
// Define un estado para indicar si se está cargando los datos
const isLoading = ref<boolean>(false)
const error = ref<string | null>(null)

// Define las propiedades de la paginación
const currentPage = ref(1)
const totalPages = ref(0)
const pageSize = 10

// Define las columnas que quieres mostrar
// Las 'key' deben coincidir con las propiedades de PerfilUsuarioDto
const columns: TableColumn[] = [
  { key: 'nombreCompleto', label: 'Nombre' },
  { key: 'email', label: 'Email' },
  { key: 'rol', label: 'Rol' },
  { key: 'fechaRegistro', label: 'Miembro Desde' },
  { key: 'actions', label: 'Acciones' }
];
/**
 * Función para cargar los datos de los usuarios
 */
const loadUsers = async () => {
  isLoading.value = true
  error.value = null
  try {
    // Llama al endpoint paginado
    const response = await apiClient.get<PagedResultDto<PerfilUsuarioDto>>('/v1/admin/users', {
      params: {
        pageNumber: currentPage.value,
        pageSize: pageSize
      }
    })
    // Actualiza el estado con los datos de la respuesta
    users.value = response.data.items
    totalPages.value = response.data.totalPages
  } catch (err: any) {
    error.value = err.response?.data?.message || 'No se pudieron cargar los usuarios.'
  } finally {
    isLoading.value = false
  }
}

// Llama al método para cargar los datos de los usuarios al renderizar la vista.
onMounted(loadUsers)

// --- 5. Crea el manejador para el evento 'page-changed' ---
const handlePageChange = (newPage: number) => {
  currentPage.value = newPage
  loadUsers()
}
// Helper para formatear la fecha
const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString()
}

/**
* Muestra el diálogo de confirmación antes de eliminar.
*/
const confirmDeleteUser = (item: PerfilUsuarioDto) => {
  // Obtenemos el ID del usuario actual desde el store
  const currentAdminId = authStore.userProfile?.id;

  // Comprobación de seguridad en el frontend (aunque el backend ya la hace)
  if (item.id === currentAdminId) {
    toast.error("No puedes eliminar tu propia cuenta de administrador.");
    return;
  }

  // Llama al toast de confirmación
  toast.warning(`¿Estás seguro de que quieres eliminar a ${item.nombreCompleto}?`, {
    description: "Esta acción no se puede deshacer.",
    action: {
      label: "Eliminar",
      // Llama a la lógica de borrado si se confirma
      onClick: () => handleDeleteUser(item.id)
    },
    cancel: {
      label: "Cancelar",
      // Cierra el toast si se cancela
      onClick: () => toast.dismiss()
    }
  });
}

/**
* Llama a la API para eliminar el usuario y actualiza la UI.
*/
const handleDeleteUser = async (id: number) => {
  isLoading.value = true;
  error.value = null;

  try {
    // Llama al endpoint para eliminar el usuario
    await apiClient.delete(`/v1/admin/users/${id}`);

    // Éxito: muestra un toast de éxito
    toast.success("Usuario eliminado correctamente.");

    // Actualiza la lista de usuarios en la UI:
    // Filtra el usuario eliminado de la lista local 'users.value'
    users.value = users.value.filter(user => user.id !== id);
    // Carga la lista de usuarios nuevamente
    await loadUsers(); 

  } catch (err: any) {
    // Si falla (ej. error 400, 404, 500 del middleware)
    const message = err.response?.data?.message || 'No se pudo eliminar el usuario.';
    toast.error(message);
    error.value = message;
  } finally {
    isLoading.value = false;
  }
}

const confirmEditRole = (item: PerfilUsuarioDto) => {
  const currentAdminId = authStore.userProfile?.id;
  if (item.id === currentAdminId) {
    toast.error("No puedes editar tu propio rol.");
    return;
  }

  // El rol opuesto al actual
  const newRole = item.rol === 'Admin' ? 'User' : 'Admin';

  toast.info(`¿Cambiar el rol de ${item.nombreCompleto} a ${newRole}?`, {
    action: {
      label: `Cambiar a ${newRole}`,
      onClick: () => handleUpdateRole(item.id, newRole)
    },
    cancel: {
      label: "Cancelar",
      onClick: () => toast.dismiss()
    }
  });
}

const handleUpdateRole = async (id: number, newRole: string) => {
  isLoading.value = true;
  error.value = null;

  const dto: ActualizarRolUsuarioDto = { rol: newRole };

  try {
    await apiClient.put(`/v1/admin/users/${id}/role`, dto);
    toast.success("Rol actualizado correctamente.");

    // Actualiza la UI localmente
    const user = users.value.find(u => u.id === id);
    if (user) {
      user.rol = newRole;
    }

  } catch (err: any) {
    const message = err.response?.data?.message || 'No se pudo actualizar el rol.';
    toast.error(message);
    error.value = message;
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div>
    <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-6">Panel de Administrador</h1>

    <div v-if="error" class="bg-red-100 ... mb-4" role="alert">
      <strong class="font-bold">Error:</strong>
      <span>{{ error }}</span>
    </div>

    <BaseTable :columns="columns" :items="users" :isLoading="isLoading">
      <template #col-rol="{ item }">
        <span :class="[
          'px-2 py-0.5 inline-flex text-xs leading-5 font-semibold rounded-full',
          item.rol === 'Admin'
            ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
            : 'bg-gray-100 text-gray-800 dark:bg-gray-600 dark:text-gray-200'
        ]">
          {{ item.rol }}
        </span>
      </template>

      <template #col-fechaRegistro="{ item }">
        <span class="text-gray-500 dark:text-gray-300">
          {{ formatDate(item.fechaRegistro) }}
        </span>
      </template>

      <template #col-actions="{ item }">
        <div class="flex space-x-2">
          <BaseButton variant="secondary" @click="confirmEditRole(item)">
            Editar Rol
          </BaseButton>
          <BaseButton variant="danger-text" @click="confirmDeleteUser(item)">
            Eliminar
          </BaseButton>
        </div>
      </template>
    </BaseTable>
    <BasePagination v-if="!isLoading && totalPages > 1" :currentPage="currentPage" :totalPages="totalPages"
      @page-changed="handlePageChange" />
  </div>
</template>

<style scoped></style>