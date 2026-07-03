<script setup>
defineProps({
  modelValue: { type: Boolean, default: false },
  label: { type: String, required: true },
  helper: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
})
const emit = defineEmits(['update:modelValue'])
</script>

<template>
  <label
    class="flex cursor-pointer items-center justify-between gap-4 rounded-xl border border-line bg-surface p-3"
  >
    <span>
      <span class="block text-sm font-bold text-foreground">{{ label }}</span>
      <span v-if="helper" class="mt-0.5 block text-sm text-muted">{{ helper }}</span>
    </span>
    <input
      class="sr-only"
      type="checkbox"
      :checked="modelValue"
      :disabled="disabled"
      @change="emit('update:modelValue', $event.target.checked)"
    />
    <span
      :class="[
        'relative h-7 w-12 shrink-0 rounded-full transition-colors',
        modelValue ? 'bg-primary' : 'bg-surface-2',
      ]"
      aria-hidden="true"
    >
      <span
        :class="[
          'absolute top-1 h-5 w-5 rounded-full bg-foreground transition-transform',
          modelValue ? 'translate-x-6 bg-primary-foreground' : 'translate-x-1',
        ]"
      ></span>
    </span>
  </label>
</template>
