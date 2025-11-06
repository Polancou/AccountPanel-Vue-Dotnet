<script setup lang="ts">

const modelValue = defineModel<string>({ required: false })

defineProps<{
    label: string
    id: string
    type: string
    placeholder?: string
    required?: boolean
}>()
</script>

<template>
    <div>
        <label :for="id" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            {{ label }}
        </label>
        <input :id="id" :type="type" :placeholder="placeholder" :required="required" v-model="modelValue"
            class="input-field" />
        <slot></slot>
    </div>
</template>

<style scoped></style>