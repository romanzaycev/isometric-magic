<script setup lang="ts">
import { computed } from 'vue'
import type { Member } from '../../composables/useRuntimeEditorState'

type MemberRow = Member & { depth: number }

const props = withDefaults(defineProps<{
  member: MemberRow
  readonly?: boolean
  checkboxBooleans?: boolean
  useBounds?: boolean
}>(), {
  readonly: false,
  checkboxBooleans: true,
  useBounds: false
})

const emit = defineEmits<{
  edit: [value: unknown]
}>()

function isVectorMember(member: Member): boolean {
  return member.type === 'Vector2' || member.type === 'Vector3' || member.type === 'Vector4'
}

function isNumericMember(member: Member): boolean {
  return member.type === 'Int32' || member.type === 'UInt32' || member.type === 'Int64' || member.type === 'Single' || member.type === 'Double'
}

function isEnumMember(member: Member): boolean {
  return !!member.enumValues && member.enumValues.length > 0
}

function toRecord(value: unknown): Record<string, unknown> {
  if (value !== null && typeof value === 'object') {
    return value as Record<string, unknown>
  }

  return {}
}

const vectorValue = computed(() => toRecord(props.member.value))

function updateBoolean(event: Event): void {
  emit('edit', (event.target as HTMLInputElement).checked)
}

function updateEnum(event: Event): void {
  emit('edit', (event.target as HTMLSelectElement).value)
}

function updateNumeric(event: Event): void {
  const raw = (event.target as HTMLInputElement).value
  if (props.member.type === 'Int64') {
    emit('edit', raw)
    return
  }

  emit('edit', Number(raw))
}

function updateText(event: Event): void {
  emit('edit', (event.target as HTMLInputElement).value)
}

function updateVectorField(axis: 'x' | 'y' | 'z' | 'w', event: Event): void {
  const next = {
    ...vectorValue.value,
    [axis]: Number((event.target as HTMLInputElement).value)
  }

  emit('edit', next)
}
</script>

<template>
  <div class="row" :style="{ marginLeft: `${member.depth * 14}px` }">
    <div>{{ member.path }} <span class="muted">: {{ member.type }}</span></div>

    <div v-if="readonly || !member.editable" class="readonly">{{ String(member.value ?? 'null') }}</div>

    <input
      v-else-if="checkboxBooleans && member.type === 'Boolean'"
      type="checkbox"
      :checked="Boolean(member.value)"
      @change="updateBoolean"
    />

    <select
      v-else-if="isEnumMember(member)"
      :value="String(member.value ?? '')"
      @change="updateEnum"
    >
      <option v-for="enumValue in member.enumValues" :key="enumValue" :value="enumValue">{{ enumValue }}</option>
    </select>

    <input
      v-else-if="isNumericMember(member)"
      :type="member.type === 'Int64' ? 'text' : 'number'"
      :value="String(member.value ?? '')"
      :step="member.step ?? 0.1"
      :min="useBounds ? member.min ?? undefined : undefined"
      :max="useBounds ? member.max ?? undefined : undefined"
      @change="updateNumeric"
    />

    <div v-else-if="isVectorMember(member)" class="vec">
      <input
        type="number"
        step="0.1"
        :value="Number(vectorValue.x ?? 0)"
        @change="updateVectorField('x', $event)"
      />
      <input
        type="number"
        step="0.1"
        :value="Number(vectorValue.y ?? 0)"
        @change="updateVectorField('y', $event)"
      />
      <input
        v-if="member.type !== 'Vector2'"
        type="number"
        step="0.1"
        :value="Number(vectorValue.z ?? 0)"
        @change="updateVectorField('z', $event)"
      />
      <input
        v-if="member.type === 'Vector4'"
        type="number"
        step="0.1"
        :value="Number(vectorValue.w ?? 0)"
        @change="updateVectorField('w', $event)"
      />
    </div>

    <input
      v-else
      type="text"
      :value="String(member.value ?? '')"
      @change="updateText"
    />
  </div>
</template>
