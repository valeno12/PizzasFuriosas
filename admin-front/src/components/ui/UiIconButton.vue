<script setup>
defineProps({
  icon: { type: String, required: true },
  label: { type: String, required: true },
  type: { type: String, default: 'button' },
  disabled: { type: Boolean, default: false },
})
</script>

<template>
  <button
    :type="type"
    :disabled="disabled"
    :aria-label="label"
    :title="label"
    class="inline-flex h-[42px] w-[42px] items-center justify-center rounded-[10px] border border-line bg-surface-2 text-muted transition hover:border-line-strong hover:text-foreground disabled:opacity-50"
  >
    <span class="material-symbols-outlined" aria-hidden="true">{{ icon }}</span>
  </button>
</template>
