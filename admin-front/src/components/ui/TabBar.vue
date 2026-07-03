<script setup>
defineProps({
  tabs: { type: Array, required: true }, // [{ value, label }]
  modelValue: { type: [String, Number], default: null },
})
const emit = defineEmits(['update:modelValue'])
</script>

<template>
  <div
    class="no-scrollbar inline-flex max-w-full gap-0.5 overflow-x-auto rounded-xl border border-line-soft bg-surface p-[3px]"
  >
    <button
      v-for="tab in tabs"
      :key="tab.value ?? 'all'"
      type="button"
      :class="[
        'h-9 whitespace-nowrap rounded-[9px] px-3.5 text-center text-sm font-bold transition-colors',
        modelValue === tab.value
          ? 'bg-primary text-primary-foreground'
          : 'text-muted hover:text-foreground',
      ]"
      @click="emit('update:modelValue', tab.value)"
    >
      {{ tab.label }}
    </button>
  </div>
</template>
