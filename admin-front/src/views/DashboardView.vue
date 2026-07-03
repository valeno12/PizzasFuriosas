<script setup>
import { computed, onMounted, ref } from 'vue'
import PageHeader from '../components/shared/PageHeader.vue'
import StatCard from '../components/ui/StatCard.vue'
import TabBar from '../components/ui/TabBar.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ErrorState from '../components/ui/ErrorState.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import { useEventsStore } from '../stores/events'
import { getBalance, getStatistics } from '../services/dashboardService'
import { getApiErrorMessage } from '../services/apiErrors'
import { customRange, lastDaysRange, monthRange, todayRange } from '../utils/dates'
import { formatMoney, formatNumber } from '../utils/formatters'

const eventsStore = useEventsStore()

const PERIODS = [
  { value: 'hoy', label: 'Hoy' },
  { value: '7d', label: '7 días' },
  { value: 'mes', label: 'Este mes' },
  { value: 'todo', label: 'Todo' },
  { value: 'custom', label: 'Elegir fechas' },
]

const period = ref('mes')
const customFrom = ref('')
const customTo = ref('')
const balance = ref(null)
const stats = ref(null)
const isLoading = ref(false)
const error = ref(null)

function periodParams() {
  if (period.value === 'hoy') return todayRange()
  if (period.value === '7d') return lastDaysRange(7)
  if (period.value === 'mes') return monthRange()
  if (period.value === 'custom') return customRange(customFrom.value, customTo.value)
  return {}
}

async function loadData() {
  isLoading.value = true
  error.value = null
  try {
    const params = periodParams()
    const [balanceData, statsData] = await Promise.all([getBalance(params), getStatistics(params)])
    balance.value = balanceData
    stats.value = statsData
  } catch (err) {
    console.error('Error fetching dashboard:', err)
    error.value = getApiErrorMessage(err, 'No se pudieron cargar los rendimientos.')
  } finally {
    isLoading.value = false
  }
}

function selectPeriod(value) {
  period.value = value
  // Con fechas propias espera a que el usuario las elija.
  if (value !== 'custom') loadData()
}

function onCustomDateChange() {
  if (customFrom.value || customTo.value) loadData()
}

onMounted(() => {
  loadData()
  eventsStore.fetchEvents({ pageSize: 20 })
})

const totalOrders = computed(() =>
  stats.value ? stats.value.totalDeliveryOrders + stats.value.totalTakeAwayOrders : 0,
)

// Ancho relativo de las barras del ranking (el más vendido = 100%).
const maxProductQuantity = computed(() =>
  Math.max(1, ...(stats.value?.topProducts.map((p) => p.totalQuantitySold) ?? [1])),
)

const pendingCloseEvents = computed(() =>
  eventsStore.items.filter((e) => e.status === 'Pendiente de cierre'),
)

function splitPercent(a, b) {
  const total = a + b
  return total === 0 ? 50 : Math.round((a / total) * 100)
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1200px]">
    <PageHeader title="Rendimientos" subtitle="Cómo viene el negocio, en números" />

    <div class="grid gap-4 px-4 pb-8 md:px-8 md:pb-10">
      <!-- Período -->
      <div class="flex flex-wrap items-center gap-2.5">
        <TabBar :tabs="PERIODS" :model-value="period" @update:model-value="selectPeriod" />
        <div v-if="period === 'custom'" class="flex items-center gap-2">
          <input
            v-model="customFrom"
            type="date"
            class="form-control h-9 min-h-0 w-auto px-2.5 text-sm"
            aria-label="Desde"
            @change="onCustomDateChange"
          />
          <span class="text-sm text-muted">→</span>
          <input
            v-model="customTo"
            type="date"
            class="form-control h-9 min-h-0 w-auto px-2.5 text-sm"
            aria-label="Hasta"
            @change="onCustomDateChange"
          />
        </div>
      </div>

      <ErrorState v-if="error" :message="error" />

      <div v-else-if="isLoading && !balance" class="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <SkeletonBlock v-for="i in 4" :key="i" height="70px" />
      </div>

      <template v-else-if="balance && stats">
        <!-- KPIs principales -->
        <div class="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <StatCard
            label="Ingresos"
            icon="trending_up"
            :value="formatMoney(balance.totalGrossIncome)"
            tone="accent"
          />
          <StatCard label="Pedidos" icon="receipt_long" :value="formatNumber(totalOrders)" />
          <StatCard
            label="Ticket promedio"
            icon="sell"
            :value="formatMoney(stats.averageOrderValue)"
          />
          <StatCard
            label="Ganancia neta"
            icon="savings"
            :value="formatMoney(balance.netProfit)"
            :tone="balance.netProfit >= 0 ? 'accent' : 'danger'"
          />
        </div>

        <EmptyState
          v-if="totalOrders === 0 && balance.totalGrossIncome === 0"
          icon="query_stats"
          title="Sin movimientos en este período"
          message="Cuando haya pedidos entregados, acá aparecen los números."
        />

        <div v-else class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_360px] lg:items-start">
          <!-- Columna principal: rankings -->
          <div class="grid gap-4">
            <div>
              <div class="section-label"><span>Pizzas más vendidas</span></div>
              <p v-if="stats.topProducts.length === 0" class="py-4 text-center text-sm text-muted">
                Sin ventas en este período.
              </p>
              <div v-else class="list-panel">
                <div
                  v-for="(product, index) in stats.topProducts"
                  :key="product.productId"
                  class="grid gap-1.5 px-3.5 py-3"
                >
                  <div class="flex items-center justify-between gap-2">
                    <span class="min-w-0 truncate text-[0.94rem] font-bold">
                      <span class="mr-1 font-mono text-[0.78rem] text-muted">{{ index + 1 }}.</span>
                      {{ product.productName }}
                    </span>
                    <span class="shrink-0 text-sm font-extrabold text-primary"
                      >{{ formatNumber(product.totalQuantitySold) }} u.</span
                    >
                  </div>
                  <div class="h-1.5 overflow-hidden rounded-full bg-surface-2">
                    <div
                      class="h-full rounded-full bg-primary"
                      :style="{
                        width: `${(product.totalQuantitySold / maxProductQuantity) * 100}%`,
                      }"
                    ></div>
                  </div>
                </div>
              </div>
            </div>

            <div>
              <div class="section-label"><span>Mejores clientes</span></div>
              <p v-if="stats.topCustomers.length === 0" class="py-4 text-center text-sm text-muted">
                Sin clientes en este período.
              </p>
              <div v-else class="list-panel">
                <div
                  v-for="customer in stats.topCustomers"
                  :key="customer.customerId"
                  class="list-row"
                >
                  <div class="min-w-0">
                    <div class="list-row-title">{{ customer.customerName }}</div>
                    <div class="list-row-meta">
                      {{ customer.totalOrders }} pedido{{ customer.totalOrders !== 1 ? 's' : '' }}
                    </div>
                  </div>
                  <span class="font-extrabold text-primary">{{
                    formatMoney(customer.totalSpent)
                  }}</span>
                </div>
              </div>
            </div>

            <!-- Cómo venden: canales y medios de pago -->
            <div>
              <div class="section-label"><span>Cómo venden</span></div>
              <div class="grid gap-2 sm:grid-cols-2">
                <div class="grid gap-2 rounded-[14px] border border-line-soft bg-surface p-3.5">
                  <div class="flex items-center justify-between text-sm font-bold">
                    <span class="inline-flex items-center gap-1.5">
                      <span
                        class="material-symbols-outlined !text-base text-primary"
                        aria-hidden="true"
                        >pedal_bike</span
                      >
                      Delivery · {{ stats.totalDeliveryOrders }}
                    </span>
                    <span class="inline-flex items-center gap-1.5 text-muted">
                      Take away · {{ stats.totalTakeAwayOrders }}
                      <span class="material-symbols-outlined !text-base" aria-hidden="true"
                        >shopping_bag</span
                      >
                    </span>
                  </div>
                  <div class="flex h-1.5 overflow-hidden rounded-full bg-surface-2">
                    <div
                      class="h-full bg-primary"
                      :style="{
                        width: `${splitPercent(stats.totalDeliveryOrders, stats.totalTakeAwayOrders)}%`,
                      }"
                    ></div>
                  </div>
                </div>

                <div class="grid gap-2 rounded-[14px] border border-line-soft bg-surface p-3.5">
                  <div class="flex items-center justify-between text-sm font-bold">
                    <span class="inline-flex items-center gap-1.5">
                      <span
                        class="material-symbols-outlined !text-base text-success"
                        aria-hidden="true"
                        >payments</span
                      >
                      Efectivo · {{ stats.totalCashPayments }}
                    </span>
                    <span class="inline-flex items-center gap-1.5 text-muted">
                      Transferencia · {{ stats.totalTransferPayments }}
                      <span class="material-symbols-outlined !text-base" aria-hidden="true"
                        >sync_alt</span
                      >
                    </span>
                  </div>
                  <div class="flex h-1.5 overflow-hidden rounded-full bg-surface-2">
                    <div
                      class="h-full bg-success"
                      :style="{
                        width: `${splitPercent(stats.totalCashPayments, stats.totalTransferPayments)}%`,
                      }"
                    ></div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Panel financiero -->
          <aside class="grid gap-4 lg:sticky lg:top-5">
            <div>
              <div class="section-label"><span>Financiero</span></div>
              <div class="list-panel">
                <div class="list-row">
                  <div class="min-w-0">
                    <div class="list-row-title">Ingresos por pedidos</div>
                    <div class="list-row-meta">Pedidos entregados</div>
                  </div>
                  <span class="font-extrabold text-primary">{{
                    formatMoney(balance.totalOrdersIncome)
                  }}</span>
                </div>
                <div class="list-row">
                  <div class="min-w-0">
                    <div class="list-row-title">Ingresos por eventos</div>
                    <div class="list-row-meta">Eventos completados</div>
                  </div>
                  <span class="font-extrabold text-primary">{{
                    formatMoney(balance.totalEventsIncome)
                  }}</span>
                </div>
                <div class="list-row">
                  <div class="min-w-0">
                    <div class="list-row-title">Envíos</div>
                    <div class="list-row-meta">Costo de deliveries</div>
                  </div>
                  <span class="font-extrabold text-muted">{{
                    formatMoney(balance.deliveryFees)
                  }}</span>
                </div>
                <div class="list-row">
                  <div class="min-w-0">
                    <div class="list-row-title">Gastos</div>
                    <div class="list-row-meta">Egresos del período</div>
                  </div>
                  <span class="font-extrabold text-danger">{{
                    formatMoney(balance.totalExpenses)
                  }}</span>
                </div>
                <div class="list-row bg-surface-2/50">
                  <div class="min-w-0">
                    <div class="list-row-title">Ganancia neta</div>
                  </div>
                  <span
                    :class="[
                      'text-lg font-extrabold',
                      balance.netProfit >= 0 ? 'text-success' : 'text-danger',
                    ]"
                  >
                    {{ formatMoney(balance.netProfit) }}
                  </span>
                </div>
                <div v-if="pendingCloseEvents.length > 0" class="list-row">
                  <div class="min-w-0">
                    <div class="list-row-title">Eventos por cerrar</div>
                    <div class="list-row-meta">Todavía no suman al balance</div>
                  </div>
                  <span class="font-extrabold text-warning">{{ pendingCloseEvents.length }}</span>
                </div>
              </div>
            </div>
          </aside>
        </div>
      </template>
    </div>
  </div>
</template>
