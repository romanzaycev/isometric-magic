import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'

export type Member = {
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

export type EntityNode = {
  id: number
  name: string
  tag: string
  activeSelf: boolean
  activeInHierarchy: boolean
  components: string[]
  children: EntityNode[]
}

export function useRuntimeEditorState() {
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

  return {
    status,
    currentScene,
    sceneNames,
    selectedSceneName,
    treeFilter,
    selectedEntityId,
    selectedSpriteId,
    autoRefreshEnabled,
    autoRefreshSeconds,
    spriteLayers,
    lightingModalOpen,
    spriteModalOpen,
    inspectorFieldFilter,
    lights,
    ambientRows,
    spriteRows,
    entityView,
    transformView,
    componentViews,
    filteredTreeRows,
    shouldAutoOpenInspectorGroups,
    isEnumMember,
    isNumericMember,
    isVectorMember,
    isTreeCollapsed,
    toggleTreeNode,
    flattenedMembers,
    setValue,
    refreshAll,
    switchScene,
    openLightingModal,
    closeLightingModal,
    loadLighting,
    addLight,
    removeLight,
    openSpriteInspector,
    closeSpriteInspectorModal,
    loadEntityInspector
  }
}
