<script setup>
defineProps({
  label: { type: String, required: true },
  value: { type: [String, Number], required: true },
  icon: { type: String, default: '' },
  tone: { type: String, default: 'default' }, // default | accent | danger
})

const tones = {
  default: 'text-foreground',
  accent: 'text-primary',
  danger: 'text-danger',
}
</script>

<template>
  <div class="grid gap-0.5 rounded-xl border border-line bg-surface p-3">
    <div
      class="flex items-center gap-1 text-[0.68rem] font-bold uppercase tracking-[0.06em] text-muted"
    >
      <span v-if="icon" class="material-symbols-outlined !text-[13px]" aria-hidden="true">{{
        icon
      }}</span>
      {{ label }}
    </div>
    <div :class="['mt-1 text-[1.2rem] font-extrabold leading-none', tones[tone] || tones.default]">
      {{ value }}
    </div>
  </div>
</template>
