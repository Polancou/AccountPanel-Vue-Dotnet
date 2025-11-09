<script setup lang="ts">
import { computed } from 'vue'

// --- Props ---
// El componente recibe la página actual y el total de páginas.
const props = defineProps<{
  currentPage: number
  totalPages: number
}>()

// --- Emits ---
// Define un evento personalizado 'page-changed' que notificará al padre
// cuando el usuario haga clic en un botón.
const emit = defineEmits<{
  (e: 'page-changed', page: number): void
}>()

// --- Getters (Computed) ---
// Calcula si el botón "Anterior" debe estar habilitado.
const hasPreviousPage = computed(() => props.currentPage > 1)
// Calcula si el botón "Siguiente" debe estar habilitado.
const hasNextPage = computed(() => props.currentPage < props.totalPages)

// --- Acciones ---
// Emite el evento 'page-changed' con el número de la nueva página.
const goToPage = (page: number) => {
  if (page >= 1 && page <= props.totalPages) {
    emit('page-changed', page)
  }
}
</script>

<template>
  <nav
    class="flex items-center justify-between border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-4 py-3 sm:px-6">
    <div class="flex-1 flex justify-between sm:justify-end">
      <button @click="goToPage(currentPage - 1)" :disabled="!hasPreviousPage"
        class="relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50">
        Anterior
      </button>

      <span class="text-sm text-gray-700 dark:text-gray-300 mx-4 self-center hidden sm:inline">
        Página {{ currentPage }} de {{ totalPages }}
      </span>

      <button @click="goToPage(currentPage + 1)" :disabled="!hasNextPage"
        class="ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50">
        Siguiente
      </button>
    </div>
  </nav>
</template>

<style scoped></style>