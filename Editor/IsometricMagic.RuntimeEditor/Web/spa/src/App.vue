<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'

type Member = {
  name: string
  path: string
  type: string
  editable: boolean
  value: unknown
  enumValues?: string[] | null
  step?: number | null
  min?: number | null
  max?: number | null
  children?: Member[] | null
}

type EntityNode = {
  id: number
  name: string
  tag: string
  activeSelf: boolean
  activeInHierarchy: boolean
  components: string[]
  children: EntityNode[]
}

const status = ref('Ready')
const currentScene = ref('')
const sceneNames = ref<string[]>([])
const selectedSceneName = ref('')

const treeRoot = ref<EntityNode | null>(null)
const treeFilter = ref('')
const selectedEntityId = ref<number | null>(null)
const collapsedTreeNodeIds = ref<Set<number>>(new Set())

const entityMembers = ref<Member[]>([])
const transformMembers = ref<Member[]>([])
const componentBlocks = ref<{ index: number; type: string; members: Member[] }[]>([])

const ambientMembers = ref<Member[]>([])
const lights = ref<{ id: number; index: number; members: Member[] }[]>([])

const spriteLayers = ref<{ name: string; sprites: { id: number; name: string; sorting: number; visible: boolean }[] }[]>([])
const selectedSpriteId = ref<number | null>(null)
const spriteMembers = ref<Member[]>([])
const spriteModalOpen = ref(false)

const lightingModalOpen = ref(false)

const inspectorFieldFilter = ref('')

const autoRefreshEnabled = ref(false)
const autoRefreshSeconds = ref(1.5)
let autoRefreshTimer: number | null = null

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, init)
  return await response.json() as T
}

function setStatus(text: string): void {
  status.value = text
}

function isVectorMember(member: Member): boolean {
  return member.type === 'Vector2' || member.type === 'Vector3' || member.type === 'Vector4'
}

function isNumericMember(member: Member): boolean {
  return member.type === 'Int32' || member.type === 'UInt32' || member.type === 'Int64' || member.type === 'Single' || member.type === 'Double'
}

function isEnumMember(member: Member): boolean {
  return !!member.enumValues && member.enumValues.length > 0
}

function isTreeCollapsed(nodeId: number): boolean {
  return collapsedTreeNodeIds.value.has(nodeId)
}

function toggleTreeNode(nodeId: number): void {
  const next = new Set(collapsedTreeNodeIds.value)
  if (next.has(nodeId)) {
    next.delete(nodeId)
  } else {
    next.add(nodeId)
  }

  collapsedTreeNodeIds.value = next
}

function flattenedMembers(members: Member[]): Array<Member & { depth: number }> {
  const rows: Array<Member & { depth: number }> = []

  const walk = (items: Member[], depth: number): void => {
    for (const item of items) {
      rows.push({ ...item, depth })
      if (item.children && item.children.length > 0) {
        walk(item.children, depth + 1)
      }
    }
  }

  walk(members, 0)
  return rows
}

function filterInspectorRows(rows: Array<Member & { depth: number }>): Array<Member & { depth: number }> {
  const normalized = inspectorFieldFilter.value.trim().toLowerCase()
  if (!normalized) {
    return rows
  }

  return rows.filter((row) => {
    const pathText = row.path.toLowerCase()
    const nameText = row.name.toLowerCase()
    return pathText.includes(normalized) || nameText.includes(normalized)
  })
}

function applyTreeFilter(node: EntityNode, filterText: string): EntityNode | null {
  const normalized = filterText.trim().toLowerCase()
  if (!normalized) {
    return node
  }

  const ownText = `${node.name} ${node.tag} ${(node.components || []).join(' ')}`.toLowerCase()
  const ownMatch = ownText.includes(normalized)

  const children = (node.children || [])
    .map((child) => applyTreeFilter(child, normalized))
    .filter((x): x is EntityNode => x !== null)

  if (ownMatch || children.length > 0) {
    return { ...node, children }
  }

  return null
}

function flattenTree(node: EntityNode | null, forceExpand: boolean): Array<EntityNode & { depth: number }> {
  if (!node) {
    return []
  }

  const rows: Array<EntityNode & { depth: number }> = []
  const walk = (item: EntityNode, depth: number): void => {
    rows.push({ ...item, depth })

    if (!forceExpand && isTreeCollapsed(item.id)) {
      return
    }

    for (const child of item.children || []) {
      walk(child, depth + 1)
    }
  }

  walk(node, 0)
  return rows
}

const filteredTreeRows = computed(() => {
  const filteredRoot = treeRoot.value ? applyTreeFilter(treeRoot.value, treeFilter.value) : null
  const forceExpand = treeFilter.value.trim().length > 0
  return flattenTree(filteredRoot, forceExpand)
})

const entityView = computed(() => {
  const rows = filterInspectorRows(flattenedMembers(entityMembers.value))
  return {
    editableRows: rows.filter((row) => row.editable),
    readonlyRows: rows.filter((row) => !row.editable)
  }
})

const transformView = computed(() => {
  const rows = filterInspectorRows(flattenedMembers(transformMembers.value))
  return {
    editableRows: rows.filter((row) => row.editable),
    readonlyRows: rows.filter((row) => !row.editable)
  }
})

const ambientRows = computed(() => flattenedMembers(ambientMembers.value))
const spriteRows = computed(() => flattenedMembers(spriteMembers.value))
const componentViews = computed(() => {
  return componentBlocks.value.map((component) => {
    const rows = filterInspectorRows(flattenedMembers(component.members))
    return {
      index: component.index,
      type: component.type,
      editableRows: rows.filter((row) => row.editable),
      readonlyRows: rows.filter((row) => !row.editable)
    }
  })
})

const shouldAutoOpenInspectorGroups = computed(() => inspectorFieldFilter.value.trim().length > 0)

async function loadScene(): Promise<void> {
  const data = await api<{ scene: string; scenes: string[]; root: EntityNode }>('/api/scene')
  currentScene.value = data.scene
  sceneNames.value = data.scenes ?? []
  selectedSceneName.value = data.scene
  treeRoot.value = data.root
  collapsedTreeNodeIds.value = new Set()
}

async function loadEntityInspector(entityId: number): Promise<void> {
  const data = await api<{
    found: boolean
    entity?: {
      entityMembers: Member[]
      transformMembers: Member[]
      components: { index: number; type: string; members: Member[] }[]
    }
  }>(`/api/entity/${entityId}`)

  if (!data.found || !data.entity) {
    entityMembers.value = []
    transformMembers.value = []
    componentBlocks.value = []
    return
  }

  entityMembers.value = data.entity.entityMembers ?? []
  transformMembers.value = data.entity.transformMembers ?? []
  componentBlocks.value = data.entity.components ?? []
}

async function loadLighting(): Promise<void> {
  const data = await api<{
    ambientMembers: Member[]
    lights: { id: number; index: number; members: Member[] }[]
  }>('/api/lighting')

  ambientMembers.value = data.ambientMembers ?? []
  lights.value = data.lights ?? []
}

async function loadSprites(): Promise<void> {
  const data = await api<{
    layers: { name: string; sprites: { id: number; name: string; sorting: number; visible: boolean }[] }[]
  }>('/api/sprites')

  spriteLayers.value = data.layers ?? []
}

async function loadSpriteInspector(spriteId: number): Promise<void> {
  const data = await api<{ found: boolean; sprite?: { members: Member[] } }>(`/api/sprite/${spriteId}`)
  spriteMembers.value = data.found && data.sprite ? data.sprite.members : []
}

async function openSpriteInspector(spriteId: number): Promise<void> {
  selectedSpriteId.value = spriteId
  await loadSpriteInspector(spriteId)
  spriteModalOpen.value = true
}

function closeSpriteInspectorModal(): void {
  spriteModalOpen.value = false
}

async function openLightingModal(): Promise<void> {
  await loadLighting()
  lightingModalOpen.value = true
}

function closeLightingModal(): void {
  lightingModalOpen.value = false
}

async function setValue(payload: unknown): Promise<void> {
  const result = await api<{ ok: boolean; error?: string }>('/api/set', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  })

  if (!result.ok) {
    setStatus(`Apply failed: ${result.error ?? 'unknown error'}`)
    return
  }

  setStatus('Applied ✅')
}

async function refreshAll(): Promise<void> {
  await loadScene()
  if (selectedEntityId.value !== null) {
    await loadEntityInspector(selectedEntityId.value)
  }
  await loadLighting()
  await loadSprites()
  if (selectedSpriteId.value !== null) {
    await loadSpriteInspector(selectedSpriteId.value)
  }
}

async function switchScene(): Promise<void> {
  if (!selectedSceneName.value) {
    return
  }

  const result = await api<{ ok: boolean; error?: string }>('/api/scenes/load', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: selectedSceneName.value })
  })

  if (!result.ok) {
    setStatus(`Scene switch failed: ${result.error ?? 'unknown error'}`)
    return
  }

  selectedEntityId.value = null
  selectedSpriteId.value = null
  spriteModalOpen.value = false
  entityMembers.value = []
  transformMembers.value = []
  componentBlocks.value = []
  spriteMembers.value = []
  await refreshAll()
  setStatus(`Scene switched 🎬 ${selectedSceneName.value}`)
}

async function addLight(): Promise<void> {
  const result = await api<{ ok: boolean; error?: string }>('/api/light/add', { method: 'POST' })
  if (!result.ok) {
    setStatus(`Add light failed: ${result.error ?? 'unknown error'}`)
    return
  }

  await loadLighting()
  setStatus('Light added 💡')
}

async function removeLight(lightId: number): Promise<void> {
  const result = await api<{ ok: boolean; error?: string }>('/api/light/remove', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ lightId })
  })

  if (!result.ok) {
    setStatus(`Remove light failed: ${result.error ?? 'unknown error'}`)
    return
  }

  await loadLighting()
  setStatus('Light removed 🗑️')
}

function startAutoRefresh(): void {
  stopAutoRefresh()
  if (!autoRefreshEnabled.value) {
    return
  }

  const intervalMs = Math.max(300, Math.floor(autoRefreshSeconds.value * 1000))
  autoRefreshTimer = window.setInterval(() => {
    void refreshAll()
  }, intervalMs)
}

function stopAutoRefresh(): void {
  if (autoRefreshTimer !== null) {
    window.clearInterval(autoRefreshTimer)
    autoRefreshTimer = null
  }
}

watch([autoRefreshEnabled, autoRefreshSeconds], () => startAutoRefresh())

onMounted(async () => {
  try {
    await refreshAll()
    setStatus('Ready ✅')
  } catch (error) {
    setStatus(`Init failed: ${String(error)}`)
  }
})

onBeforeUnmount(() => stopAutoRefresh())
</script>

<template>
  <div class="page">
    <header class="topbar">
      <div class="topbar-left">
        <strong>🛠️ Runtime Editor</strong>
        <button @click="refreshAll">🔄 Refresh All</button>
        <label class="inline">
          <input v-model="autoRefreshEnabled" type="checkbox" />
          ⏱ Auto Refresh
        </label>
        <input v-model.number="autoRefreshSeconds" type="number" min="0.3" step="0.1" class="seconds" />
        <span class="muted">sec</span>
      </div>
      <span class="status">{{ status }}</span>
    </header>

    <section class="scene-switch">
      <span>🎬 Scene:</span>
      <select v-model="selectedSceneName">
        <option v-for="name in sceneNames" :key="name" :value="name">{{ name }}</option>
      </select>
      <button @click="switchScene">Load Scene</button>
      <button @click="openLightingModal">Lighting</button>
      <span class="muted">Current: {{ currentScene }}</span>
    </section>

    <main class="layout">
      <section class="column">
        <article class="card">
          <h3>🌳 Entity Tree</h3>
          <input v-model="treeFilter" class="search" type="text" placeholder="Filter name/tag/component" />
          <div class="tree">
            <div
              v-for="node in filteredTreeRows"
              :key="node.id"
              class="tree-row"
              :class="{ selected: selectedEntityId === node.id }"
              :style="{ paddingLeft: `${node.depth * 16 + 8}px` }"
              @click="selectedEntityId = node.id; loadEntityInspector(node.id)"
            >
              <button
                v-if="node.children && node.children.length > 0"
                class="tree-toggle"
                @click.stop="toggleTreeNode(node.id)"
              >
                {{ isTreeCollapsed(node.id) ? '▶' : '▼' }}
              </button>
              <span v-else class="tree-toggle-placeholder"></span>
              {{ node.activeInHierarchy ? '🟢' : '⚫' }} {{ node.name }}
              <span class="muted">[{{ node.components.join(', ') }}]</span>
            </div>
          </div>
        </article>

        <article class="card">
          <h3>🖼️ Sprites</h3>
          <div v-for="layer in spriteLayers" :key="layer.name" class="layer-block">
            <h4>{{ layer.name }}</h4>
            <template v-for="sprite in layer.sprites" :key="sprite.id">
              <div
                v-if="sprite.name !== '(unnamed)'"
                class="tree-row"
                :class="{ selected: selectedSpriteId === sprite.id }"
                @click="openSpriteInspector(sprite.id)"
              >
                {{ sprite.visible ? '👁️' : '🚫' }} {{ sprite.name }}
                <span class="muted">(sorting {{ sprite.sorting }})</span>
              </div>
            </template>
          </div>
        </article>
      </section>

      <section class="column">
        <article class="card">
          <h3>🔎 Entity Inspector</h3>
          <input
            v-model="inspectorFieldFilter"
            class="search inspector-filter"
            type="text"
            placeholder="Filter by field name"
          />

          <details class="inspector-group" :open="shouldAutoOpenInspectorGroups">
            <summary class="group-summary">👤 Entity</summary>
            <div v-for="member in entityView.editableRows" :key="`entity-${member.path}`" class="row" :style="{ marginLeft: `${member.depth * 14}px` }">
              <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
              <input
                v-if="member.editable && member.type === 'Boolean'"
                type="checkbox"
                :checked="Boolean(member.value)"
                @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: ($event.target as HTMLInputElement).checked })"
              />
              <select
                v-else-if="member.editable && isEnumMember(member)"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: ($event.target as HTMLSelectElement).value })"
              >
                <option v-for="enumValue in member.enumValues" :key="`entity-${member.path}-${enumValue}`" :value="enumValue">{{ enumValue }}</option>
              </select>
              <input
                v-else-if="member.editable && isNumericMember(member)"
                :type="member.type === 'Int64' ? 'text' : 'number'"
                :value="String(member.value ?? '')"
                :step="member.step ?? 0.1"
                :min="member.min ?? undefined"
                :max="member.max ?? undefined"
                @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: member.type === 'Int64' ? ($event.target as HTMLInputElement).value : Number(($event.target as HTMLInputElement).value) })"
              />
              <div v-else-if="member.editable && isVectorMember(member)" class="vec">
                <input type="number" step="0.1" :value="(member.value as any)?.x ?? 0" @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), x: Number(($event.target as HTMLInputElement).value) } })" />
                <input type="number" step="0.1" :value="(member.value as any)?.y ?? 0" @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), y: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type !== 'Vector2'" type="number" step="0.1" :value="(member.value as any)?.z ?? 0" @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), z: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type === 'Vector4'" type="number" step="0.1" :value="(member.value as any)?.w ?? 0" @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), w: Number(($event.target as HTMLInputElement).value) } })" />
              </div>
              <input
                v-else-if="member.editable"
                type="text"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'entity', entityId: selectedEntityId, path: member.path, value: ($event.target as HTMLInputElement).value })"
              />
              <div v-else class="readonly">{{ String(member.value ?? 'null') }}</div>
            </div>

            <details v-if="entityView.readonlyRows.length > 0" class="readonly-group" :open="shouldAutoOpenInspectorGroups">
              <summary>📦 Read-only public members ({{ entityView.readonlyRows.length }})</summary>
              <div v-for="member in entityView.readonlyRows" :key="`entity-ro-${member.path}`" class="row readonly-row" :style="{ marginLeft: `${member.depth * 14}px` }">
                <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
                <div class="readonly">{{ String(member.value ?? 'null') }}</div>
              </div>
            </details>
          </details>

          <details class="inspector-group" :open="shouldAutoOpenInspectorGroups">
            <summary class="group-summary">🧭 Transform</summary>
            <div v-for="member in transformView.editableRows" :key="`transform-${member.path}`" class="row" :style="{ marginLeft: `${member.depth * 14}px` }">
              <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
              <div v-if="member.editable && isVectorMember(member)" class="vec">
                <input type="number" step="0.1" :value="(member.value as any)?.x ?? 0" @change="setValue({ target: 'transform', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), x: Number(($event.target as HTMLInputElement).value) } })" />
                <input type="number" step="0.1" :value="(member.value as any)?.y ?? 0" @change="setValue({ target: 'transform', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), y: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type !== 'Vector2'" type="number" step="0.1" :value="(member.value as any)?.z ?? 0" @change="setValue({ target: 'transform', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), z: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type === 'Vector4'" type="number" step="0.1" :value="(member.value as any)?.w ?? 0" @change="setValue({ target: 'transform', entityId: selectedEntityId, path: member.path, value: { ...(member.value as any), w: Number(($event.target as HTMLInputElement).value) } })" />
              </div>
              <select
                v-else-if="member.editable && isEnumMember(member)"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'transform', entityId: selectedEntityId, path: member.path, value: ($event.target as HTMLSelectElement).value })"
              >
                <option v-for="enumValue in member.enumValues" :key="`transform-${member.path}-${enumValue}`" :value="enumValue">{{ enumValue }}</option>
              </select>
              <input
                v-else-if="member.editable"
                :type="isNumericMember(member) && member.type !== 'Int64' ? 'number' : 'text'"
                :step="member.step ?? 0.1"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'transform', entityId: selectedEntityId, path: member.path, value: isNumericMember(member) && member.type !== 'Int64' ? Number(($event.target as HTMLInputElement).value) : ($event.target as HTMLInputElement).value })"
              />
              <div v-else class="readonly">{{ String(member.value ?? 'null') }}</div>
            </div>

            <details v-if="transformView.readonlyRows.length > 0" class="readonly-group" :open="shouldAutoOpenInspectorGroups">
              <summary>📦 Read-only public members ({{ transformView.readonlyRows.length }})</summary>
              <div v-for="member in transformView.readonlyRows" :key="`transform-ro-${member.path}`" class="row readonly-row" :style="{ marginLeft: `${member.depth * 14}px` }">
                <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
                <div class="readonly">{{ String(member.value ?? 'null') }}</div>
              </div>
            </details>
          </details>

          <details v-for="component in componentViews" :key="`component-${component.index}`" class="inspector-group" :open="shouldAutoOpenInspectorGroups">
            <summary class="group-summary">🧩 {{ component.type }}</summary>
            <div v-for="member in component.editableRows" :key="`component-${component.index}-${member.path}`" class="row" :style="{ marginLeft: `${member.depth * 14}px` }">
              <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
              <div v-if="member.editable && member.type === 'Boolean'">
                <input type="checkbox" :checked="Boolean(member.value)" @change="setValue({ target: 'component', entityId: selectedEntityId, componentIndex: component.index, path: member.path, value: ($event.target as HTMLInputElement).checked })" />
              </div>
              <div v-else-if="member.editable && isVectorMember(member)" class="vec">
                <input type="number" step="0.1" :value="(member.value as any)?.x ?? 0" @change="setValue({ target: 'component', entityId: selectedEntityId, componentIndex: component.index, path: member.path, value: { ...(member.value as any), x: Number(($event.target as HTMLInputElement).value) } })" />
                <input type="number" step="0.1" :value="(member.value as any)?.y ?? 0" @change="setValue({ target: 'component', entityId: selectedEntityId, componentIndex: component.index, path: member.path, value: { ...(member.value as any), y: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type !== 'Vector2'" type="number" step="0.1" :value="(member.value as any)?.z ?? 0" @change="setValue({ target: 'component', entityId: selectedEntityId, componentIndex: component.index, path: member.path, value: { ...(member.value as any), z: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type === 'Vector4'" type="number" step="0.1" :value="(member.value as any)?.w ?? 0" @change="setValue({ target: 'component', entityId: selectedEntityId, componentIndex: component.index, path: member.path, value: { ...(member.value as any), w: Number(($event.target as HTMLInputElement).value) } })" />
              </div>
              <select
                v-else-if="member.editable && isEnumMember(member)"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'component', entityId: selectedEntityId, componentIndex: component.index, path: member.path, value: ($event.target as HTMLSelectElement).value })"
              >
                <option v-for="enumValue in member.enumValues" :key="`component-${component.index}-${member.path}-${enumValue}`" :value="enumValue">{{ enumValue }}</option>
              </select>
              <input
                v-else-if="member.editable"
                :type="isNumericMember(member) && member.type !== 'Int64' ? 'number' : 'text'"
                :step="member.step ?? 0.1"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'component', entityId: selectedEntityId, componentIndex: component.index, path: member.path, value: isNumericMember(member) && member.type !== 'Int64' ? Number(($event.target as HTMLInputElement).value) : ($event.target as HTMLInputElement).value })"
              />
              <div v-else class="readonly">{{ String(member.value ?? 'null') }}</div>
            </div>

            <details v-if="component.readonlyRows.length > 0" class="readonly-group" :open="shouldAutoOpenInspectorGroups">
              <summary>📦 Read-only public members ({{ component.readonlyRows.length }})</summary>
              <div v-for="member in component.readonlyRows" :key="`component-ro-${component.index}-${member.path}`" class="row readonly-row" :style="{ marginLeft: `${member.depth * 14}px` }">
                <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
                <div class="readonly">{{ String(member.value ?? 'null') }}</div>
              </div>
            </details>
          </details>
        </article>
      </section>
    </main>

    <div v-if="lightingModalOpen" class="modal-overlay" @click.self="closeLightingModal">
      <article class="modal">
        <header class="modal-header">
          <h3>💡 Lighting</h3>
          <button @click="closeLightingModal">Close</button>
        </header>
        <div class="modal-body">
          <div class="actions">
            <button @click="loadLighting">🔄 Refresh Lighting</button>
            <button @click="addLight">➕ Add Light</button>
          </div>

          <h4>Ambient</h4>
          <div v-for="member in ambientRows" :key="`ambient-${member.path}`" class="row" :style="{ marginLeft: `${member.depth * 14}px` }">
            <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
            <div v-if="member.editable && isVectorMember(member)" class="vec">
              <input type="number" step="0.1" :value="(member.value as any)?.x ?? 0" @change="setValue({ target: 'lighting', path: member.path, value: { ...(member.value as any), x: Number(($event.target as HTMLInputElement).value) } })" />
              <input type="number" step="0.1" :value="(member.value as any)?.y ?? 0" @change="setValue({ target: 'lighting', path: member.path, value: { ...(member.value as any), y: Number(($event.target as HTMLInputElement).value) } })" />
              <input v-if="member.type !== 'Vector2'" type="number" step="0.1" :value="(member.value as any)?.z ?? 0" @change="setValue({ target: 'lighting', path: member.path, value: { ...(member.value as any), z: Number(($event.target as HTMLInputElement).value) } })" />
              <input v-if="member.type === 'Vector4'" type="number" step="0.1" :value="(member.value as any)?.w ?? 0" @change="setValue({ target: 'lighting', path: member.path, value: { ...(member.value as any), w: Number(($event.target as HTMLInputElement).value) } })" />
            </div>
            <select
              v-else-if="member.editable && isEnumMember(member)"
              :value="String(member.value ?? '')"
              @change="setValue({ target: 'lighting', path: member.path, value: ($event.target as HTMLSelectElement).value })"
            >
              <option v-for="enumValue in member.enumValues" :key="`ambient-${member.path}-${enumValue}`" :value="enumValue">{{ enumValue }}</option>
            </select>
            <input
              v-else-if="member.editable"
              :type="isNumericMember(member) && member.type !== 'Int64' ? 'number' : 'text'"
              :step="member.step ?? 0.1"
              :value="String(member.value ?? '')"
              @change="setValue({ target: 'lighting', path: member.path, value: isNumericMember(member) && member.type !== 'Int64' ? Number(($event.target as HTMLInputElement).value) : ($event.target as HTMLInputElement).value })"
            />
            <div v-else class="readonly">{{ String(member.value ?? 'null') }}</div>
          </div>

          <template v-for="light in lights" :key="light.id">
            <h4>Point Light #{{ light.index }} <button class="tiny" @click="removeLight(light.id)">🗑️ Remove</button></h4>
            <div v-for="member in flattenedMembers(light.members)" :key="`light-${light.id}-${member.path}`" class="row" :style="{ marginLeft: `${member.depth * 14}px` }">
              <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
              <div v-if="member.editable && isVectorMember(member)" class="vec">
                <input type="number" step="0.1" :value="(member.value as any)?.x ?? 0" @change="setValue({ target: 'light', lightId: light.id, path: member.path, value: { ...(member.value as any), x: Number(($event.target as HTMLInputElement).value) } })" />
                <input type="number" step="0.1" :value="(member.value as any)?.y ?? 0" @change="setValue({ target: 'light', lightId: light.id, path: member.path, value: { ...(member.value as any), y: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type !== 'Vector2'" type="number" step="0.1" :value="(member.value as any)?.z ?? 0" @change="setValue({ target: 'light', lightId: light.id, path: member.path, value: { ...(member.value as any), z: Number(($event.target as HTMLInputElement).value) } })" />
                <input v-if="member.type === 'Vector4'" type="number" step="0.1" :value="(member.value as any)?.w ?? 0" @change="setValue({ target: 'light', lightId: light.id, path: member.path, value: { ...(member.value as any), w: Number(($event.target as HTMLInputElement).value) } })" />
              </div>
              <select
                v-else-if="member.editable && isEnumMember(member)"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'light', lightId: light.id, path: member.path, value: ($event.target as HTMLSelectElement).value })"
              >
                <option v-for="enumValue in member.enumValues" :key="`light-${light.id}-${member.path}-${enumValue}`" :value="enumValue">{{ enumValue }}</option>
              </select>
              <input
                v-else-if="member.editable"
                :type="isNumericMember(member) && member.type !== 'Int64' ? 'number' : 'text'"
                :step="member.step ?? 0.1"
                :value="String(member.value ?? '')"
                @change="setValue({ target: 'light', lightId: light.id, path: member.path, value: isNumericMember(member) && member.type !== 'Int64' ? Number(($event.target as HTMLInputElement).value) : ($event.target as HTMLInputElement).value })"
              />
              <div v-else class="readonly">{{ String(member.value ?? 'null') }}</div>
            </div>
          </template>
        </div>
      </article>
    </div>

    <div v-if="spriteModalOpen && selectedSpriteId !== null" class="modal-overlay" @click.self="closeSpriteInspectorModal">
      <article class="modal">
        <header class="modal-header">
          <h3>🎨 Sprite Inspector</h3>
          <button @click="closeSpriteInspectorModal">Close</button>
        </header>
        <div class="modal-body">
          <div v-for="member in spriteRows" :key="`sprite-${member.path}`" class="row" :style="{ marginLeft: `${member.depth * 14}px` }">
            <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>
            <div v-if="member.editable && member.type === 'Boolean'">
              <input type="checkbox" :checked="Boolean(member.value)" @change="setValue({ target: 'sprite', spriteId: selectedSpriteId, path: member.path, value: ($event.target as HTMLInputElement).checked })" />
            </div>
            <div v-else-if="member.editable && isVectorMember(member)" class="vec">
              <input type="number" step="0.1" :value="(member.value as any)?.x ?? 0" @change="setValue({ target: 'sprite', spriteId: selectedSpriteId, path: member.path, value: { ...(member.value as any), x: Number(($event.target as HTMLInputElement).value) } })" />
              <input type="number" step="0.1" :value="(member.value as any)?.y ?? 0" @change="setValue({ target: 'sprite', spriteId: selectedSpriteId, path: member.path, value: { ...(member.value as any), y: Number(($event.target as HTMLInputElement).value) } })" />
              <input v-if="member.type !== 'Vector2'" type="number" step="0.1" :value="(member.value as any)?.z ?? 0" @change="setValue({ target: 'sprite', spriteId: selectedSpriteId, path: member.path, value: { ...(member.value as any), z: Number(($event.target as HTMLInputElement).value) } })" />
              <input v-if="member.type === 'Vector4'" type="number" step="0.1" :value="(member.value as any)?.w ?? 0" @change="setValue({ target: 'sprite', spriteId: selectedSpriteId, path: member.path, value: { ...(member.value as any), w: Number(($event.target as HTMLInputElement).value) } })" />
            </div>
            <select
              v-else-if="member.editable && isEnumMember(member)"
              :value="String(member.value ?? '')"
              @change="setValue({ target: 'sprite', spriteId: selectedSpriteId, path: member.path, value: ($event.target as HTMLSelectElement).value })"
            >
              <option v-for="enumValue in member.enumValues" :key="`sprite-${member.path}-${enumValue}`" :value="enumValue">{{ enumValue }}</option>
            </select>
            <input
              v-else-if="member.editable"
              :type="isNumericMember(member) && member.type !== 'Int64' ? 'number' : 'text'"
              :step="member.step ?? 0.1"
              :value="String(member.value ?? '')"
              @change="setValue({ target: 'sprite', spriteId: selectedSpriteId, path: member.path, value: isNumericMember(member) && member.type !== 'Int64' ? Number(($event.target as HTMLInputElement).value) : ($event.target as HTMLInputElement).value })"
            />
            <div v-else class="readonly">{{ String(member.value ?? 'null') }}</div>
          </div>
        </div>
      </article>
    </div>
  </div>
</template>

<style>
:root {
  color-scheme: light;
}

* {
  box-sizing: border-box;
}

body {
  margin: 0;
  background: radial-gradient(circle at top left, #f4f1e6 0%, #d8deea 52%, #c3ccd8 100%);
  color: #162028;
  font: 14px/1.45 "IBM Plex Mono", "Fira Code", monospace;
}

.page {
  min-height: 100vh;
}

.topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  border-bottom: 1px solid #a5b4c9;
  background: rgba(247, 250, 255, 0.85);
  backdrop-filter: blur(4px);
}

.topbar-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.scene-switch {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  flex-wrap: wrap;
}

.layout {
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: 10px;
  padding: 0 10px 10px;
}

.column {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-width: 0;
}

.card {
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid #a6b2c4;
  border-radius: 10px;
  padding: 10px;
  box-shadow: 0 6px 16px rgba(37, 56, 84, 0.1);
}

.tree {
  max-height: 420px;
  overflow: auto;
  border: 1px solid #d6deeb;
  border-radius: 8px;
  background: #f8fbff;
}

.search,
input,
select,
button {
  font: inherit;
}

.search,
input,
select {
  border: 1px solid #a9b7cd;
  border-radius: 6px;
  padding: 5px 7px;
  min-width: 0;
}

button {
  border: 1px solid #8e9db6;
  border-radius: 6px;
  padding: 4px 8px;
  background: #eef3fb;
  cursor: pointer;
}

button:hover {
  background: #dde7f5;
}

.tree-row {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 8px;
  border-bottom: 1px solid #e5edf8;
  cursor: pointer;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tree-row.selected {
  background: #d4e2f8;
}

.tree-toggle,
.tree-toggle-placeholder {
  width: 22px;
  min-width: 22px;
  height: 22px;
}

.tree-toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
}

.tree-toggle-placeholder {
  display: inline-block;
}

.muted {
  opacity: 0.68;
}

.status {
  text-align: right;
  max-width: 42%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.row {
  display: grid;
  grid-template-columns: minmax(140px, 1fr) minmax(160px, 1fr);
  gap: 8px;
  align-items: center;
  margin: 4px 0;
}

.vec {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 6px;
}

.readonly {
  color: #4c5c73;
}

.readonly-group {
  margin-top: 6px;
  border: 1px dashed #b8c3d6;
  border-radius: 6px;
  padding: 6px;
}

.inspector-filter {
  margin-bottom: 8px;
  width: 100%;
}

.inspector-group {
  margin: 10px 0;
  padding: 8px;
  border: 1px solid #b7c3d6;
  border-radius: 8px;
  background: rgba(239, 244, 252, 0.55);
}

.group-summary {
  cursor: pointer;
  user-select: none;
  font-weight: 600;
}

.readonly-group > summary {
  cursor: pointer;
  user-select: none;
}

.readonly-row {
  opacity: 0.82;
}

.actions {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
}

.tiny {
  padding: 2px 6px;
  margin-left: 8px;
}

.seconds {
  width: 72px;
}

.inline {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.layer-block {
  margin-bottom: 8px;
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(10, 20, 32, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  z-index: 40;
}

.modal {
  width: min(1100px, 96vw);
  max-height: 88vh;
  overflow: auto;
  border: 1px solid #8e9db6;
  border-radius: 10px;
  background: #f5f9ff;
  box-shadow: 0 16px 38px rgba(17, 33, 53, 0.35);
}

.modal-header {
  position: sticky;
  top: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 10px 12px;
  border-bottom: 1px solid #c2cede;
  background: #eaf2ff;
}

.modal-header h3 {
  margin: 0;
}

.modal-body {
  padding: 10px 12px 14px;
}

@media (max-width: 1280px) {
  .layout {
    grid-template-columns: 1fr;
  }

  .status {
    max-width: 100%;
  }
}
</style>
