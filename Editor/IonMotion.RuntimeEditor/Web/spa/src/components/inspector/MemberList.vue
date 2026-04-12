<script setup lang="ts">
import type { Member } from '../../composables/useRuntimeEditorState'
import MemberRow from './MemberRow.vue'

type MemberRowModel = Member & { depth: number }

const props = withDefaults(defineProps<{
  rows: MemberRowModel[]
  keyPrefix: string
  readonly?: boolean
  checkboxBooleans?: boolean
  useBounds?: boolean
}>(), {
  readonly: false,
  checkboxBooleans: true,
  useBounds: false
})

const emit = defineEmits<{
  edit: [member: MemberRowModel, value: unknown]
}>()
</script>

<template>
  <MemberRow
    v-for="member in rows"
    :key="`${keyPrefix}-${member.path}`"
    :member="member"
    :readonly="readonly"
    :checkbox-booleans="checkboxBooleans"
    :use-bounds="useBounds"
    @edit="(value) => emit('edit', member, value)"
  />
</template>
