<script setup lang="ts">
// Importamos RouterView para mostrar el contenido de la ruta actual
import { RouterView } from 'vue-router'
// Podrías importar aquí un componente de barra lateral o de navegación si lo tuvieras
// import SidebarNav from '@/components/layout/SidebarNav.vue'
</script>

<template>
  <div class="flex h-screen bg-gray-100 dark:bg-gray-900">
    <aside class="w-64 flex-shrink-0 bg-white dark:bg-gray-800 p-4 shadow-md hidden md:block">
      <h1 class="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-6">Account Panel</h1>
      <nav>
        <a href="#" class="block py-2 px-4 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded">Dashboard</a>
        <a href="#" class="block py-2 px-4 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded">Profile</a>
        <a href="#" class="block py-2 px-4 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded">Settings</a>
      </nav>
    </aside>

    <main class="flex-1 overflow-y-auto p-6">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>

</style>
