<script setup lang="ts">
// --- Imports ---
import { ref, onMounted, watch, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import apiClient from '@/services/api';
import type { PagedResultDto, PerfilUsuarioDto, ActualizarRolUsuarioDto } from '@/types/dto';
import BaseTable from '@/components/common/BaseTable.vue'
import type { TableColumn } from '@/components/common/BaseTable.vue'
import BasePagination from '@/components/common/BasePagination.vue'
import BaseButton from '@/components/common/BaseButton.vue';
import { toast } from 'vue-sonner'
import { useDebounceFn } from '@vueuse/core'
import type { AxiosError } from 'axios';

// --- Store ---
const authStore = useAuthStore()

// --- Estado de la UI ---
const users = ref<PerfilUsuarioDto[]>([])
const isLoading = ref<boolean>(false)
const error = ref<string | null>(null)

// --- Estado de Paginación y Filtros ---
const currentPage = ref(1)
const totalPages = ref(0)
const pageSize = 10
const searchTerm = ref('') // Conectado al <input> de búsqueda
const selectedRole = ref<string | number>('') // Conectado al <select> de rol

// Propiedad computada para saber si hay filtros activos (para el feedback visual de la tabla)
const filterActive = computed(() => searchTerm.value !== '' || selectedRole.value !== '')

// --- Configuración de la Tabla ---
const columns: TableColumn[] = [
  { key: 'nombreCompleto', label: 'Nombre' },
  { key: 'email', label: 'Email' },
  { key: 'rol', label: 'Rol' },
  { key: 'fechaRegistro', label: 'Miembro Desde' },
  { key: 'actions', label: 'Acciones' } // Columna para los botones
];

// --- Lógica de Carga de Datos ---

// Definimos la interfaz para los parámetros de búsqueda
interface AdminUserQueryParams {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  rol?: string | number;
}

/**
 * Función principal para cargar los usuarios desde la API.
 */
const loadUsers = async () => {
  isLoading.value = true
  error.value = null

  // Construye los parámetros de la API de forma tipada
  const params: AdminUserQueryParams = {
    pageNumber: currentPage.value,
    pageSize: pageSize
  };

  if (searchTerm.value) {
    params.searchTerm = searchTerm.value;
  }
  // El select tiene value="" para 'Todos', así que validamos que no sea string vacío
  if (selectedRole.value !== '') {
    params.rol = selectedRole.value;
  }

  try {
    const response = await apiClient.get<PagedResultDto<PerfilUsuarioDto>>('/v1/admin/users', { params });
    users.value = response.data.items
    totalPages.value = response.data.totalPages
  } catch (err: unknown) {
    const axiosError = err as AxiosError<{ message: string }>;
    error.value = axiosError.response?.data?.message || 'No se pudieron cargar los usuarios.'
  } finally {
    isLoading.value = false
  }
}

// Carga inicial al montar el componente
onMounted(loadUsers)

// --- Observadores (Watchers) para Filtros ---

// Crea una versión de loadUsers con 300ms de retraso
const debouncedLoadUsers = useDebounceFn(() => {
  currentPage.value = 1; // Resetea la página al buscar
  loadUsers();
}, 300);

// Observa el término de búsqueda
watch(searchTerm, () => {
  debouncedLoadUsers();
});

// Observa el filtro de rol (cambio instantáneo)
watch(selectedRole, () => {
  currentPage.value = 1; // Resetea la página al filtrar
  loadUsers();
});

// --- Lógica de Acciones de Admin ---

/**
 * Muestra confirmación para eliminar un usuario.
 */
const confirmDeleteUser = (item: PerfilUsuarioDto) => {
  const currentAdminId = authStore.userProfile?.id;
  if (item.id === currentAdminId) {
    toast.error("No puedes eliminar tu propia cuenta de administrador.");
    return;
  }

  toast.warning(`¿Estás seguro de que quieres eliminar a ${item.nombreCompleto}?`, {
    description: "Esta acción no se puede deshacer.",
    action: {
      label: "Eliminar",
      onClick: () => handleDeleteUser(item.id)
    },
    cancel: {
      label: "Cancelar"
    }
  });
}

/**
 * Llama a la API para eliminar el usuario.
 */
const handleDeleteUser = async (id: number) => {
  isLoading.value = true;
  error.value = null;
  try {
    await apiClient.delete(`/v1/admin/users/${id}`);
    toast.success("Usuario eliminado correctamente.");
    // Actualiza la UI filtrando el usuario eliminado
    users.value = users.value.filter(user => user.id !== id);
  } catch (err: unknown) {
    const axiosError = err as AxiosError<{ message: string }>;
    const message = axiosError.response?.data?.message || 'No se pudo eliminar el usuario.';
    toast.error(message);
    error.value = message;
  } finally {
    isLoading.value = false;
  }
}

/**
 * Muestra confirmación para editar el rol de un usuario.
 */
const confirmEditRole = (item: PerfilUsuarioDto) => {
  const currentAdminId = authStore.userProfile?.id;
  if (item.id === currentAdminId) {
    toast.error("No puedes editar tu propio rol.");
    return;
  }

  const newRole = item.rol === 'Admin' ? 'User' : 'Admin';

  toast.info(`¿Cambiar el rol de ${item.nombreCompleto} a ${newRole}?`, {
    action: {
      label: `Cambiar a ${newRole}`,
      onClick: () => handleUpdateRole(item.id, newRole)
    },
    cancel: {
      label: "Cancelar"
    }
  });
}

/**
 * Llama a la API para actualizar el rol del usuario.
 */
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

  } catch (err: unknown) {
    const axiosError = err as AxiosError<{ message: string }>;
    const message = axiosError.response?.data?.message || 'No se pudo actualizar el rol.';
    toast.error(message);
    error.value = message;
  } finally {
    isLoading.value = false;
  }
}

// --- Helpers ---
const handlePageChange = (newPage: number) => {
  currentPage.value = newPage
  loadUsers()
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString()
}
</script>

<template>
  <div>
    <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-6">Panel de Administrador</h1>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">

      <div class="md:col-span-2">
        <label for="searchTerm" class="block text-sm font-medium text-gray-700 dark:text-gray-300">Buscar
          usuario</label>
        <input id="searchTerm" name="searchTerm" type="text" placeholder="Buscar por nombre o email..."
               v-model="searchTerm" class="input-field" />
      </div>

      <div>
        <label for="roleFilter" class="block text-sm font-medium text-gray-700 dark:text-gray-300">Filtrar por
          Rol</label>
        <select id="roleFilter" name="roleFilter" v-model="selectedRole" class="input-field">
          <option value="">Todos los Roles</option>
          <option :value="0">User</option>
          <option :value="1">Admin</option>
        </select>
      </div>
    </div>

    <div v-if="error"
         class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded dark:bg-red-900 dark:border-red-700 dark:text-red-300 mb-4"
         role="alert">
      <strong class="font-bold">Error:</strong>
      <span>{{ error }}</span>
    </div>

    <BaseTable :columns="columns" :items="users" :isLoading="isLoading" :filter-active="filterActive">

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
