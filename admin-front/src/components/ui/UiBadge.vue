<script setup>
defineProps({
  tone: { type: String, default: 'neutral' }, // neutral | brand | success | warning | danger | info
  icon: { type: String, default: '' },
})

const tones = {
  neutral: 'bg-surface-2 text-muted',
  brand: 'bg-primary-soft text-primary',
  success: 'bg-success-soft text-success',
  warning: 'bg-warning-soft text-warning',
  danger: 'bg-danger-soft text-danger',
  info: 'bg-info-soft text-info',
}
</script>

<template>
  <span
    :class="[
      'inline-flex min-h-[26px] items-center gap-1 whitespace-nowrap rounded-full px-2.5 py-1 text-[0.76rem] font-bold leading-none',
      tones[tone] || tones.neutral,
    ]"
  >
    <span v-if="icon" class="material-symbols-outlined !text-base" aria-hidden="true">{{
      icon
    }}</span>
    <slot />
  </span>
</template>
