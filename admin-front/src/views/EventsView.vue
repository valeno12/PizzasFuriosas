<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import PageHeader from '../components/shared/PageHeader.vue'
import UiButton from '../components/ui/UiButton.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiSelect from '../components/ui/UiSelect.vue'
import StatCard from '../components/ui/StatCard.vue'
import StatusBadge from '../components/ui/StatusBadge.vue'
import TabBar from '../components/ui/TabBar.vue'
import SearchInput from '../components/ui/SearchInput.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ErrorState from '../components/ui/ErrorState.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import AppDrawer from '../components/ui/AppDrawer.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import EventEditorDrawer from '../components/events/EventEditorDrawer.vue'
import { useEventsStore } from '../stores/events'
import { useToastsStore } from '../stores/toasts'
import { EVENT_PAYMENT_METHODS } from '../constants/events'
import { formatDateTime, formatMoney } from '../utils/formatters'

const router = useRouter()
const store = useEventsStore()
const toasts = useToastsStore()

const activeFilter = ref('active')
const searchTerm = ref('')
const selectedId = ref(null)
const detailOpen = ref(false)
const paymentOpen = ref(false)
const editOpen = ref(false)
const completeOpen = ref(false)
const cancelTarget = ref(null)
const paymentForm = ref({ paymentMethod: 'Efectivo', amount: '' })
const completeForm = ref({ extraPizzas: 0, description: '', amount: '' })
const localError = ref('')

const paymentOptions = EVENT_PAYMENT_METHODS.map((value) => ({ value, label: value }))

const FILTERS = [
  { value: 'active', label: 'Activos' },
  { value: 'balance', label: 'Con saldo' },
  { value: 'closed', label: 'Historial' },
  { value: 'all', label: 'Todos' },
]

onMounted(async () => {
  await store.fetchEvents()
  if (store.items[0]) selectEvent(store.items[0].id)
})

const selected = computed(() => store.selectedEvent)
const canAct = computed(
  () => selected.value && !['Completado', 'Cancelado'].includes(selected.value.status),
)

const filteredEvents = computed(() => {
  const term = searchTerm.value.trim().toLowerCase()
  return store.items.filter((event) => {
    const matches =
      !term ||
      event.customerName.toLowerCase().includes(term) ||
      event.location.toLowerCase().includes(term)
    if (!matches) return false
    if (activeFilter.value === 'active')
      return ['Próximo', 'Pendiente de cierre'].includes(event.status)
    if (activeFilter.value === 'balance')
      return event.outstandingBalance > 0 && event.status !== 'Cancelado'
    if (activeFilter.value === 'closed') return ['Completado', 'Cancelado'].includes(event.status)
    return true
  })
})

const stats = computed(() => ({
  active: store.items.filter((e) => ['Próximo', 'Pendiente de cierre'].includes(e.status)).length,
  pendingClose: store.items.filter((e) => e.status === 'Pendiente de cierre').length,
  balance: store.items.reduce(
    (sum, e) => (e.status !== 'Cancelado' ? sum + Math.max(0, e.outstandingBalance) : sum),
    0,
  ),
  total: store.items.length,
}))

async function selectEvent(id) {
  selectedId.value = id
  await store.fetchEvent(id)
}

// En desktop el detalle vive en el panel lateral; el drawer es solo para mobile.
async function openDetail(id) {
  await selectEvent(id)
  detailOpen.value = window.matchMedia('(max-width: 1023px)').matches
}

function openPayment() {
  localError.value = ''
  paymentForm.value = { paymentMethod: 'Efectivo', amount: '' }
  paymentOpen.value = true
}

async function submitPayment() {
  localError.value = ''
  if (!selected.value || Number(paymentForm.value.amount) <= 0) {
    localError.value = 'Ingresá un monto mayor a cero.'
    return
  }
  try {
    await store.registerPayment(selected.value.id, {
      paymentMethod: paymentForm.value.paymentMethod,
      amount: Number(paymentForm.value.amount),
    })
    paymentOpen.value = false
    toasts.success('Pago registrado.')
  } catch {
    toasts.error(store.operationError || 'No se pudo registrar el pago.')
  }
}

function openComplete() {
  localError.value = ''
  completeForm.value = { extraPizzas: 0, description: '', amount: '' }
  completeOpen.value = true
}

async function submitComplete() {
  localError.value = ''
  if (!selected.value) return
  if (Number(completeForm.value.extraPizzas) < 0) {
    localError.value = 'Las pizzas extra no pueden ser negativas.'
    return
  }
  const extraSurcharges = completeForm.value.description.trim()
    ? [
        {
          description: completeForm.value.description.trim(),
          amount: Number(completeForm.value.amount),
        },
      ]
    : []
  if (extraSurcharges.length && extraSurcharges[0].amount <= 0) {
    localError.value = 'El viático extra debe tener un monto mayor a cero.'
    return
  }
  try {
    await store.complete(selected.value.id, {
      extraPizzas: Number(completeForm.value.extraPizzas || 0),
      extraSurcharges,
    })
    completeOpen.value = false
    toasts.success('Evento cerrado.')
  } catch {
    toasts.error(store.operationError || 'No se pudo cerrar el evento.')
  }
}

// Acciones desde el drawer mobile: cierran el detalle antes de abrir el siguiente paso.
async function submitEdit(payload) {
  if (!selected.value) return
  try {
    await store.editEvent(selected.value.id, payload)
    editOpen.value = false
    toasts.success('Evento actualizado.')
  } catch {
    toasts.error(store.operationError || 'No se pudo actualizar el evento.')
  }
}

function openEditFromMobile() {
  detailOpen.value = false
  editOpen.value = true
}

function openPaymentFromMobile() {
  detailOpen.value = false
  openPayment()
}

function openCompleteFromMobile() {
  detailOpen.value = false
  openComplete()
}

function cancelFromMobile() {
  detailOpen.value = false
  cancelTarget.value = selected.value
}

async function cancelEvent() {
  if (!cancelTarget.value) return
  try {
    await store.cancel(cancelTarget.value.id)
    toasts.success('Evento cancelado.')
    cancelTarget.value = null
  } catch {
    toasts.error(store.operationError || 'No se pudo cancelar el evento.')
  }
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1200px]">
    <PageHeader title="Eventos" subtitle="Reservas, pagos, viáticos y cierres">
      <template #right>
        <UiButton variant="primary" @click="router.push('/admin/eventos/nuevo')">
          <span class="material-symbols-outlined !text-lg" aria-hidden="true">event_available</span>
          Evento
        </UiButton>
      </template>
    </PageHeader>

    <div class="grid gap-4 px-4 pb-8 md:px-8 md:pb-10">
      <!-- Stats -->
      <div class="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <StatCard label="Activos" icon="event" :value="stats.active" />
        <StatCard label="Por cerrar" icon="pending" :value="stats.pendingClose" />
        <StatCard
          label="Saldo"
          icon="account_balance_wallet"
          :value="formatMoney(stats.balance)"
          tone="accent"
        />
        <StatCard label="Total" icon="format_list_numbered" :value="stats.total" />
      </div>

      <!-- Filtros + búsqueda -->
      <div class="flex flex-wrap items-center gap-2.5">
        <TabBar v-model="activeFilter" :tabs="FILTERS" />
        <div class="min-w-[180px] flex-1">
          <SearchInput v-model="searchTerm" placeholder="Buscar cliente o ubicación" />
        </div>
      </div>

      <ErrorState v-if="store.error" :message="store.error" />
      <div v-else-if="store.isLoading && store.items.length === 0" class="grid gap-2">
        <SkeletonBlock v-for="i in 6" :key="i" height="72px" />
      </div>
      <EmptyState
        v-else-if="filteredEvents.length === 0"
        title="Sin eventos en esta vista"
        message="Cuando haya reservas o cierres pendientes, aparecen acá."
      />

      <!-- Lista + detalle -->
      <div v-else class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_360px] lg:items-start">
        <div class="list-panel">
          <button
            v-for="event in filteredEvents"
            :key="event.id"
            type="button"
            :class="[
              'list-row transition-colors hover:bg-surface-2/50',
              selectedId === event.id ? 'bg-primary/10' : '',
            ]"
            @click="openDetail(event.id)"
          >
            <div class="min-w-0">
              <div class="mb-1 flex items-center gap-2">
                <StatusBadge :event-status="event.status" />
                <span class="font-mono text-[0.72rem] text-muted">{{
                  formatDateTime(event.eventDate)
                }}</span>
              </div>
              <div class="list-row-title">{{ event.customerName }}</div>
              <div class="list-row-meta">{{ event.location }} · {{ event.pizzaCount }} pizzas</div>
            </div>
            <div class="flex shrink-0 flex-col items-end gap-1">
              <strong
                :class="
                  event.outstandingBalance > 0
                    ? 'text-[0.95rem] font-extrabold text-danger'
                    : 'text-base font-extrabold text-success'
                "
              >
                {{ event.outstandingBalance > 0 ? formatMoney(event.outstandingBalance) : '✓' }}
              </strong>
              <span class="text-[0.68rem] font-semibold text-muted">saldo</span>
            </div>
          </button>
        </div>

        <!-- Detalle desktop -->
        <div class="hidden gap-4 lg:sticky lg:top-5 lg:grid">
          <SkeletonBlock v-if="store.isLoadingDetail" height="220px" />
          <EmptyState
            v-else-if="!selected"
            :mascot="false"
            icon="event_note"
            title="Elegí un evento"
            message="Pagos, viáticos y acciones aparecen aquí."
          />
          <div v-else class="grid gap-3">
            <div class="list-panel">
              <div class="list-row">
                <div class="min-w-0">
                  <div class="mb-1.5"><StatusBadge :event-status="selected.status" /></div>
                  <div class="list-row-title">{{ selected.customerName }}</div>
                  <div class="list-row-meta">
                    {{ formatDateTime(selected.eventDate) }} · {{ selected.location }}
                  </div>
                </div>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-2">
              <StatCard label="Total" :value="formatMoney(selected.totalCost)" />
              <StatCard
                label="Saldo"
                :value="formatMoney(selected.outstandingBalance)"
                :tone="selected.outstandingBalance > 0 ? 'accent' : 'default'"
              />
              <StatCard label="Pizzas" :value="selected.pizzaCount" />
              <StatCard label="Seña" :value="formatMoney(selected.deposit)" />
            </div>

            <div class="section-label mt-2"><span>Pagos</span></div>
            <div class="list-panel">
              <p v-if="selected.payments.length === 0" class="list-row text-sm text-muted">
                Sin pagos registrados.
              </p>
              <div v-for="payment in selected.payments" :key="payment.id" class="list-row">
                <span>{{ payment.paymentMethod }}</span
                ><strong>{{ formatMoney(payment.amount) }}</strong>
              </div>
            </div>

            <div class="section-label"><span>Viáticos</span></div>
            <div class="list-panel">
              <p v-if="selected.surcharges.length === 0" class="list-row text-sm text-muted">
                Sin viáticos.
              </p>
              <div v-for="s in selected.surcharges" :key="s.id" class="list-row">
                <span>{{ s.description }}</span
                ><strong>{{ formatMoney(s.amount) }}</strong>
              </div>
            </div>

            <div
              v-if="selected.notes"
              class="rounded-[10px] border border-line-soft bg-surface-2 px-3 py-2.5 text-[0.88rem] text-muted"
            >
              {{ selected.notes }}
            </div>

            <div v-if="canAct" class="mt-2 grid gap-2">
              <UiButton variant="primary" @click="openPayment">Registrar pago</UiButton>
              <UiButton variant="secondary" @click="openComplete">Cerrar evento</UiButton>
              <UiButton variant="secondary" @click="editOpen = true">Editar evento</UiButton>
              <UiButton variant="ghost" class="text-danger" @click="cancelTarget = selected"
                >Cancelar evento</UiButton
              >
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Drawer detalle mobile -->
    <AppDrawer
      :open="detailOpen && Boolean(selected)"
      :title="selected?.customerName || ''"
      :description="selected ? formatDateTime(selected.eventDate) + ' · ' + selected.location : ''"
      @close="detailOpen = false"
    >
      <div v-if="selected" class="grid gap-5">
        <div class="flex items-center justify-between gap-3 rounded-xl border border-line bg-surface p-3">
          <StatusBadge :event-status="selected.status" />
          <span class="text-[0.78rem] font-bold text-muted">{{ selected.location }}</span>
        </div>

        <div class="grid grid-cols-2 gap-2">
          <StatCard label="Total" :value="formatMoney(selected.totalCost)" />
          <StatCard
            label="Saldo"
            :value="formatMoney(selected.outstandingBalance)"
            :tone="selected.outstandingBalance > 0 ? 'accent' : 'default'"
          />
          <StatCard label="Pizzas" :value="selected.pizzaCount" />
          <StatCard label="Seña" :value="formatMoney(selected.deposit)" />
        </div>

        <section>
          <h3 class="text-base font-bold">Detalle</h3>
          <div class="list-panel mt-2">
            <div class="list-row">
              <span>Fecha y hora</span>
              <strong>{{ formatDateTime(selected.eventDate) }}</strong>
            </div>
            <div class="list-row">
              <span>Ubicación</span>
              <strong class="text-right">{{ selected.location }}</strong>
            </div>
            <div class="list-row">
              <span>Precio por pizza</span>
              <strong>{{ formatMoney(selected.pricePerPizza) }}</strong>
            </div>
          </div>
        </section>

        <section>
          <h3 class="text-base font-bold">Pagos</h3>
          <div class="list-panel mt-2">
            <p v-if="selected.payments.length === 0" class="list-row text-sm text-muted">
              Sin pagos.
            </p>
            <div v-for="payment in selected.payments" :key="payment.id" class="list-row">
              <span>{{ payment.paymentMethod }}</span>
              <strong>{{ formatMoney(payment.amount) }}</strong>
            </div>
          </div>
        </section>

        <section>
          <h3 class="text-base font-bold">Viáticos</h3>
          <div class="list-panel mt-2">
            <p v-if="selected.surcharges.length === 0" class="list-row text-sm text-muted">
              Sin viáticos.
            </p>
            <div v-for="s in selected.surcharges" :key="s.id" class="list-row">
              <span>{{ s.description }}</span>
              <strong>{{ formatMoney(s.amount) }}</strong>
            </div>
          </div>
        </section>

        <div
          v-if="selected.notes"
          class="rounded-[10px] border border-line-soft bg-surface-2 px-3 py-2.5 text-[0.88rem] text-muted"
        >
          {{ selected.notes }}
        </div>

        <div v-if="canAct" class="grid gap-2">
          <UiButton variant="primary" @click="openPaymentFromMobile">Registrar pago</UiButton>
          <UiButton variant="secondary" @click="openCompleteFromMobile">Cerrar evento</UiButton>
          <UiButton variant="secondary" @click="openEditFromMobile">Editar evento</UiButton>
          <UiButton variant="ghost" class="text-danger" @click="cancelFromMobile"
            >Cancelar evento</UiButton
          >
        </div>
      </div>
    </AppDrawer>

    <!-- Drawer registrar pago -->
    <AppDrawer
      :open="paymentOpen"
      title="Registrar pago"
      :description="selected?.customerName"
      @close="paymentOpen = false"
    >
      <form class="grid gap-4" @submit.prevent="submitPayment">
        <div
          v-if="localError || store.operationError"
          class="rounded-xl bg-danger-soft px-3 py-2.5 text-sm font-bold text-danger"
        >
          {{ localError || store.operationError }}
        </div>
        <UiSelect
          v-model="paymentForm.paymentMethod"
          label="Método"
          :options="paymentOptions"
          required
        />
        <UiInput
          v-model="paymentForm.amount"
          label="Monto"
          type="number"
          inputmode="numeric"
          min="0"
          step="0.01"
          required
        />
      </form>
      <template #footer>
        <UiButton variant="secondary" :disabled="store.isSaving" @click="paymentOpen = false"
          >Cancelar</UiButton
        >
        <UiButton variant="primary" :loading="store.isSaving" @click="submitPayment"
          >Guardar pago</UiButton
        >
      </template>
    </AppDrawer>

    <!-- Drawer cerrar evento -->
    <AppDrawer
      :open="completeOpen"
      title="Cerrar evento"
      :description="selected?.customerName"
      @close="completeOpen = false"
    >
      <form class="grid gap-4" @submit.prevent="submitComplete">
        <div
          v-if="localError || store.operationError"
          class="rounded-xl bg-danger-soft px-3 py-2.5 text-sm font-bold text-danger"
        >
          {{ localError || store.operationError }}
        </div>
        <UiInput
          v-model="completeForm.extraPizzas"
          label="Pizzas extra"
          type="number"
          inputmode="numeric"
          min="0"
        />
        <section class="rounded-xl border border-line-soft bg-surface-2 p-4">
          <h3 class="text-base font-bold">Viático extra (opcional)</h3>
          <div class="mt-4 grid gap-4">
            <UiInput
              v-model="completeForm.description"
              label="Descripción"
              placeholder="Traslado extra"
            />
            <UiInput
              v-model="completeForm.amount"
              label="Monto"
              type="number"
              inputmode="numeric"
              min="0"
              step="0.01"
            />
          </div>
        </section>
      </form>
      <template #footer>
        <UiButton variant="secondary" :disabled="store.isSaving" @click="completeOpen = false"
          >Cancelar</UiButton
        >
        <UiButton variant="primary" :loading="store.isSaving" @click="submitComplete"
          >Cerrar evento</UiButton
        >
      </template>
    </AppDrawer>

    <EventEditorDrawer
      :open="editOpen"
      :event="selected"
      :is-saving="store.isSaving"
      :error-message="store.operationError"
      @close="editOpen = false"
      @save="submitEdit"
    />

    <ConfirmDialog
      :open="Boolean(cancelTarget)"
      title="Cancelar evento"
      :message="cancelTarget ? `Se cancelará el evento de ${cancelTarget.customerName}.` : ''"
      confirm-label="Cancelar evento"
      cancel-label="Volver"
      :loading="store.isSaving"
      @close="cancelTarget = null"
      @confirm="cancelEvent"
    />
  </div>
</template>
