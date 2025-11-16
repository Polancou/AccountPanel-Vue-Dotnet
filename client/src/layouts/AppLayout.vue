<script setup lang="ts">
// Importamos RouterView para mostrar el contenido de la ruta actual
import { RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import BaseButton from '@/components/common/BaseButton.vue';

const authStore = useAuthStore();
// Función para cerrar sesión
const handleLogout = () => {
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
        <RouterLink to="/security"
          class="block py-2 px-4 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded">
          Security
        </RouterLink>
        <RouterLink v-if="authStore.isAdmin" to="/admin"
          class="block py-2 px-4 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded">
          Admin Panel
        </RouterLink>
      </nav>
      <div class="mt-auto pt-4 border-t border-gray-200 dark:border-gray-700">
        <BaseButton variant="danger-text" @click="handleLogout" class="w-full">
          Cerrar Sesión
        </BaseButton>
      </div>
    </aside>

    <main class="flex-1 overflow-y-auto p-6">
      <RouterView v-slot="{ Component }">
        <Transition name="fade" mode="out-in">
          <component :is="Component" />
        </Transition>
      </RouterView>
    </main>
  </div>
</template>

<style scoped>
/* Ajuste para que el botón de logout quede abajo */
aside {
  display: flex;
  flex-direction: column;
}

.fade-enter-active,
.fade-leave-active {
  /* Duración de la animación (150ms) */
  transition: opacity 0.15s ease;
}

.fade-enter-from,
.fade-leave-to {
  /* Estado inicial (completamente invisible) */
  opacity: 0;
}
</style>
