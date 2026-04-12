<script setup lang="ts">
import type { EntityNode } from '../composables/useRuntimeEditorState'

type TreeRow = EntityNode & { depth: number }

const treeFilter = defineModel<string>('treeFilter', { required: true })

defineProps<{
  rows: TreeRow[]
  selectedEntityId: number | null
  isTreeCollapsed: (nodeId: number) => boolean
}>()

defineEmits<{
  toggleNode: [nodeId: number]
  selectEntity: [nodeId: number]
}>()
</script>

<template>
  <article class="card">
    <h3>🌳 Entity Tree</h3>
    <input v-model="treeFilter" class="search" type="text" placeholder="Filter name/tag/component" />
    <div class="tree">
      <div
        v-for="node in rows"
        :key="node.id"
        class="tree-row"
        :class="{ selected: selectedEntityId === node.id }"
        :style="{ paddingLeft: `${node.depth * 16 + 8}px` }"
        @click="$emit('selectEntity', node.id)"
      >
        <button
          v-if="node.children && node.children.length > 0"
          class="tree-toggle"
          @click.stop="$emit('toggleNode', node.id)"
        >
          {{ isTreeCollapsed(node.id) ? '▶' : '▼' }}
        </button>
        <span v-else class="tree-toggle-placeholder"></span>
        {{ node.activeInHierarchy ? '🟢' : '⚫' }} {{ node.name }}
        <span class="muted">[{{ node.components.join(', ') }}]</span>
      </div>
    </div>
  </article>
</template>
