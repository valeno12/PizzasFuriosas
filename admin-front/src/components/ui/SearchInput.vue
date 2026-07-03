<script setup>
defineProps({
  modelValue: { type: String, default: '' },
  placeholder: { type: String, default: 'Buscar...' },
  label: { type: String, default: 'Buscar' },
})
const emit = defineEmits(['update:modelValue'])
</script>

<template>
  <label class="relative block">
    <span class="sr-only">{{ label }}</span>
    <span
      class="material-symbols-outlined pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 !text-[18px] text-muted"
      aria-hidden="true"
      >search</span
    >
    <input
      class="form-control h-11 pl-10 text-[0.9rem]"
      type="search"
      :value="modelValue"
      :placeholder="placeholder"
      @input="emit('update:modelValue', $event.target.value)"
    />
  </label>
</template>
