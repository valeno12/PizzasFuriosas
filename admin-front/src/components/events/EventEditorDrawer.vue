<script setup>
import { ref, watch } from 'vue'
import AppDrawer from '../ui/AppDrawer.vue'
import UiButton from '../ui/UiButton.vue'
import UiInput from '../ui/UiInput.vue'
import UiTextarea from '../ui/UiTextarea.vue'
import { toDateTimeLocalValue } from '../../utils/formatters'

const props = defineProps({
  open: Boolean,
  event: { type: Object, default: null },
  isSaving: Boolean,
  errorMessage: { type: String, default: '' },
})
const emit = defineEmits(['close', 'save'])

const form = ref({
  eventDate: '',
  location: '',
  notes: '',
  pizzaCount: 0,
  pricePerPizza: 0,
  deposit: 0,
})
const localError = ref('')

watch(
  () => props.open,
  (open) => {
    if (!open || !props.event) return
    localError.value = ''
    form.value = {
      eventDate: toDateTimeLocalValue(props.event.eventDate),
      location: props.event.location || '',
      notes: props.event.notes || '',
      pizzaCount: props.event.pizzaCount ?? 0,
      pricePerPizza: props.event.pricePerPizza ?? 0,
      deposit: props.event.deposit ?? 0,
    }
  },
)

function submit() {
  localError.value = ''
  if (!form.value.eventDate || !form.value.location.trim()) {
    localError.value = 'Fecha y ubicación son obligatorias.'
    return
  }
  emit('save', {
    eventDate: new Date(form.value.eventDate).toISOString(),
    location: form.value.location.trim(),
    notes: form.value.notes.trim() || null,
    pizzaCount: Number(form.value.pizzaCount || 0),
    pricePerPizza: Number(form.value.pricePerPizza || 0),
    deposit: Number(form.value.deposit || 0),
  })
}
</script>

<template>
  <AppDrawer
    :open="open"
    title="Editar evento"
    :description="event?.customerName"
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
        <UiInput v-model="form.eventDate" label="Fecha y hora" type="datetime-local" required />
        <UiInput v-model="form.location" label="Ubicación" required maxlength="200" />
      </div>
      <div class="grid gap-4 sm:grid-cols-2">
        <UiInput
          v-model="form.pizzaCount"
          label="Pizzas"
          type="number"
          inputmode="numeric"
          min="0"
        />
        <UiInput
          v-model="form.pricePerPizza"
          label="Precio por pizza"
          type="number"
          inputmode="numeric"
          min="0"
          step="0.01"
        />
      </div>
      <UiInput
        v-model="form.deposit"
        label="Seña"
        type="number"
        inputmode="numeric"
        min="0"
        step="0.01"
      />
      <UiTextarea v-model="form.notes" label="Notas" maxlength="500" />
    </form>
    <template #footer>
      <UiButton variant="secondary" :disabled="isSaving" @click="emit('close')">Cancelar</UiButton>
      <UiButton variant="primary" :loading="isSaving" @click="submit">Guardar cambios</UiButton>
    </template>
  </AppDrawer>
</template>
