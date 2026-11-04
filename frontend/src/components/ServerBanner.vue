<script setup lang="ts">
defineProps<{
  visible: boolean
}>()

defineEmits<{
  retry: []
}>()
</script>

<template>
  <div
    v-if="visible"
    role="alert"
    class="flex flex-wrap items-center justify-between gap-3 rounded-lg bg-danger px-4 py-3 text-white shadow-sm"
  >
    <p class="font-medium">Can't reach the bank right now.</p>
    <button
      type="button"
      class="rounded-md bg-white px-3 py-1 font-semibold text-danger focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
      @click="$emit('retry')"
    >
      Retry
    </button>
  </div>
</template>
