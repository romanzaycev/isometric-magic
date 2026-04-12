<script setup lang="ts">
import type { Member } from '../../composables/useRuntimeEditorState'
import MemberList from '../inspector/MemberList.vue'
import ModalShell from './ModalShell.vue'

type MemberRow = Member & { depth: number }

defineProps<{
  open: boolean
  ambientRows: MemberRow[]
  lights: { id: number; index: number; members: Member[] }[]
  flattenedMembers: (members: Member[]) => MemberRow[]
}>()

const emit = defineEmits<{
  close: []
  refreshLighting: []
  addLight: []
  removeLight: [lightId: number]
  editAmbient: [member: MemberRow, value: unknown]
  editLight: [lightId: number, member: MemberRow, value: unknown]
}>()
</script>

<template>
  <ModalShell :open="open" title="💡 Lighting" @close="emit('close')">
    <div class="actions">
      <button @click="emit('refreshLighting')">🔄 Refresh Lighting</button>
      <button @click="emit('addLight')">➕ Add Light</button>
    </div>

    <h4>Ambient</h4>
    <MemberList
      :rows="ambientRows"
      key-prefix="ambient"
      :checkbox-booleans="false"
      @edit="(member, value) => emit('editAmbient', member, value)"
    />

    <template v-for="light in lights" :key="light.id">
      <h4>Point Light #{{ light.index }} <button class="tiny" @click="emit('removeLight', light.id)">🗑️ Remove</button></h4>
      <MemberList
        :rows="flattenedMembers(light.members)"
        :key-prefix="`light-${light.id}`"
        :checkbox-booleans="false"
        @edit="(member, value) => emit('editLight', light.id, member, value)"
      />
    </template>
  </ModalShell>
</template>
