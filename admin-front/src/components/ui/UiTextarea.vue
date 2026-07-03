<script setup>
import { useId } from 'vue'
import FormField from './FormField.vue'

defineProps({
  modelValue: { type: String, default: '' },
  label: { type: String, required: true },
  placeholder: { type: String, default: '' },
  required: { type: Boolean, default: false },
  helper: { type: String, default: '' },
  error: { type: String, default: '' },
  rows: { type: [String, Number], default: 4 },
  maxlength: { type: [String, Number], default: null },
  disabled: { type: Boolean, default: false },
})

const emit = defineEmits(['update:modelValue'])
const id = useId()
</script>

<template>
  <FormField :label="label" :for-id="id" :required="required" :helper="helper" :error="error">
    <textarea
      :id="id"
      class="form-control min-h-28 resize-y"
      :class="{ invalid: error }"
      :value="modelValue"
      :placeholder="placeholder"
      :required="required"
      :rows="rows"
      :maxlength="maxlength"
      :disabled="disabled"
      @input="emit('update:modelValue', $event.target.value)"
    ></textarea>
  </FormField>
</template>
