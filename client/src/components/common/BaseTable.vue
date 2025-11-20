<script setup lang="ts" generic="T extends { id: any }">
import BaseSkeleton from './BaseSkeleton.vue';

// --- 1. DEFINIR PROPS ---
export interface TableColumn {
  label: string
  key: string
  sortable?: boolean
  width?: string
}

defineProps<{
  columns: TableColumn[]
  items: T[]
  isLoading?: boolean
  filterActive?: boolean
}>()
</script>

<template>
  <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden">
    <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
        <tr>
          <th v-for="col in columns" :key="col.key" scope="col"
              class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider"
              :style="{ width: col.width }">
            {{ col.label }}
          </th>
        </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">

        <template v-if="isLoading">
          <tr v-for="i in 5" :key="i">
            <td v-for="col in columns" :key="col.key" class="px-6 py-4 whitespace-nowrap">
              <BaseSkeleton height="1.25rem" />
            </td>
          </tr>
        </template>

        <tr v-else-if="!items || items.length === 0">
          <td :colspan="columns.length" class="px-6 py-10 text-center text-sm text-gray-500 dark:text-gray-400">
            <div class="flex flex-col items-center justify-center space-y-2">
              <span class="text-2xl">🔍</span>
              <p v-if="filterActive">No se encontraron resultados para tu búsqueda.</p>
              <p v-else>Aún no hay datos registrados.</p>
            </div>
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
