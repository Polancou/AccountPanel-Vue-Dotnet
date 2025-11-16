<script setup lang="ts">
import { RouterView } from 'vue-router'
import { Toaster } from 'vue-sonner';
import 'vue-sonner/style.css'
import { useDark } from '@vueuse/core';

import IconSun from '@/components/icons/IconSun.vue';
import IconMoon from '@/components/icons/IconMoon.vue';

const isDark = useDark();
</script>

<template>
    <button @click="isDark = !isDark" class="fixed top-4 right-4 z-50 p-2 rounded-full 
             bg-gray-200 dark:bg-gray-700 
             text-gray-800 dark:text-gray-200 
             hover:bg-gray-300 dark:hover:bg-gray-600
             transition-colors duration-200" aria-label="Toggle theme">
        <IconSun v-if="isDark" />
        <IconMoon v-else />
    </button>

    <RouterView />
    <Toaster position="top-center" rich-colors />
</template>

<style scoped>
button svg {
    width: 20px;
    height: 20px;
}
</style>