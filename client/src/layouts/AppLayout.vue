<script setup lang="ts">
// Importamos RouterView para mostrar el contenido de la ruta actual
import { RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore();
// Función para cerrar sesión
const closeSession = () => {
  authStore.logout()
}
</script>

<template>
  <div class="flex h-screen bg-gray-100 dark:bg-gray-900">
    <aside class="w-64 shrink-0 bg-white dark:bg-gray-800 p-4 shadow-md hidden md:block">
      <h1 class="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-6">
        Account Panel
      </h1>
      <nav>
        <RouterLink to="/profile"
          class="block py-2 px-4 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded">
          Profile
        </RouterLink>
      </nav>
      <div class="mt-auto pt-4 border-t border-gray-200 dark:border-gray-700">
        <button @click="closeSession"
          class="w-full cursor-pointer py-2 px-4 text-left text-red-600 dark:text-red-400 hover:bg-red-100 dark:hover:bg-red-900 rounded">
          Cerrar Sesión
        </button>
      </div>
    </aside>

    <main class="flex-1 overflow-y-auto p-6">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
/* Ajuste para que el botón de logout quede abajo */
aside {
  display: flex;
  flex-direction: column;
}
</style>
