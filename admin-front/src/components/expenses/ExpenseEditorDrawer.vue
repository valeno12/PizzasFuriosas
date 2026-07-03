<script setup>
import { computed, ref, watch } from 'vue'
import AppDrawer from '../ui/AppDrawer.vue'
import UiButton from '../ui/UiButton.vue'
import UiInput from '../ui/UiInput.vue'
import UiSelect from '../ui/UiSelect.vue'
import { EXPENSE_CATEGORIES } from '../../constants/expenses'
import { toDateInputValue } from '../../utils/formatters'

const props = defineProps({
  open: Boolean,
  expense: { type: Object, default: null },
  isSaving: Boolean,
  errorMessage: { type: String, default: '' },
})
const emit = defineEmits(['close', 'save'])

const categoryOptions = EXPENSE_CATEGORIES.map((value) => ({ value, label: value }))
const form = ref({ description: '', amount: '', category: 'Proveedores', date: '' })
const localError = ref('')
const isEditing = computed(() => Boolean(props.expense))

watch(
  () => props.open,
  (open) => {
    if (!open) return
    localError.value = ''
    form.value = props.expense
      ? {
          description: props.expense.description || '',
          amount: props.expense.amount || '',
          category: props.expense.category || 'Proveedores',
          date: toDateInputValue(props.expense.date),
        }
      : { description: '', amount: '', category: 'Proveedores', date: toDateInputValue(new Date()) }
  },
)

function submit() {
  localError.value = ''
  if (
    !form.value.description.trim() ||
    Number(form.value.amount) <= 0 ||
    !form.value.category ||
    !form.value.date
  ) {
    localError.value = 'Completá descripción, monto, categoría y fecha.'
    return
  }
  emit('save', {
    date: new Date(`${form.value.date}T00:00:00`).toISOString(),
    description: form.value.description.trim(),
    amount: Number(form.value.amount),
    category: form.value.category,
  })
}
</script>

<template>
  <AppDrawer
    :open="open"
    :title="isEditing ? 'Editar gasto' : 'Nuevo gasto'"
    description="Registro de compra o egreso operativo."
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
        v-model="form.description"
        label="Descripción"
        required
        maxlength="250"
        placeholder="Harina, proveedor, impuestos..."
      />
      <div class="grid gap-4 sm:grid-cols-2">
        <UiInput
          v-model="form.amount"
          label="Monto"
          required
          type="number"
          inputmode="numeric"
          min="0"
          step="0.01"
        />
        <UiSelect v-model="form.category" label="Categoría" required :options="categoryOptions" />
      </div>
      <UiInput v-model="form.date" label="Fecha" required type="date" />
    </form>
    <template #footer>
      <UiButton variant="secondary" :disabled="isSaving" @click="emit('close')">Cancelar</UiButton>
      <UiButton variant="primary" :loading="isSaving" @click="submit">{{
        isEditing ? 'Guardar cambios' : 'Registrar gasto'
      }}</UiButton>
    </template>
  </AppDrawer>
</template>
