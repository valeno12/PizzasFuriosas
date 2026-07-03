<script setup>
import UiButton from './UiButton.vue'

defineProps({
  page: { type: Number, required: true },
  totalPages: { type: Number, required: true },
  isLoading: { type: Boolean, default: false },
})
const emit = defineEmits(['prev', 'next'])
</script>

<template>
  <div
    v-if="totalPages > 1"
    class="flex items-center justify-between gap-3 rounded-xl border border-line bg-surface px-4 py-3"
  >
    <span class="text-sm text-muted">Página {{ page }} de {{ totalPages }}</span>
    <div class="flex gap-2">
      <UiButton
        variant="secondary"
        size="sm"
        :disabled="page <= 1 || isLoading"
        @click="emit('prev')"
        >Anterior</UiButton
      >
      <UiButton
        variant="secondary"
        size="sm"
        :disabled="page >= totalPages || isLoading"
        @click="emit('next')"
        >Siguiente</UiButton
      >
    </div>
  </div>
</template>
