<script setup>
import { computed, ref, watch } from 'vue'
import AppDrawer from '../ui/AppDrawer.vue'
import UiButton from '../ui/UiButton.vue'
import UiInput from '../ui/UiInput.vue'
import UiTextarea from '../ui/UiTextarea.vue'

const props = defineProps({
  open: Boolean,
  client: { type: Object, default: null },
  isSaving: Boolean,
  errorMessage: { type: String, default: '' },
})
const emit = defineEmits(['close', 'save'])

const form = ref({ name: '', phone: '', notes: '' })
const localError = ref('')
const isEditing = computed(() => Boolean(props.client))

watch(
  () => props.open,
  (open) => {
    if (!open) return
    localError.value = ''
    form.value = props.client
      ? {
          name: props.client.name || '',
          phone: props.client.phone || '',
          notes: props.client.notes || '',
        }
      : { name: '', phone: '', notes: '' }
  },
)

function submit() {
  localError.value = ''
  if (!form.value.name.trim()) {
    localError.value = 'El nombre es obligatorio.'
    return
  }
  emit('save', {
    name: form.value.name.trim(),
    phone: form.value.phone.trim() || null,
    notes: form.value.notes.trim() || null,
  })
}
</script>

<template>
  <AppDrawer
    :open="open"
    :title="isEditing ? 'Editar cliente' : 'Nuevo cliente'"
    description="Datos de contacto y notas operativas."
    @close="emit('close')"
  >
    <form class="grid gap-4" @submit.prevent="submit">
      <div
        v-if="localError || errorMessage"
        class="rounded-2xl bg-danger-soft p-3 text-sm font-semibold text-danger"
      >
        {{ localError || errorMessage }}
      </div>
      <UiInput
        v-model="form.name"
        label="Nombre"
        required
        maxlength="100"
        placeholder="Juan Pérez"
      />
      <UiInput
        v-model="form.phone"
        label="Teléfono"
        maxlength="20"
        placeholder="11 5555-5555"
        type="tel"
        inputmode="numeric"
      />
      <UiTextarea
        v-model="form.notes"
        label="Notas"
        maxlength="500"
        placeholder="Preferencias, referencias o aclaraciones"
      />
    </form>
    <template #footer>
      <UiButton variant="secondary" :disabled="isSaving" @click="emit('close')">Cancelar</UiButton>
      <UiButton variant="primary" :loading="isSaving" @click="submit">{{
        isEditing ? 'Guardar cambios' : 'Crear cliente'
      }}</UiButton>
    </template>
  </AppDrawer>
</template>
