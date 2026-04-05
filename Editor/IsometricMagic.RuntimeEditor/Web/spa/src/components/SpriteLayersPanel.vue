<script setup lang="ts">
defineProps<{
  spriteLayers: { name: string; sprites: { id: number; name: string; sorting: number; visible: boolean }[] }[]
  selectedSpriteId: number | null
}>()

defineEmits<{
  openSpriteInspector: [spriteId: number]
}>()
</script>

<template>
  <article class="card">
    <h3>🖼️ Sprites</h3>
    <div v-for="layer in spriteLayers" :key="layer.name" class="layer-block">
      <h4>{{ layer.name }}</h4>
      <template v-for="sprite in layer.sprites" :key="sprite.id">
        <div
          v-if="sprite.name !== '(unnamed)'"
          class="tree-row"
          :class="{ selected: selectedSpriteId === sprite.id }"
          @click="$emit('openSpriteInspector', sprite.id)"
        >
          {{ sprite.visible ? '👁️' : '🚫' }} {{ sprite.name }}
          <span class="muted">(sorting {{ sprite.sorting }})</span>
        </div>
      </template>
    </div>
  </article>
</template>
