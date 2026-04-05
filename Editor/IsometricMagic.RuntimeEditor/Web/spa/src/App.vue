<script setup lang="ts">
import EntityInspectorPanel from './components/EntityInspectorPanel.vue'
import EntityTreePanel from './components/EntityTreePanel.vue'
import SceneSwitch from './components/SceneSwitch.vue'
import SpriteLayersPanel from './components/SpriteLayersPanel.vue'
import TopBar from './components/TopBar.vue'
import LightingModal from './components/modals/LightingModal.vue'
import SpriteInspectorModal from './components/modals/SpriteInspectorModal.vue'
import { useRuntimeEditorState } from './composables/useRuntimeEditorState'
import type { Member } from './composables/useRuntimeEditorState'

type MemberRow = Member & { depth: number }

const {
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
} = useRuntimeEditorState()

async function selectEntity(entityId: number): Promise<void> {
  selectedEntityId.value = entityId
  await loadEntityInspector(entityId)
}

function editEntity(member: MemberRow, value: unknown): void {
  void setValue({ target: 'entity', entityId: selectedEntityId.value, path: member.path, value })
}

function editTransform(member: MemberRow, value: unknown): void {
  void setValue({ target: 'transform', entityId: selectedEntityId.value, path: member.path, value })
}

function editComponent(componentIndex: number, member: MemberRow, value: unknown): void {
  void setValue({ target: 'component', entityId: selectedEntityId.value, componentIndex, path: member.path, value })
}

function editAmbient(member: MemberRow, value: unknown): void {
  void setValue({ target: 'lighting', path: member.path, value })
}

function editLight(lightId: number, member: MemberRow, value: unknown): void {
  void setValue({ target: 'light', lightId, path: member.path, value })
}

function editSprite(member: MemberRow, value: unknown): void {
  void setValue({ target: 'sprite', spriteId: selectedSpriteId.value, path: member.path, value })
}
</script>

<template>
  <div class="page">
    <TopBar
      v-model:auto-refresh-enabled="autoRefreshEnabled"
      v-model:auto-refresh-seconds="autoRefreshSeconds"
      :status="status"
      @refresh-all="refreshAll"
    />

    <SceneSwitch
      v-model:selected-scene-name="selectedSceneName"
      :scene-names="sceneNames"
      :current-scene="currentScene"
      @switch-scene="switchScene"
      @open-lighting="openLightingModal"
    />

    <main class="layout">
      <section class="column">
        <EntityTreePanel
          v-model:tree-filter="treeFilter"
          :rows="filteredTreeRows"
          :selected-entity-id="selectedEntityId"
          :is-tree-collapsed="isTreeCollapsed"
          @toggle-node="toggleTreeNode"
          @select-entity="selectEntity"
        />

        <SpriteLayersPanel
          :sprite-layers="spriteLayers"
          :selected-sprite-id="selectedSpriteId"
          @open-sprite-inspector="openSpriteInspector"
        />
      </section>

      <section class="column">
        <EntityInspectorPanel
          v-model:inspector-field-filter="inspectorFieldFilter"
          :selected-entity-id="selectedEntityId"
          :should-auto-open-inspector-groups="shouldAutoOpenInspectorGroups"
          :entity-view="entityView"
          :transform-view="transformView"
          :component-views="componentViews"
          @edit-entity="editEntity"
          @edit-transform="editTransform"
          @edit-component="editComponent"
        />
      </section>
    </main>

    <LightingModal
      :open="lightingModalOpen"
      :ambient-rows="ambientRows"
      :lights="lights"
      :flattened-members="flattenedMembers"
      @close="closeLightingModal"
      @refresh-lighting="loadLighting"
      @add-light="addLight"
      @remove-light="removeLight"
      @edit-ambient="editAmbient"
      @edit-light="editLight"
    />

    <SpriteInspectorModal
      :open="spriteModalOpen"
      :selected-sprite-id="selectedSpriteId"
      :sprite-rows="spriteRows"
      @close="closeSpriteInspectorModal"
      @edit-sprite="editSprite"
    />
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
