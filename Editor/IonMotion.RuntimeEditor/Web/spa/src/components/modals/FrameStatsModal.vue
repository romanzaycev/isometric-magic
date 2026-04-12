<script setup lang="ts">
import type { FrameStatsPayload } from '../../composables/useRuntimeEditorState'
import ModalShell from './ModalShell.vue'

defineProps<{
  open: boolean
  stats: FrameStatsPayload | null
}>()

const emit = defineEmits<{
  close: []
  refresh: []
}>()

function formatBytes(value: number | undefined): string {
  if (!value || value <= 0) {
    return '0 B'
  }

  if (value < 1024) {
    return `${value} B`
  }

  if (value < 1024 * 1024) {
    return `${(value / 1024).toFixed(1)} KB`
  }

  return `${(value / (1024 * 1024)).toFixed(2)} MB`
}
</script>

<template>
  <ModalShell :open="open" title="📈 Frame Stats" @close="emit('close')">
    <div class="actions">
      <button @click="emit('refresh')">🔄 Refresh</button>
      <span class="muted">Live update: 4 Hz</span>
    </div>

    <template v-if="stats">
      <h4>Timing</h4>
      <div class="stats-grid">
        <div>Frame: <strong>{{ stats.timing.frameMs.toFixed(2) }} ms</strong></div>
        <div>Frame Avg: <strong>{{ stats.timing.frameMsAvg.toFixed(2) }} ms</strong></div>
        <div>FPS Avg: <strong>{{ stats.timing.fpsAvg.toFixed(1) }}</strong></div>
        <div>Events: <strong>{{ stats.timing.eventLoopMs.toFixed(2) }} ms</strong></div>
        <div>Update CPU: <strong>{{ stats.timing.updateCpuMs.toFixed(2) }} ms</strong></div>
        <div>Render CPU: <strong>{{ stats.timing.renderCpuMs.toFixed(2) }} ms</strong></div>
        <div>Sleep: <strong>{{ stats.timing.sleepMs.toFixed(2) }} ms</strong></div>
      </div>

      <h4>Update</h4>
      <div class="stats-grid">
        <div>Active Entities: <strong>{{ stats.update.activeEntities }}</strong></div>
        <div>Components Updated: <strong>{{ stats.update.componentsUpdated }}</strong></div>
        <div>Components LateUpdated: <strong>{{ stats.update.componentsLateUpdated }}</strong></div>
      </div>

      <h4>Render</h4>
      <div class="stats-grid">
        <div>Sprites Visited: <strong>{{ stats.render.spritesVisited }}</strong></div>
        <div>Sprites Skipped: <strong>{{ stats.render.spritesSkipped }}</strong></div>
        <div>Sprites Drawn: <strong>{{ stats.render.spritesDrawn }}</strong></div>
        <div>Sprites Culled: <strong>{{ stats.render.spritesCulled }}</strong></div>
        <div>Draw Calls: <strong>{{ stats.render.drawCalls }}</strong></div>
        <div>Texture Binds: <strong>{{ stats.render.textureBinds }}</strong></div>
        <div>Texture Loads: <strong>{{ stats.render.textureLoads }}</strong></div>
      </div>

      <h4>Memory</h4>
      <div class="stats-grid">
        <div>GC Alloc / Frame: <strong>{{ formatBytes(stats.memory.gcAllocBytes) }}</strong></div>
      </div>

      <h4>Meta</h4>
      <div class="stats-grid">
        <div>Scene: <strong>{{ stats.meta.sceneName }}</strong></div>
        <div>Viewport: <strong>{{ stats.meta.viewportWidth }}x{{ stats.meta.viewportHeight }}</strong></div>
        <div>Backend: <strong>{{ stats.meta.backend }}</strong></div>
        <div>VSync: <strong>{{ stats.meta.vsync ? 'On' : 'Off' }}</strong></div>
      </div>
    </template>

    <p v-else class="muted">No stats yet</p>
  </ModalShell>
</template>
