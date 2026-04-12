<script setup lang="ts">
defineProps<{
  title: string
  open: boolean
}>()
</script>

<template>
  <details class="inspector-group" :open="open">
    <summary class="group-summary">{{ title }}</summary>
    <slot />
  </details>
</template>
