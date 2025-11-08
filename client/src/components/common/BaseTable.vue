<script setup lang="ts" generic="T extends { id: any }">
import LoadingSpinner from '../LoadingSpinner.vue';

// --- 1. DEFINIR PROPS ---
// Usamos genéricos (<T>) para que el componente acepte cualquier tipo de array,
// siempre que cada objeto tenga una propiedad 'id' para el :key.

// 'columns' es una matriz de objetos que definen las columnas de la tabla.
export interface TableColumn {
  label: string // Título de la columna
  key: string // Clave de la columna
  sortable?: boolean // Indica si la columna es ordenable
  width?: string // Ancho de la columna
}

// --- 2. DEFINIR COMPONENTE ---
defineProps<{
  columns: TableColumn[] // Propiedad que define las columnas de la tabla
  items: T[] // Propiedad que define los datos de la tabla
  isLoading?: boolean // Propiedad que indica si se está cargando los datos
}>()
</script>

<template>
  <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden">
    <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th v-for="col in columns" :key="col.key" scope="col"
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
              {{ col.label }}
            </th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-if="isLoading">
            <td :colspan="columns.length" class="p-4 text-center text-gray-500">
              <LoadingSpinner />
            </td>
          </tr>
          <tr v-else-if="!items || items.length === 0">
            <td :colspan="columns.length" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">
              No se encontraron resultados.
            </td>
          </tr>
          <tr v-else v-for="item in items" :key="item.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td v-for="col in columns" :key="col.key" class="px-6 py-4 whitespace-nowrap text-sm">
              <slot :name="`col-${col.key}`" :item="item">
                <span class="text-gray-900 dark:text-gray-100">
                  {{ item[col.key as keyof T] }}
                </span>
              </slot>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
<style scoped></style>