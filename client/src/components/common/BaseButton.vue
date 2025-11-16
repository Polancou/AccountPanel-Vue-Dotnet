<script setup lang="ts">
import { computed } from 'vue'
// Define las props que el botón aceptará
const props = defineProps<{
  type?: 'button' | 'submit' | 'reset' // El tipo de botón
  disabled?: boolean,
  fullWidth?: boolean,
  variant?: 'primary' | 'secondary' | 'danger' | 'danger-text'
}>()

// Define las propiedad computada para el tipo de botón
const variantClasses = computed<string>(() => {
  // Objeto que mapea cada variante a sus clases de Tailwind
  const styles = {
    // Estilo Principal (Fondo sólido)
    'primary':
      'text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-blue-600 dark:hover:bg-blue-700 focus:ring-indigo-500 dark:focus:ring-blue-800',

    // Estilo Secundario (Borde y fondo claro/transparente)
    'secondary':
      'text-gray-700 bg-white border-gray-300 hover:bg-gray-50 dark:bg-gray-600 dark:text-gray-200 dark:border-gray-500 dark:hover:bg-gray-500 focus:ring-indigo-500',

    // Estilo Peligro (Rojo)
    'danger':
      'text-red-700 bg-red-100 hover:bg-red-200 dark:text-red-300 dark:bg-red-900 dark:hover:bg-red-800 focus:ring-red-500',

    // Estilo Peligro (Rojo con texto)
    'danger-text':
      'text-red-600 dark:text-red-400 hover:bg-red-100 dark:hover:bg-red-900 focus:ring-red-500 border-transparent bg-transparent shadow-none'
  }
  // Devuelve las clases para la variante seleccionada, o 'primary' si no se especifica
  return styles[props.variant || 'primary']
})
</script>

<template>
  <button :type="type || 'button'" :disabled="disabled" :class="[
    // Clases base que TODOS los botones comparten
    'cursor-pointer px-4 py-2 text-sm font-medium rounded-md shadow-sm',
    'focus:outline-none focus:ring-2 focus:ring-offset-2',
    'disabled:opacity-50',

    // Clases de animación
    'transition duration-150 ease-in-out',
    'hover:-translate-y-1 hover:scale-105',

    // Clases condicionales
    { 'w-full': fullWidth }, // Ancho completo si fullWidth es true

    // Clases de variante
    variantClasses // Añade las clases computadas (primary, secondary, o danger)
  ]">
    <slot></slot>
  </button>
</template>

<style scoped></style>