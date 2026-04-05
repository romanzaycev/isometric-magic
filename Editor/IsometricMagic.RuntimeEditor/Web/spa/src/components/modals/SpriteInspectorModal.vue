<script setup lang="ts">
import type { Member } from '../../composables/useRuntimeEditorState'
import MemberList from '../inspector/MemberList.vue'
import ModalShell from './ModalShell.vue'

type MemberRow = Member & { depth: number }

defineProps<{
  open: boolean
  selectedSpriteId: number | null
  spriteRows: MemberRow[]
}>()

const emit = defineEmits<{
  close: []
  editSprite: [member: MemberRow, value: unknown]
}>()
</script>

<template>
  <ModalShell :open="open && selectedSpriteId !== null" title="🎨 Sprite Inspector" @close="emit('close')">
    <MemberList
      :rows="spriteRows"
      key-prefix="sprite"
      @edit="(member, value) => emit('editSprite', member, value)"
    />
  </ModalShell>
</template>
