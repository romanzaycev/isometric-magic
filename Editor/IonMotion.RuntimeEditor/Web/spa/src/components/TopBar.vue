<script setup lang="ts">
const autoRefreshEnabled = defineModel<boolean>('autoRefreshEnabled', { required: true })
const autoRefreshSeconds = defineModel<number>('autoRefreshSeconds', { required: true })

defineProps<{
  status: string
}>()

defineEmits<{
  refreshAll: []
}>()
</script>

<template>
  <header class="topbar">
    <div class="topbar-left">
      <strong>🛠️ Runtime Editor</strong>
      <button @click="$emit('refreshAll')">🔄 Refresh All</button>
      <label class="inline">
        <input v-model="autoRefreshEnabled" type="checkbox" />
        ⏱ Auto Refresh
      </label>
      <input v-model.number="autoRefreshSeconds" type="number" min="0.3" step="0.1" class="seconds" />
      <span class="muted">sec</span>
    </div>
    <span class="status">{{ status }}</span>
  </header>
</template>
