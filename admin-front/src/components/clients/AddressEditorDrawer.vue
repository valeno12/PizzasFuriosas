<script setup>
import { ref, watch } from 'vue'
import AppDrawer from '../ui/AppDrawer.vue'
import UiButton from '../ui/UiButton.vue'
import UiInput from '../ui/UiInput.vue'
import UiTextarea from '../ui/UiTextarea.vue'

const props = defineProps({
  open: Boolean,
  customerName: { type: String, default: '' },
  isSaving: Boolean,
  errorMessage: { type: String, default: '' },
})
const emit = defineEmits(['close', 'save'])

const form = ref({ street: '', number: '', apartment: '', notes: '' })
const localError = ref('')

watch(
  () => props.open,
  (open) => {
    if (!open) return
    localError.value = ''
    form.value = { street: '', number: '', apartment: '', notes: '' }
  },
)

function submit() {
  localError.value = ''
  if (!form.value.street.trim() || !form.value.number.trim()) {
    localError.value = 'Calle y número son obligatorios.'
    return
  }
  emit('save', {
    street: form.value.street.trim(),
    number: form.value.number.trim(),
    apartment: form.value.apartment.trim() || null,
    notes: form.value.notes.trim() || null,
  })
}
</script>

<template>
  <AppDrawer
    :open="open"
    title="Agregar dirección"
    :description="customerName ? `Dirección para ${customerName}` : 'Nueva dirección del cliente'"
    @close="emit('close')"
  >
    <form class="grid gap-4" @submit.prevent="submit">
      <div
        v-if="localError || errorMessage"
        class="rounded-2xl bg-danger-soft p-3 text-sm font-semibold text-danger"
      >
        {{ localError || errorMessage }}
      </div>
      <div class="grid gap-4 sm:grid-cols-2">
        <UiInput
          v-model="form.street"
          label="Calle"
          required
          maxlength="150"
          placeholder="Av. Siempreviva"
        />
        <UiInput v-model="form.number" label="Número" required maxlength="10" placeholder="742" />
      </div>
      <UiInput
        v-model="form.apartment"
        label="Depto / piso"
        maxlength="20"
        placeholder="Opcional"
      />
      <UiTextarea
        v-model="form.notes"
        label="Notas"
        maxlength="200"
        placeholder="Entrecalles, timbre o referencia"
      />
    </form>
    <template #footer>
      <UiButton variant="secondary" :disabled="isSaving" @click="emit('close')">Cancelar</UiButton>
      <UiButton variant="primary" :loading="isSaving" @click="submit">Guardar dirección</UiButton>
    </template>
  </AppDrawer>
</template>
