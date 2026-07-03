<script setup>
import { useId } from 'vue'
import FormField from './FormField.vue'

defineProps({
  modelValue: { type: [String, Number], default: '' },
  label: { type: String, required: true },
  type: { type: String, default: 'text' },
  placeholder: { type: String, default: '' },
  required: { type: Boolean, default: false },
  helper: { type: String, default: '' },
  error: { type: String, default: '' },
  min: { type: [String, Number], default: null },
  max: { type: [String, Number], default: null },
  step: { type: [String, Number], default: null },
  maxlength: { type: [String, Number], default: null },
  autocomplete: { type: String, default: null },
  inputmode: { type: String, default: null },
  disabled: { type: Boolean, default: false },
})

const emit = defineEmits(['update:modelValue'])
const id = useId()
</script>

<template>
  <FormField :label="label" :for-id="id" :required="required" :helper="helper" :error="error">
    <input
      :id="id"
      class="form-control"
      :class="{ invalid: error }"
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :required="required"
      :min="min"
      :max="max"
      :step="step"
      :maxlength="maxlength"
      :autocomplete="autocomplete"
      :inputmode="inputmode"
      :disabled="disabled"
      @input="emit('update:modelValue', $event.target.value)"
    />
  </FormField>
</template>
