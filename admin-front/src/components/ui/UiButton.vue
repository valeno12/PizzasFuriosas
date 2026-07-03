<script setup>
defineProps({
  variant: { type: String, default: 'secondary' }, // primary | secondary | ghost | danger
  size: { type: String, default: 'md' }, // sm | md | lg
  type: { type: String, default: 'button' },
  loading: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
})

const variants = {
  primary: 'bg-primary text-primary-foreground hover:opacity-90',
  secondary: 'border border-line bg-surface-2 text-foreground hover:border-line-strong',
  ghost: 'bg-transparent text-muted hover:text-foreground',
  danger: 'bg-danger text-danger-foreground hover:opacity-90',
}

const sizes = {
  sm: 'min-h-[34px] px-3 text-[0.8rem]',
  md: 'min-h-[40px] px-3.5 text-[0.875rem]',
  lg: 'min-h-[52px] px-4 rounded-[14px] text-base',
}
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    :class="[
      'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-[10px] font-bold leading-none transition disabled:opacity-50',
      variants[variant],
      sizes[size],
    ]"
  >
    <span
      v-if="loading"
      class="material-symbols-outlined animate-spin !text-[18px]"
      aria-hidden="true"
      >progress_activity</span
    >
    <slot />
  </button>
</template>
