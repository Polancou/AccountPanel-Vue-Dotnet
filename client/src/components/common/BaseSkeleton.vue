<script setup lang="ts">
// Props para personalizar la forma
defineProps<{
  width?: string;
  height?: string;
  borderRadius?: string;
  className?: string;
}>();
</script>

<template>
  <div
    class="animate-pulse bg-gray-200 dark:bg-gray-700"
    :class="className"
    :style="{
      width: width || '100%',
      height: height || '1rem',
      borderRadius: borderRadius || '0.375rem'
    }"
  ></div>
</template>
