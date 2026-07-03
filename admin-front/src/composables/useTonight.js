import { computed, onMounted, onUnmounted, ref } from 'vue'
import { getOrders, updateOrderStatus as updateOrderStatusRequest } from '../services/ordersService'
import { getApiErrorMessage } from '../services/apiErrors'
import { ORDER_STATUS, isFinalStatus, orderStatusPriority } from '../constants/status'
import { todayRange } from '../utils/dates'
import { useToastsStore } from '../stores/toasts'

const REFRESH_INTERVAL_MS = 30_000

// Pantalla "Esta noche": pedidos activos (de cualquier día) + todo lo de hoy,
// con auto-refresh mientras la pestaña está visible.
export function useTonight() {
  const toasts = useToastsStore()

  const items = ref([])
  const isLoading = ref(false)
  const error = ref(null)
  const updatingId = ref(null)
  const oldOrderWarning = ref(false)

  let refreshInterval = null

  async function fetchTonightOrders() {
    if (isLoading.value) return
    isLoading.value = true
    error.value = null

    try {
      const { from, to } = todayRange()
      const [active, today] = await Promise.all([
        getOrders({ onlyActive: true, pageSize: 500 }),
        getOrders({ from, to, pageSize: 500 }),
      ])

      const todayStart = new Date(from).getTime()
      oldOrderWarning.value = active.items.some((o) => new Date(o.createdAt).getTime() < todayStart)

      const merged = new Map()
      today.items.forEach((o) => merged.set(o.id, o))
      active.items.forEach((o) => merged.set(o.id, o))
      items.value = Array.from(merged.values())
    } catch (err) {
      console.error('Error fetching tonight orders:', err)
      error.value = 'No se pudieron cargar los pedidos de esta noche.'
    } finally {
      isLoading.value = false
    }
  }

  function startRefresh() {
    if (refreshInterval) return
    refreshInterval = setInterval(() => {
      if (document.visibilityState === 'visible') fetchTonightOrders()
    }, REFRESH_INTERVAL_MS)
  }

  function stopRefresh() {
    clearInterval(refreshInterval)
    refreshInterval = null
  }

  function onVisibilityChange() {
    if (document.visibilityState === 'visible') {
      fetchTonightOrders()
      startRefresh()
    } else {
      stopRefresh()
    }
  }

  onMounted(() => {
    fetchTonightOrders()
    document.addEventListener('visibilitychange', onVisibilityChange)
    startRefresh()
  })

  onUnmounted(() => {
    stopRefresh()
    document.removeEventListener('visibilitychange', onVisibilityChange)
  })

  // Dentro de cada estado, manda la hora prometida; sin hora, el orden de llegada.
  const dispatchTime = (order) => new Date(order.scheduledFor ?? order.createdAt)

  const activeOrders = computed(() =>
    items.value
      .filter((o) => !isFinalStatus(o.statusId))
      .sort((a, b) => {
        const byPriority = orderStatusPriority(a.statusId) - orderStatusPriority(b.statusId)
        return byPriority !== 0 ? byPriority : dispatchTime(a) - dispatchTime(b)
      }),
  )

  const deliveredToday = computed(() =>
    items.value.filter((o) => o.statusId === ORDER_STATUS.ENTREGADO),
  )

  const deliveredRevenue = computed(() =>
    deliveredToday.value.reduce((sum, o) => sum + Number(o.total || 0), 0),
  )

  const readyCount = computed(
    () => items.value.filter((o) => o.statusId === ORDER_STATUS.LISTO).length,
  )

  async function updateStatus(orderId, statusId) {
    updatingId.value = orderId
    try {
      await updateOrderStatusRequest(orderId, statusId)

      const order = items.value.find((o) => o.id === orderId)
      if (order) order.statusId = statusId

      toasts.success(`Pedido #${orderId} actualizado`)
      return true
    } catch (err) {
      toasts.error(getApiErrorMessage(err, 'No se pudo actualizar el estado.'))
      return false
    } finally {
      updatingId.value = null
    }
  }

  return {
    items,
    isLoading,
    error,
    updatingId,
    oldOrderWarning,
    activeOrders,
    deliveredToday,
    deliveredRevenue,
    readyCount,
    fetchTonightOrders,
    updateStatus,
  }
}
