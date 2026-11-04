<script setup lang="ts">
defineProps<{
  message: string | null
  tone: 'success' | 'error'
}>()
</script>

<template>
  <!-- Always rendered, so screen readers announce a message as soon as it appears. -->
  <div aria-live="polite" role="status" class="mt-4 min-h-6 text-sm font-medium">
    <p v-if="message" :class="tone === 'success' ? 'text-success' : 'text-danger'">{{ message }}</p>
  </div>
</template>
