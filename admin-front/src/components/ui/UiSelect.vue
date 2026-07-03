<script setup>
import { useId } from 'vue'
import FormField from './FormField.vue'

defineProps({
  modelValue: { type: [String, Number], default: '' },
  label: { type: String, required: true },
  options: { type: Array, required: true }, // [{ value, label }]
  required: { type: Boolean, default: false },
  helper: { type: String, default: '' },
  error: { type: String, default: '' },
  placeholder: { type: String, default: 'Seleccionar' },
  disabled: { type: Boolean, default: false },
})

const emit = defineEmits(['update:modelValue'])
const id = useId()
</script>

<template>
  <FormField :label="label" :for-id="id" :required="required" :helper="helper" :error="error">
    <select
      :id="id"
      class="form-control"
      :class="{ invalid: error }"
      :value="modelValue"
      :required="required"
      :disabled="disabled"
      @change="emit('update:modelValue', $event.target.value)"
    >
      <option v-if="placeholder" disabled value="">{{ placeholder }}</option>
      <option v-for="option in options" :key="option.value" :value="option.value">
        {{ option.label }}
      </option>
    </select>
  </FormField>
</template>
