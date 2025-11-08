<script setup lang="ts">
import { toRef } from 'vue';
import { useField } from 'vee-validate';

// Define las propiedades del componente
const props = defineProps<{
  name: string
  label: string
  id: string
  type: string
  placeholder?: string
  required?: boolean
}>()
// Vincula el input al estado de VeeValidate usando la prop 'name'
const { value, errorMessage } = useField<string>(toRef(props, 'name'))
</script>

<template>
  <div>
    <label :for="id" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
      {{ label }}
    </label>
    <input :id="id" :type="type" :placeholder="placeholder" :required="required" v-model="value" :class="[
      // 4. Clases base del input
      'input-field',
      // 5. Clases condicionales para el error
      errorMessage
        ? 'border-red-500 focus:ring-red-500 focus:border-red-500 dark:border-red-500' // Estilo de error
        : 'border-gray-300 focus:ring-indigo-500 focus:border-indigo-500 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 dark:focus:ring-blue-500 dark:focus:border-blue-500' // Estilo normal
    ]" />
    <p v-if="errorMessage" class="mt-1 text-xs text-red-600 dark:text-red-400">
      {{ errorMessage }}
    </p>

  </div>
</template>

<style scoped></style>