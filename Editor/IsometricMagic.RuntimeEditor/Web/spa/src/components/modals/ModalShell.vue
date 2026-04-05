<script setup lang="ts">
defineProps<{
  open: boolean
  title: string
}>()

defineEmits<{
  close: []
}>()
</script>

<template>
  <div v-if="open" class="modal-overlay" @click.self="$emit('close')">
    <article class="modal">
      <header class="modal-header">
        <h3>{{ title }}</h3>
        <button @click="$emit('close')">Close</button>
      </header>
      <div class="modal-body">
        <slot />
      </div>
    </article>
  </div>
</template>
