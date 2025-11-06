<script setup lang="ts">
// Definimos las props que el botón aceptará
defineProps<{
    type?: 'button' | 'submit' | 'reset' // El tipo de botón
    disabled?: boolean,
    fullWidth?: boolean
}>()
</script>

<template>
    <button :type="type || 'button'" :disabled="disabled" :class="[
        // Clases base que SIEMPRE se aplican
        'cursor-pointer px-4 py-2 text-sm font-medium border border-transparent rounded-md shadow-sm',
        'focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500',
        'disabled:opacity-50',

        // Clases de color 
        'text-white bg-indigo-600 hover:bg-indigo-700 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800',

        // Clase condicional: se aplica 'w-full' SOLO SI la prop 'fullWidth' es true
        { 'w-full': fullWidth }
    ]">
        <slot></slot>
    </button>
</template>

<style scoped></style>