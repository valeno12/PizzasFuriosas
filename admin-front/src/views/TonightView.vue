<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import PageHeader from '../components/shared/PageHeader.vue'
import UiButton from '../components/ui/UiButton.vue'
import StatCard from '../components/ui/StatCard.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ErrorState from '../components/ui/ErrorState.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import { ORDER_STATUS, ORDER_STATUSES, orderAdvance } from '../constants/status'
import { formatDateTime, formatMoney, formatTime } from '../utils/formatters'
import { formatAddress, mapsUrl, whatsappUrl } from '../utils/links'
import { useTonight } from '../composables/useTonight'

const router = useRouter()
const {
  items,
  isLoading,
  error,
  updatingId,
  oldOrderWarning,
  activeOrders,
  deliveredToday,
  deliveredRevenue,
  readyCount,
  updateStatus,
} = useTonight()

const expandedId = ref(null)
const cancelTarget = ref(null)
const isCancelling = ref(false)

const todayLabel = computed(() =>
  new Intl.DateTimeFormat('es-AR', { weekday: 'long', day: 'numeric', month: 'long' }).format(
    new Date(),
  ),
)

// Grupos visibles de la cola, en orden de despacho.
const GROUPS = [
  {
    statusId: ORDER_STATUS.PENDIENTE,
    label: 'Pendientes',
    icon: 'pending_actions',
    color: 'var(--warning)',
  },
  {
    statusId: ORDER_STATUS.EN_PREPARACION,
    label: 'En Cocina',
    icon: 'skillet',
    color: 'var(--primary)',
  },
  {
    statusId: ORDER_STATUS.LISTO,
    label: 'Listos para salir',
    icon: 'check_circle',
    color: 'var(--success)',
  },
  {
    statusId: ORDER_STATUS.EN_CAMINO,
    label: 'En Camino',
    icon: 'two_wheeler',
    color: 'color-mix(in oklch, var(--primary) 60%, white)',
  },
]

const groupedOrders = computed(() =>
  GROUPS.map((group) => ({
    ...group,
    orders: activeOrders.value.filter((o) => o.statusId === group.statusId),
  })).filter((group) => group.orders.length > 0),
)

// Botones de cambio manual: estados activos + Entregado (Cancelado va aparte, con confirmación).
const statusButtons = ORDER_STATUSES.filter((s) => s.id !== ORDER_STATUS.CANCELADO)

function itemSummary(order) {
  if (!order.items?.length) return ''
  const names = order.items.slice(0, 2).map((i) => `${i.quantity}× ${i.name}`)
  const extra = order.items.length - names.length
  return extra > 0 ? `${names.join(', ')} +${extra}` : names.join(', ')
}

function toggleOrder(id) {
  expandedId.value = expandedId.value === id ? null : id
}

// Grupos de estado colapsados (para achicar la vista en noches cargadas).
const collapsedGroups = ref(new Set())

function toggleGroup(statusId) {
  const next = new Set(collapsedGroups.value)
  if (next.has(statusId)) next.delete(statusId)
  else next.add(statusId)
  collapsedGroups.value = next
}

function orderItemLines(order) {
  return order.items?.length
    ? order.items.map((item) => `- ${item.quantity} x ${item.name}`)
    : ['- Detalle a confirmar']
}

function deliveryLine(order) {
  if (order.shippingMethod !== 'Delivery') return 'Entrega: Retiro en local'
  return `Entrega: Delivery${order.address ? ` a ${formatAddress(order.address)}` : ''}`
}

function paymentSummary(order) {
  return [`Pago: ${order.paymentMethod}`, `Total: ${formatMoney(order.total)}`]
}

function wspMessage(order) {
  if (order.statusId === ORDER_STATUS.LISTO && order.shippingMethod !== 'Delivery') {
    return [
      `¡Hola ${order.customerName}! Tu pedido de Pizzas Furiosas ya está listo para retirar.`,
      ...paymentSummary(order),
    ].join(String.fromCharCode(10))
  }

  if (order.statusId === ORDER_STATUS.LISTO && order.shippingMethod === 'Delivery') {
    return [
      `¡Hola ${order.customerName}! Tu pedido de Pizzas Furiosas ya está listo y sale en unos minutos.`,
      deliveryLine(order),
      ...paymentSummary(order),
    ].join(String.fromCharCode(10))
  }

  if (order.statusId === ORDER_STATUS.EN_CAMINO) {
    return [
      `¡Hola ${order.customerName}! Tu pedido de Pizzas Furiosas ya está en camino.`,
      deliveryLine(order),
      ...paymentSummary(order),
    ].join(String.fromCharCode(10))
  }

  return [
    `¡Hola ${order.customerName}! Somos Pizzas Furiosas. Te confirmamos tu pedido:`,
    ...orderItemLines(order),
    deliveryLine(order),
    order.scheduledFor ? `Horario estimado: ${formatTime(order.scheduledFor)}` : '',
    ...paymentSummary(order),
    order.notes ? `Notas: ${order.notes}` : '',
  ]
    .filter(Boolean)
    .join(String.fromCharCode(10))
}

async function changeStatus(order, statusId) {
  const ok = await updateStatus(order.id, statusId)
  if (ok) expandedId.value = null
}

async function confirmCancel() {
  if (!cancelTarget.value) return
  isCancelling.value = true
  try {
    const ok = await updateStatus(cancelTarget.value.id, ORDER_STATUS.CANCELADO)
    if (ok) {
      cancelTarget.value = null
      expandedId.value = null
    }
  } finally {
    isCancelling.value = false
  }
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1200px]">
    <PageHeader title="Esta noche" :subtitle="todayLabel">
      <template #right>
        <UiButton variant="primary" @click="router.push('/pedidos/nuevo')">
          <span class="material-symbols-outlined !text-lg" aria-hidden="true">add</span>
          Pedido
        </UiButton>
      </template>
    </PageHeader>

    <div class="grid gap-4 px-4 pb-8 md:px-8 md:pb-10">
      <!-- Aviso de pedidos viejos sin cerrar: tocá para ir a revisarlos -->
      <button
        v-if="oldOrderWarning"
        type="button"
        class="flex w-full items-center gap-2 rounded-xl border border-warning-soft bg-warning-soft p-3 text-left text-[0.85rem] font-semibold text-warning transition-opacity hover:opacity-85"
        @click="router.push('/pedidos')"
      >
        <span class="material-symbols-outlined !text-lg" aria-hidden="true">warning</span>
        <span class="flex-1">Hay pedidos activos de días anteriores. Tocá para revisarlos.</span>
        <span class="material-symbols-outlined !text-lg" aria-hidden="true">chevron_right</span>
      </button>

      <!-- Stats -->
      <div class="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <StatCard label="En curso" icon="local_fire_department" :value="activeOrders.length" />
        <StatCard
          label="Listos"
          icon="check_circle"
          :value="readyCount"
          :tone="readyCount > 0 ? 'accent' : 'default'"
        />
        <StatCard label="Entregados" icon="receipt" :value="deliveredToday.length" />
        <StatCard
          label="Caja"
          icon="attach_money"
          :value="formatMoney(deliveredRevenue)"
          tone="accent"
        />
      </div>

      <ErrorState v-if="error" :message="error" />

      <div v-else-if="isLoading && items.length === 0" class="grid gap-2">
        <SkeletonBlock v-for="i in 4" :key="i" height="96px" />
      </div>

      <EmptyState
        v-else-if="activeOrders.length === 0"
        icon="local_fire_department"
        title="Horno tranquilo"
        message="No hay pedidos activos. Cuando entre uno aparecerá aquí."
      />

      <div v-else class="flex flex-col">
        <template v-for="group in groupedOrders" :key="group.statusId">
          <!-- Header del grupo: colapsable -->
          <button
            type="button"
            class="section-label mt-5 w-full justify-start gap-2 transition-colors hover:text-foreground"
            :aria-expanded="!collapsedGroups.has(group.statusId)"
            @click="toggleGroup(group.statusId)"
          >
            <span
              class="material-symbols-outlined !text-lg"
              :style="{ color: group.color }"
              aria-hidden="true"
              >{{ group.icon }}</span
            >
            <span class="text-[0.85rem]">{{ group.label }}</span>
            <span
              class="rounded-full bg-surface-2 px-2 py-0.5 text-[0.7rem] font-extrabold text-foreground"
            >
              {{ group.orders.length }}
            </span>
            <span class="material-symbols-outlined ml-auto !text-lg" aria-hidden="true">
              {{ collapsedGroups.has(group.statusId) ? 'expand_more' : 'expand_less' }}
            </span>
          </button>

          <ul
            v-show="!collapsedGroups.has(group.statusId)"
            class="grid list-none gap-2 p-0"
            aria-label="Cola de pedidos activos"
          >
            <li
              v-for="order in group.orders"
              :key="order.id"
              class="overflow-hidden rounded-2xl border-[1.5px] border-line bg-surface transition-colors"
              :class="{ 'border-primary/40': expandedId === order.id }"
              :style="{ borderLeftColor: group.color }"
            >
              <!-- Cabecera clickeable -->
              <button
                type="button"
                class="grid w-full grid-cols-[4px_minmax(0,1fr)_auto] gap-3 px-3.5 py-3 text-left transition-colors hover:bg-surface-2/40"
                @click="toggleOrder(order.id)"
              >
                <span
                  class="min-h-16 self-stretch rounded-full"
                  :style="{ background: group.color }"
                  aria-hidden="true"
                ></span>
                <span class="grid min-w-0 gap-[3px]">
                  <span class="flex flex-wrap items-center gap-2">
                    <span class="font-mono text-[0.72rem] font-bold text-muted"
                      >#{{ order.id }}</span
                    >
                    <span
                      v-if="order.scheduledFor"
                      class="ml-auto inline-flex items-center gap-1 rounded-full bg-warning-soft px-2 py-0.5 text-[0.72rem] font-bold text-warning"
                    >
                      <span class="material-symbols-outlined !text-[13px]" aria-hidden="true"
                        >schedule</span
                      >
                      {{ formatTime(order.scheduledFor) }}
                    </span>
                    <span v-else class="ml-auto text-[0.72rem] text-muted">{{
                      formatDateTime(order.createdAt)
                    }}</span>
                  </span>
                  <span class="truncate text-[1.05rem] font-extrabold text-foreground">{{
                    order.customerName
                  }}</span>
                  <span class="truncate text-[0.82rem] text-muted">{{ itemSummary(order) }}</span>
                </span>
                <span class="flex shrink-0 flex-col items-end gap-1.5">
                  <strong class="text-[1.05rem] font-extrabold text-primary">{{
                    formatMoney(order.total)
                  }}</strong>
                  <span class="flex gap-1">
                    <span
                      class="grid h-7 w-7 place-items-center rounded-lg bg-surface-2 text-muted"
                    >
                      <span class="material-symbols-outlined !text-base" aria-hidden="true">
                        {{ order.shippingMethod === 'Delivery' ? 'pedal_bike' : 'shopping_bag' }}
                      </span>
                    </span>
                    <span
                      class="grid h-7 w-7 place-items-center rounded-lg bg-surface-2 text-muted"
                    >
                      <span class="material-symbols-outlined !text-base" aria-hidden="true">
                        {{ order.paymentMethod === 'Efectivo' ? 'payments' : 'sync_alt' }}
                      </span>
                    </span>
                  </span>
                  <span
                    class="material-symbols-outlined self-end !text-lg text-muted"
                    aria-hidden="true"
                  >
                    {{ expandedId === order.id ? 'expand_less' : 'expand_more' }}
                  </span>
                </span>
              </button>

              <!-- Comanda completa + acciones -->
              <div
                v-if="expandedId === order.id"
                class="grid gap-2 border-t border-line-soft bg-surface-2/40 px-3 pb-3 pt-2.5"
              >
                <!-- Ítems del pedido -->
                <div class="grid gap-1 rounded-xl bg-surface px-3 py-2.5">
                  <div
                    v-for="item in order.items"
                    :key="item.id"
                    class="flex items-center justify-between gap-2 text-[0.9rem]"
                  >
                    <span
                      ><strong>{{ item.quantity }}×</strong> {{ item.name }}</span
                    >
                    <span class="text-muted">{{ formatMoney(item.subtotal) }}</span>
                  </div>
                </div>

                <!-- Notas del pedido -->
                <p
                  v-if="order.notes"
                  class="rounded-xl bg-warning-soft px-3 py-2 text-[0.85rem] font-semibold text-warning"
                >
                  {{ order.notes }}
                </p>

                <!-- Dirección y contacto accionables -->
                <div v-if="order.address || order.customerPhone" class="flex flex-wrap gap-1.5">
                  <a
                    v-if="order.address"
                    :href="mapsUrl(order.address)"
                    target="_blank"
                    rel="noopener"
                    class="inline-flex min-h-[34px] items-center gap-1.5 rounded-[9px] border border-line bg-surface px-3 text-[0.82rem] font-bold text-foreground transition-colors hover:border-line-strong"
                  >
                    <span
                      class="material-symbols-outlined !text-base text-primary"
                      aria-hidden="true"
                      >location_on</span
                    >
                    {{ formatAddress(order.address) }}
                  </a>
                  <a
                    v-if="whatsappUrl(order.customerPhone)"
                    :href="whatsappUrl(order.customerPhone, wspMessage(order))"
                    target="_blank"
                    rel="noopener"
                    class="inline-flex min-h-[34px] items-center gap-1.5 rounded-[9px] border border-success/40 bg-surface px-3 text-[0.82rem] font-bold text-success transition-colors hover:bg-success-soft"
                  >
                    <span class="material-symbols-outlined !text-base" aria-hidden="true"
                      >chat</span
                    >
                    {{ order.customerPhone }}
                  </a>
                </div>
                <p v-if="order.address?.notes" class="px-1 text-[0.78rem] text-muted">
                  {{ order.address.notes }}
                </p>

                <!-- Avance rápido: la acción más usada -->
                <button
                  v-if="orderAdvance(order)"
                  type="button"
                  class="flex h-[46px] w-full items-center justify-center gap-2 rounded-xl bg-primary text-[0.95rem] font-extrabold text-primary-foreground transition hover:opacity-90 disabled:opacity-40"
                  :disabled="updatingId === order.id"
                  @click="changeStatus(order, orderAdvance(order).next.id)"
                >
                  <span
                    v-if="updatingId === order.id"
                    class="material-symbols-outlined animate-spin !text-xl"
                    aria-hidden="true"
                    >progress_activity</span
                  >
                  <span v-else class="material-symbols-outlined !text-xl" aria-hidden="true"
                    >arrow_forward</span
                  >
                  {{ orderAdvance(order).label }}
                </button>

                <!-- Cambio manual a cualquier estado -->
                <div class="flex flex-wrap gap-1.5">
                  <button
                    v-for="s in statusButtons"
                    :key="s.id"
                    type="button"
                    :disabled="s.id === order.statusId || updatingId === order.id"
                    :class="[
                      'inline-flex h-[34px] items-center gap-1 rounded-[9px] border px-3 text-[0.8rem] font-bold transition-colors disabled:opacity-40',
                      s.id === order.statusId
                        ? 'border-primary/40 bg-primary-soft text-primary !opacity-100'
                        : s.id === ORDER_STATUS.ENTREGADO
                          ? 'ml-auto border-success/40 text-success hover:bg-success-soft'
                          : 'border-line bg-surface text-muted hover:bg-surface-2 hover:text-foreground',
                    ]"
                    @click="changeStatus(order, s.id)"
                  >
                    <span
                      v-if="s.id === ORDER_STATUS.ENTREGADO"
                      class="material-symbols-outlined !text-sm"
                      aria-hidden="true"
                      >check</span
                    >
                    {{ s.label }}
                  </button>
                  <button
                    type="button"
                    class="inline-flex h-[34px] items-center gap-1 rounded-[9px] border border-line px-3 text-[0.8rem] font-bold text-muted transition-colors hover:bg-surface-2 hover:text-foreground disabled:opacity-40"
                    :disabled="updatingId === order.id"
                    @click="router.push(`/pedidos/${order.id}/editar`)"
                  >
                    <span class="material-symbols-outlined !text-sm" aria-hidden="true">edit</span>
                    Editar
                  </button>
                  <button
                    type="button"
                    class="inline-flex h-[34px] items-center rounded-[9px] border border-line px-3 text-[0.8rem] font-bold text-muted transition-colors hover:border-danger/40 hover:text-danger disabled:opacity-40"
                    :disabled="updatingId === order.id"
                    @click="cancelTarget = order"
                  >
                    Cancelar
                  </button>
                </div>
              </div>
            </li>
          </ul>
        </template>
      </div>

      <!-- Entregados del turno -->
      <template v-if="deliveredToday.length > 0">
        <div class="section-label mt-5">
          <span>Entregados esta noche</span>
          <button
            type="button"
            class="text-[0.75rem] font-semibold normal-case tracking-normal transition-colors hover:text-foreground"
            @click="router.push('/pedidos')"
          >
            Ver todos →
          </button>
        </div>
        <div class="list-panel">
          <div
            v-for="order in deliveredToday.slice().reverse().slice(0, 8)"
            :key="order.id"
            class="list-row"
          >
            <div class="min-w-0">
              <div class="list-row-title">{{ order.customerName }}</div>
              <div class="list-row-meta">
                #{{ order.id }} · {{ formatDateTime(order.createdAt) }}
              </div>
            </div>
            <span class="text-[0.95rem] font-extrabold text-primary">{{
              formatMoney(order.total)
            }}</span>
          </div>
        </div>
      </template>
    </div>

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
