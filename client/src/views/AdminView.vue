<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import axios from 'axios';
import type { PagedResultDto, PerfilUsuarioDto } from '@/types/dto';
import BaseTable from '@/components/common/BaseTable.vue'
import type { TableColumn } from '@/components/common/BaseTable.vue'
import BasePagination from '@/components/common/BasePagination.vue'

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
  { key: 'fechaRegistro', label: 'Miembro Desde' }
];
/**
 * Función para cargar los datos de los usuarios
 */
const loadUsers = async () => {
  isLoading.value = true
  error.value = null
  try {
    // Llama al endpoint paginado
    const response = await axios.get<PagedResultDto<PerfilUsuarioDto>>('/api/v1/admin/users', {
      headers: { 'Authorization': `Bearer ${authStore.token}` },
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

    </BaseTable>
    <BasePagination v-if="!isLoading && totalPages > 1" :currentPage="currentPage" :totalPages="totalPages"
      @page-changed="handlePageChange" />
  </div>
</template>

<style scoped></style>