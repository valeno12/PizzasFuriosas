<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import PageHeader from '../components/shared/PageHeader.vue'
import UiButton from '../components/ui/UiButton.vue'
import StatCard from '../components/ui/StatCard.vue'
import StatusBadge from '../components/ui/StatusBadge.vue'
import TabBar from '../components/ui/TabBar.vue'
import SearchInput from '../components/ui/SearchInput.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ErrorState from '../components/ui/ErrorState.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import AppDrawer from '../components/ui/AppDrawer.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import PaginationBar from '../components/ui/PaginationBar.vue'
import { useOrdersStore } from '../stores/orders'
import { useToastsStore } from '../stores/toasts'
import { useDebounce } from '../composables/useDebounce'
import { ORDER_STATUS, isFinalStatus, orderAdvance } from '../constants/status'
import { formatAddress, mapsUrl, whatsappUrl } from '../utils/links'
import { formatTime } from '../utils/formatters'
import { customRange, todayRange } from '../utils/dates'
import { formatDateTime, formatMoney } from '../utils/formatters'

const HISTORY_PAGE_SIZE = 20

const router = useRouter()
const store = useOrdersStore()
const toasts = useToastsStore()
const { debounce } = useDebounce()

const activeTab = ref('hoy')
const searchTerm = ref('')
const historyFrom = ref('')
const historyTo = ref('')
const selectedOrderId = ref(null)
const updatingId = ref(null)
const cancelTarget = ref(null)
const isCancelling = ref(false)

const TABS = [
  { value: 'hoy', label: 'Hoy' },
  { value: 'historico', label: 'Histórico' },
]

function fetchCurrentTab(page = 1) {
  if (activeTab.value === 'hoy') {
    const { from, to } = todayRange()
    return store.fetchOrders({ from, to, pageSize: 200 })
  }
  const params = {
    page,
    pageSize: HISTORY_PAGE_SIZE,
    ...customRange(historyFrom.value, historyTo.value),
  }
  const search = searchTerm.value.trim()
  if (search) params.search = search
  return store.fetchOrders(params)
}

function onHistoryDateChange() {
  fetchCurrentTab()
}

onMounted(fetchCurrentTab)

function selectTab(tab) {
  activeTab.value = tab
  fetchCurrentTab()
}

// Hoy filtra en memoria (pocos pedidos); Histórico busca en el backend.
function handleSearch(value) {
  searchTerm.value = value
  if (activeTab.value === 'historico') {
    debounce(fetchCurrentTab, 300)
  }
}

const visibleOrders = computed(() => {
  if (activeTab.value !== 'hoy') return store.items

  const term = searchTerm.value.trim().toLowerCase()
  return store.items.filter((order) => {
    if (order.statusId === ORDER_STATUS.CANCELADO) return false
    if (!term) return true
    return (
      order.customerName.toLowerCase().includes(term) ||
      String(order.id).includes(term) ||
      order.items.some((item) => item.name.toLowerCase().includes(term))
    )
  })
})

const summary = computed(() => ({
  active: store.items.filter((o) => !isFinalStatus(o.statusId)).length,
  ready: store.items.filter((o) => o.statusId === ORDER_STATUS.LISTO).length,
  delivery: store.items.filter((o) => o.statusId === ORDER_STATUS.EN_CAMINO).length,
  revenue: store.items
    .filter((o) => o.statusId === ORDER_STATUS.ENTREGADO)
    .reduce((sum, o) => sum + Number(o.total || 0), 0),
}))

const selectedOrder = computed(
  () => store.items.find((o) => o.id === selectedOrderId.value) || null,
)

async function changeStatus(order, statusId) {
  updatingId.value = order.id
  try {
    await store.updateOrderStatus(order.id, statusId)
    toasts.success(`Pedido #${order.id} actualizado.`)
  } catch {
    toasts.error(store.operationError || 'No se pudo actualizar el estado del pedido.')
  } finally {
    updatingId.value = null
  }
}

async function confirmCancel() {
  if (!cancelTarget.value) return
  isCancelling.value = true
  try {
    await store.updateOrderStatus(cancelTarget.value.id, ORDER_STATUS.CANCELADO)
    toasts.success(`Pedido #${cancelTarget.value.id} cancelado.`)
    cancelTarget.value = null
    selectedOrderId.value = null
  } catch {
    toasts.error(store.operationError || 'No se pudo cancelar el pedido.')
  } finally {
    isCancelling.value = false
  }
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1200px]">
    <PageHeader title="Pedidos" subtitle="Seguí el turno o buscá en el histórico">
      <template #right>
        <UiButton variant="primary" @click="router.push('/pedidos/nuevo')">
          <span class="material-symbols-outlined !text-lg" aria-hidden="true">add</span>
          Pedido
        </UiButton>
      </template>
    </PageHeader>

    <div class="grid gap-4 px-4 pb-8 md:px-8 md:pb-10">
      <!-- Stats del día -->
      <div v-if="activeTab === 'hoy'" class="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <StatCard label="Activos" icon="timer" :value="summary.active" />
        <StatCard label="Listos" icon="check_circle" :value="summary.ready" />
        <StatCard label="En camino" icon="pedal_bike" :value="summary.delivery" />
        <StatCard
          label="Entregado"
          icon="trending_up"
          :value="formatMoney(summary.revenue)"
          tone="accent"
        />
      </div>

      <!-- Toolbar -->
      <div class="flex flex-wrap items-center gap-3">
        <TabBar :tabs="TABS" :model-value="activeTab" @update:model-value="selectTab" />
        <div v-if="activeTab === 'historico'" class="flex items-center gap-2">
          <input
            v-model="historyFrom"
            type="date"
            class="form-control h-9 min-h-0 w-auto px-2.5 text-sm"
            aria-label="Desde"
            @change="onHistoryDateChange"
          />
          <span class="text-sm text-muted">→</span>
          <input
            v-model="historyTo"
            type="date"
            class="form-control h-9 min-h-0 w-auto px-2.5 text-sm"
            aria-label="Hasta"
            @change="onHistoryDateChange"
          />
        </div>
        <div class="min-w-[200px] flex-1">
          <SearchInput
            :model-value="searchTerm"
            placeholder="Buscar por cliente o número"
            @update:model-value="handleSearch"
          />
        </div>
      </div>

      <ErrorState v-if="store.error" :message="store.error" />

      <div v-else-if="store.isLoading && store.items.length === 0" class="grid gap-2">
        <SkeletonBlock v-for="i in 7" :key="i" height="70px" />
      </div>

      <EmptyState
        v-else-if="visibleOrders.length === 0"
        title="Sin pedidos en este filtro"
        message="Probá con otra búsqueda o tab."
      />

      <template v-else>
        <div class="list-panel">
          <button
            v-for="order in visibleOrders"
            :key="order.id"
            type="button"
            class="list-row grid-cols-[48px_minmax(0,1fr)_auto] transition-colors hover:bg-surface-2/50"
            @click="selectedOrderId = order.id"
          >
            <div class="shrink-0 text-center">
              <div class="font-mono text-xs font-bold text-muted">#{{ order.id }}</div>
              <div v-if="order.scheduledFor" class="mt-0.5 text-[0.68rem] font-bold text-warning">
                ⏰ {{ formatTime(order.scheduledFor) }}
              </div>
              <div v-else class="mt-0.5 text-[0.68rem] text-muted">
                {{ formatDateTime(order.createdAt) }}
              </div>
            </div>

            <div class="min-w-0">
              <div class="flex min-w-0 items-center gap-2">
                <span class="list-row-title">{{ order.customerName }}</span>
                <StatusBadge :status-id="order.statusId" />
              </div>
              <div class="list-row-meta">
                {{ order.items.length }} producto{{ order.items.length !== 1 ? 's' : '' }} ·
                {{ order.shippingMethod }} ·
                {{ order.paymentMethod }}
              </div>
            </div>

            <div class="flex shrink-0 flex-col items-end gap-1">
              <span class="text-[0.95rem] font-extrabold text-primary">{{
                formatMoney(order.total)
              }}</span>
              <span class="text-[0.72rem] font-semibold text-muted">Ver detalle</span>
            </div>
          </button>
        </div>

        <PaginationBar
          v-if="activeTab === 'historico'"
          :page="store.currentPage"
          :total-pages="store.totalPages"
          :is-loading="store.isLoading"
          @prev="fetchCurrentTab(store.currentPage - 1)"
          @next="fetchCurrentTab(store.currentPage + 1)"
        />
      </template>
    </div>

    <!-- Drawer de detalle -->
    <AppDrawer
      :open="Boolean(selectedOrder)"
      :title="selectedOrder ? `Pedido #${selectedOrder.id}` : 'Pedido'"
      :description="selectedOrder?.customerName"
      @close="selectedOrderId = null"
    >
      <div v-if="selectedOrder" class="grid gap-5">
        <section class="grid grid-cols-2 gap-2">
          <div class="rounded-xl bg-surface-2 p-3">
            <p class="mb-1 text-[0.78rem] font-semibold text-muted">Estado</p>
            <div class="mt-2"><StatusBadge :status-id="selectedOrder.statusId" /></div>
          </div>
          <div class="rounded-xl bg-surface-2 p-3">
            <p class="mb-1 text-[0.78rem] font-semibold text-muted">Total</p>
            <strong class="mt-1 block text-2xl font-extrabold text-primary">{{
              formatMoney(selectedOrder.total)
            }}</strong>
          </div>
          <div class="rounded-xl bg-surface-2 p-3">
            <p class="mb-1 text-[0.78rem] font-semibold text-muted">Entrega</p>
            <strong>{{ selectedOrder.shippingMethod }}</strong>
            <p class="text-sm text-muted">{{ selectedOrder.paymentMethod }}</p>
          </div>
          <div class="rounded-xl bg-surface-2 p-3">
            <p class="mb-1 text-[0.78rem] font-semibold text-muted">Fecha</p>
            <strong>{{ formatDateTime(selectedOrder.createdAt) }}</strong>
            <p v-if="selectedOrder.scheduledFor" class="mt-0.5 text-sm font-bold text-warning">
              Para las {{ formatTime(selectedOrder.scheduledFor) }}
            </p>
          </div>
        </section>

        <!-- Notas del pedido -->
        <p
          v-if="selectedOrder.notes"
          class="rounded-xl bg-warning-soft px-3 py-2.5 text-sm font-semibold text-warning"
        >
          {{ selectedOrder.notes }}
        </p>

        <!-- Dirección y contacto -->
        <div
          v-if="selectedOrder.address || selectedOrder.customerPhone"
          class="flex flex-wrap gap-2"
        >
          <a
            v-if="selectedOrder.address"
            :href="mapsUrl(selectedOrder.address)"
            target="_blank"
            rel="noopener"
            class="inline-flex min-h-[38px] items-center gap-1.5 rounded-[10px] border border-line bg-surface px-3 text-sm font-bold transition-colors hover:border-line-strong"
          >
            <span class="material-symbols-outlined !text-lg text-primary" aria-hidden="true"
              >location_on</span
            >
            {{ formatAddress(selectedOrder.address) }}
          </a>
          <a
            v-if="whatsappUrl(selectedOrder.customerPhone)"
            :href="whatsappUrl(selectedOrder.customerPhone)"
            target="_blank"
            rel="noopener"
            class="inline-flex min-h-[38px] items-center gap-1.5 rounded-[10px] border border-success/40 bg-surface px-3 text-sm font-bold text-success transition-colors hover:bg-success-soft"
          >
            <span class="material-symbols-outlined !text-lg" aria-hidden="true">chat</span>
            {{ selectedOrder.customerPhone }}
          </a>
        </div>
        <p v-if="selectedOrder.address?.notes" class="text-sm text-muted">
          {{ selectedOrder.address.notes }}
        </p>

        <section>
          <h3 class="text-base font-bold text-foreground">Productos</h3>
          <div class="list-panel mt-2">
            <div v-for="item in selectedOrder.items" :key="item.id" class="list-row">
              <span
                ><strong>{{ item.quantity }}x</strong> {{ item.name }}</span
              >
              <strong>{{ formatMoney(item.subtotal) }}</strong>
            </div>
          </div>
        </section>

        <div class="grid gap-2">
          <UiButton
            v-if="orderAdvance(selectedOrder)"
            variant="primary"
            :loading="updatingId === selectedOrder.id"
            @click="changeStatus(selectedOrder, orderAdvance(selectedOrder).next.id)"
          >
            {{ orderAdvance(selectedOrder).label }}
          </UiButton>
          <UiButton
            v-if="!isFinalStatus(selectedOrder.statusId)"
            variant="secondary"
            @click="router.push(`/pedidos/${selectedOrder.id}/editar`)"
          >
            Editar pedido
          </UiButton>
          <UiButton
            v-if="!isFinalStatus(selectedOrder.statusId)"
            variant="ghost"
            class="text-danger"
            @click="cancelTarget = selectedOrder"
          >
            Cancelar pedido
          </UiButton>
        </div>
      </div>
    </AppDrawer>

    <ConfirmDialog
      :open="Boolean(cancelTarget)"
      title="Cancelar pedido"
      :message="
        cancelTarget
          ? `Se cancelará el pedido #${cancelTarget.id} de ${cancelTarget.customerName}.`
          : ''
      "
      confirm-label="Cancelar pedido"
      cancel-label="Volver"
      :loading="isCancelling"
      @close="cancelTarget = null"
      @confirm="confirmCancel"
    />
  </div>
</template>
