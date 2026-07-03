<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import FormShell from '../components/shared/FormShell.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiTextarea from '../components/ui/UiTextarea.vue'
import UiSelect from '../components/ui/UiSelect.vue'
import UiButton from '../components/ui/UiButton.vue'
import SearchInput from '../components/ui/SearchInput.vue'
import EntityAvatar from '../components/ui/EntityAvatar.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import { getCustomers } from '../services/customersService'
import { useEventsStore } from '../stores/events'
import { useToastsStore } from '../stores/toasts'
import { useDebounce } from '../composables/useDebounce'
import { EVENT_PAYMENT_METHODS } from '../constants/events'
import { formatMoney } from '../utils/formatters'

const router = useRouter()
const store = useEventsStore()
const toasts = useToastsStore()
const { debounce } = useDebounce()

// --- Cliente (búsqueda server-side) ---
const customers = ref([])
const isLoadingCustomers = ref(false)
const customerSearch = ref('')
const selectedCustomer = ref(null)

async function fetchCustomers() {
  isLoadingCustomers.value = true
  try {
    const data = await getCustomers({ pageSize: 20, search: customerSearch.value.trim() })
    customers.value = data.items
  } catch {
    localError.value = 'No se pudieron cargar los clientes.'
  } finally {
    isLoadingCustomers.value = false
  }
}

function handleCustomerSearch(value) {
  customerSearch.value = value
  debounce(fetchCustomers, 250)
}

function selectCustomer(customer) {
  selectedCustomer.value = customer
}

function clearCustomer() {
  selectedCustomer.value = null
  customerSearch.value = ''
  fetchCustomers()
}

onMounted(fetchCustomers)

// --- Formulario ---
const localError = ref('')
const form = ref({
  eventDate: '',
  location: '',
  notes: '',
  pizzaCount: 0,
  pricePerPizza: 0,
  deposit: 0,
})
const surcharges = ref([])
const payments = ref([])
const paymentMethods = EVENT_PAYMENT_METHODS.map((value) => ({ value, label: value }))

const totalSurcharges = computed(() =>
  surcharges.value.reduce((sum, i) => sum + Number(i.amount || 0), 0),
)
const totalPayments = computed(() =>
  payments.value.reduce((sum, i) => sum + Number(i.amount || 0), 0),
)
const estimatedTotal = computed(
  () =>
    Number(form.value.pizzaCount || 0) * Number(form.value.pricePerPizza || 0) +
    totalSurcharges.value,
)
const outstanding = computed(
  () => estimatedTotal.value - Number(form.value.deposit || 0) - totalPayments.value,
)
const canSubmit = computed(() =>
  Boolean(selectedCustomer.value && form.value.eventDate && form.value.location.trim()),
)

// Evita perder un evento a medio cargar por un toque de más en "Cancelar" o "Atrás".
const confirmExit = ref(false)

const hasProgress = computed(() =>
  Boolean(selectedCustomer.value || form.value.eventDate || form.value.location.trim()),
)

function requestExit() {
  if (hasProgress.value) confirmExit.value = true
  else router.push('/admin/eventos')
}

function addSurcharge() {
  surcharges.value.push({ description: '', amount: '' })
}
function removeSurcharge(index) {
  surcharges.value.splice(index, 1)
}
function addPayment() {
  payments.value.push({ paymentMethod: 'Efectivo', amount: '' })
}
function removePayment(index) {
  payments.value.splice(index, 1)
}

async function submit() {
  localError.value = ''
  if (!canSubmit.value) {
    localError.value = 'Cliente, fecha y ubicación son obligatorios.'
    return
  }
  if (surcharges.value.some((i) => !i.description.trim() || Number(i.amount) <= 0)) {
    localError.value = 'Completá descripción y monto de cada viático.'
    return
  }
  if (payments.value.some((i) => !i.paymentMethod || Number(i.amount) <= 0)) {
    localError.value = 'Completá método y monto de cada pago.'
    return
  }
  try {
    await store.addEvent({
      customerId: selectedCustomer.value.id,
      eventDate: new Date(form.value.eventDate).toISOString(),
      location: form.value.location.trim(),
      notes: form.value.notes.trim() || null,
      pizzaCount: Number(form.value.pizzaCount || 0),
      pricePerPizza: Number(form.value.pricePerPizza || 0),
      deposit: Number(form.value.deposit || 0),
      surcharges: surcharges.value.map((i) => ({
        description: i.description.trim(),
        amount: Number(i.amount),
      })),
      payments: payments.value.map((i) => ({
        paymentMethod: i.paymentMethod,
        amount: Number(i.amount),
      })),
    })
    toasts.success('Evento creado.')
    router.push('/admin/eventos')
  } catch {
    toasts.error(store.operationError || 'No se pudo crear el evento.')
  }
}
</script>

<template>
  <FormShell
    title="Nuevo evento"
    subtitle="Reserva con seña, viáticos y pagos"
    :can-submit="canSubmit"
    primary-label="Crear evento"
    :is-loading="store.isSaving"
    :total-amount="formatMoney(estimatedTotal)"
    @back="requestExit"
    @cancel="requestExit"
    @primary="submit"
  >
    <div class="grid gap-4 px-4 md:px-6 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start lg:gap-6">
      <!-- Formulario -->
      <form class="grid gap-3.5" @submit.prevent="submit">
        <div
          v-if="localError || store.operationError"
          class="rounded-xl border border-danger/30 bg-danger-soft px-3 py-2.5 text-sm font-bold text-danger"
        >
          {{ localError || store.operationError }}
        </div>

        <!-- Cliente -->
        <div class="grid gap-3 rounded-2xl border border-line-soft bg-surface p-4">
          <div class="text-[0.72rem] font-extrabold uppercase tracking-[0.07em] text-muted">
            Cliente *
          </div>

          <div
            v-if="selectedCustomer"
            class="flex items-center gap-3 rounded-xl border-[1.5px] border-primary/30 bg-primary-soft px-3 py-2.5"
          >
            <EntityAvatar :name="selectedCustomer.name" />
            <div class="min-w-0">
              <div class="text-[0.95rem] font-bold">{{ selectedCustomer.name }}</div>
              <div class="text-xs text-muted">{{ selectedCustomer.phone || 'Sin teléfono' }}</div>
            </div>
            <button
              type="button"
              class="ml-auto grid h-8 w-8 shrink-0 place-items-center rounded-lg text-muted transition-colors hover:bg-surface-2 hover:text-foreground"
              aria-label="Cambiar cliente"
              @click="clearCustomer"
            >
              <span class="material-symbols-outlined !text-lg" aria-hidden="true">close</span>
            </button>
          </div>

          <template v-else>
            <SearchInput
              :model-value="customerSearch"
              placeholder="Buscar cliente por nombre o teléfono"
              @update:model-value="handleCustomerSearch"
            />

            <div v-if="isLoadingCustomers" class="grid gap-2">
              <SkeletonBlock v-for="i in 4" :key="i" height="52px" />
            </div>
            <div v-else-if="customers.length === 0" class="py-4 text-center text-sm text-muted">
              No hay clientes que coincidan.
            </div>
            <div v-else class="list-panel max-h-[260px] overflow-y-auto">
              <button
                v-for="client in customers"
                :key="client.id"
                type="button"
                class="list-row transition-colors hover:bg-surface-2/50"
                @click="selectCustomer(client)"
              >
                <div class="flex min-w-0 items-center gap-2.5">
                  <EntityAvatar :name="client.name" size="sm" />
                  <div class="min-w-0">
                    <div class="list-row-title">{{ client.name }}</div>
                    <div class="list-row-meta">{{ client.phone || 'Sin teléfono' }}</div>
                  </div>
                </div>
              </button>
            </div>
          </template>
        </div>

        <!-- Datos del evento -->
        <div class="grid gap-3 rounded-2xl border border-line-soft bg-surface p-4">
          <div class="text-[0.72rem] font-extrabold uppercase tracking-[0.07em] text-muted">
            Datos del evento
          </div>
          <div class="grid gap-4 sm:grid-cols-2">
            <UiInput
              v-model="form.eventDate"
              label="Fecha y hora *"
              type="datetime-local"
              required
            />
            <UiInput
              v-model="form.location"
              label="Ubicación *"
              required
              maxlength="200"
              placeholder="Dirección o lugar"
            />
          </div>
          <div class="grid gap-4 sm:grid-cols-2">
            <UiInput
              v-model="form.pizzaCount"
              label="Pizzas estimadas"
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
            label="Seña inicial"
            type="number"
            inputmode="numeric"
            min="0"
            step="0.01"
          />
          <UiTextarea
            v-model="form.notes"
            label="Notas"
            placeholder="Aclaraciones logísticas, preferencias o referencias"
          />
        </div>

        <!-- Viáticos -->
        <div class="grid gap-3 rounded-2xl border border-line-soft bg-surface p-4">
          <div class="flex items-end justify-between gap-3">
            <div>
              <div class="text-[0.72rem] font-extrabold uppercase tracking-[0.07em] text-muted">
                Viáticos
              </div>
              <p class="mt-1.5 text-xs text-muted">Costos adicionales del evento.</p>
            </div>
            <UiButton variant="secondary" size="sm" @click="addSurcharge">Agregar</UiButton>
          </div>
          <p v-if="surcharges.length === 0" class="text-sm text-muted">Sin viáticos adicionales.</p>
          <div class="grid gap-3">
            <div
              v-for="(item, idx) in surcharges"
              :key="idx"
              class="grid grid-cols-[1fr_auto] gap-2.5 rounded-xl border border-line-soft bg-surface-2 p-3 sm:grid-cols-[1fr_140px_auto]"
            >
              <UiInput
                v-model="item.description"
                label="Descripción"
                placeholder="Traslado"
                class="max-sm:col-span-2"
              />
              <UiInput
                v-model="item.amount"
                label="Monto"
                type="number"
                inputmode="numeric"
                min="0"
                step="0.01"
              />
              <UiButton variant="ghost" class="self-end text-danger" @click="removeSurcharge(idx)"
                >Quitar</UiButton
              >
            </div>
          </div>
        </div>

        <!-- Pagos iniciales -->
        <div class="grid gap-3 rounded-2xl border border-line-soft bg-surface p-4">
          <div class="flex items-end justify-between gap-3">
            <div>
              <div class="text-[0.72rem] font-extrabold uppercase tracking-[0.07em] text-muted">
                Pagos iniciales
              </div>
              <p class="mt-1.5 text-xs text-muted">Opcional. Se pueden cargar luego.</p>
            </div>
            <UiButton variant="secondary" size="sm" @click="addPayment">Agregar</UiButton>
          </div>
          <p v-if="payments.length === 0" class="text-sm text-muted">Sin pagos iniciales.</p>
          <div class="grid gap-3">
            <div
              v-for="(item, idx) in payments"
              :key="idx"
              class="grid grid-cols-[1fr_auto] gap-2.5 rounded-xl border border-line-soft bg-surface-2 p-3 sm:grid-cols-[1fr_140px_auto]"
            >
              <UiSelect
                v-model="item.paymentMethod"
                label="Método"
                :options="paymentMethods"
                class="max-sm:col-span-2"
              />
              <UiInput
                v-model="item.amount"
                label="Monto"
                type="number"
                inputmode="numeric"
                min="0"
                step="0.01"
              />
              <UiButton variant="ghost" class="self-end text-danger" @click="removePayment(idx)"
                >Quitar</UiButton
              >
            </div>
          </div>
        </div>
      </form>

      <!-- Resumen desktop -->
      <aside class="hidden lg:sticky lg:top-24 lg:grid lg:gap-3">
        <div class="overflow-hidden rounded-2xl border border-line-soft bg-surface">
          <div
            class="border-b border-line-soft px-4 pb-2 pt-3 text-[0.72rem] font-extrabold uppercase tracking-[0.07em] text-muted"
          >
            Resumen
          </div>
          <div class="grid divide-y divide-line-soft">
            <div class="flex items-center justify-between gap-3 px-4 py-3">
              <span class="text-muted">Pizzas × precio</span>
              <strong>{{
                formatMoney(Number(form.pizzaCount || 0) * Number(form.pricePerPizza || 0))
              }}</strong>
            </div>
            <div class="flex items-center justify-between gap-3 px-4 py-3">
              <span class="text-muted">Viáticos</span>
              <strong>{{ formatMoney(totalSurcharges) }}</strong>
            </div>
            <div class="flex items-center justify-between gap-3 px-4 py-3">
              <span class="text-muted">Seña + pagos</span>
              <strong>{{ formatMoney(Number(form.deposit || 0) + totalPayments) }}</strong>
            </div>
          </div>
          <div class="grid gap-2.5 border-t border-line-soft px-4 py-3">
            <div class="flex items-center justify-between gap-3 text-[0.9rem] font-bold">
              <span>Total estimado</span>
              <strong class="text-primary">{{ formatMoney(estimatedTotal) }}</strong>
            </div>
            <div class="flex items-center justify-between gap-3 text-[0.9rem] font-bold">
              <span class="text-muted">Saldo pendiente</span>
              <strong :class="outstanding > 0 ? 'text-danger' : 'text-success'">{{
                formatMoney(outstanding)
              }}</strong>
            </div>
          </div>
        </div>
      </aside>
    </div>

    <ConfirmDialog
      :open="confirmExit"
      title="Descartar evento"
      message="Tenés un evento a medio cargar. Si salís ahora, se pierde lo cargado."
      confirm-label="Descartar"
      cancel-label="Seguir cargando"
      @close="confirmExit = false"
      @confirm="router.push('/admin/eventos')"
    />
  </FormShell>
</template>
