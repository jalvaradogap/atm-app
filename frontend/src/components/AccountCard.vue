<script setup lang="ts">
import type { AccountDto } from '../types/api'
import { formatMoney } from '../utils/format'

defineProps<{
  account: AccountDto
  selected: boolean
}>()

defineEmits<{
  select: [id: string]
}>()
</script>

<template>
  <button
    type="button"
    class="card w-full border-2 text-left transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
    :class="selected ? 'border-accent' : 'border-transparent hover:ring-slate-300'"
    :aria-pressed="selected"
    @click="$emit('select', account.id)"
  >
    <span class="flex items-center justify-between gap-2">
      <span class="font-semibold">{{ account.name }}</span>
      <span
        v-if="selected"
        class="rounded bg-accent px-2 py-0.5 text-xs font-semibold text-white"
      >Selected</span>
    </span>
    <span class="mt-1 block text-sm text-muted">{{ account.number }}</span>
    <span class="mt-3 block text-2xl font-semibold tabular-nums">{{ formatMoney(account.balance) }}</span>
  </button>
</template>
