<script setup lang="ts">
import type { Member } from '../composables/useRuntimeEditorState'
import InspectorGroup from './inspector/InspectorGroup.vue'
import MemberList from './inspector/MemberList.vue'

type MemberRow = Member & { depth: number }
type InspectorView = { editableRows: MemberRow[]; readonlyRows: MemberRow[] }
type ComponentView = InspectorView & { index: number; type: string }

const inspectorFieldFilter = defineModel<string>('inspectorFieldFilter', { required: true })

defineProps<{
  selectedEntityId: number | null
  shouldAutoOpenInspectorGroups: boolean
  entityView: InspectorView
  transformView: InspectorView
  componentViews: ComponentView[]
}>()

const emit = defineEmits<{
  editEntity: [member: MemberRow, value: unknown]
  editTransform: [member: MemberRow, value: unknown]
  editComponent: [componentIndex: number, member: MemberRow, value: unknown]
}>()
</script>

<template>
  <article class="card">
    <h3>🔎 Entity Inspector</h3>
    <input
      v-model="inspectorFieldFilter"
      class="search inspector-filter"
      type="text"
      placeholder="Filter by field name"
    />

    <InspectorGroup title="👤 Entity" :open="shouldAutoOpenInspectorGroups">
      <MemberList
        :rows="entityView.editableRows"
        key-prefix="entity"
        :use-bounds="true"
        @edit="(member, value) => emit('editEntity', member, value)"
      />

      <details v-if="entityView.readonlyRows.length > 0" class="readonly-group" :open="shouldAutoOpenInspectorGroups">
        <summary>📦 Read-only public members ({{ entityView.readonlyRows.length }})</summary>
        <MemberList
          :rows="entityView.readonlyRows"
          key-prefix="entity-ro"
          :readonly="true"
        />
      </details>
    </InspectorGroup>

    <InspectorGroup title="🧭 Transform" :open="shouldAutoOpenInspectorGroups">
      <MemberList
        :rows="transformView.editableRows"
        key-prefix="transform"
        :checkbox-booleans="false"
        @edit="(member, value) => emit('editTransform', member, value)"
      />

      <details v-if="transformView.readonlyRows.length > 0" class="readonly-group" :open="shouldAutoOpenInspectorGroups">
        <summary>📦 Read-only public members ({{ transformView.readonlyRows.length }})</summary>
        <MemberList
          :rows="transformView.readonlyRows"
          key-prefix="transform-ro"
          :readonly="true"
        />
      </details>
    </InspectorGroup>

    <InspectorGroup
      v-for="component in componentViews"
      :key="`component-${component.index}`"
      :title="`🧩 ${component.type}`"
      :open="shouldAutoOpenInspectorGroups"
    >
      <MemberList
        :rows="component.editableRows"
        :key-prefix="`component-${component.index}`"
        @edit="(member, value) => emit('editComponent', component.index, member, value)"
      />

      <details v-if="component.readonlyRows.length > 0" class="readonly-group" :open="shouldAutoOpenInspectorGroups">
        <summary>📦 Read-only public members ({{ component.readonlyRows.length }})</summary>
        <MemberList
          :rows="component.readonlyRows"
          :key-prefix="`component-ro-${component.index}`"
          :readonly="true"
        />
      </details>
    </InspectorGroup>
  </article>
</template>
