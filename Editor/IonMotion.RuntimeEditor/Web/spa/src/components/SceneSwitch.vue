<script setup lang="ts">
const selectedSceneName = defineModel<string>('selectedSceneName', { required: true })

defineProps<{
  sceneNames: string[]
  currentScene: string
}>()

defineEmits<{
  switchScene: []
  openLighting: []
  openFrameStats: []
}>()
</script>

<template>
  <section class="scene-switch">
    <span>🎬 Scene:</span>
    <select v-model="selectedSceneName">
      <option v-for="name in sceneNames" :key="name" :value="name">{{ name }}</option>
    </select>
    <button @click="$emit('switchScene')">Load Scene</button>
    <button @click="$emit('openLighting')">Lighting</button>
    <button @click="$emit('openFrameStats')">Frame Stats</button>
    <span class="muted">Current: {{ currentScene }}</span>
  </section>
</template>
