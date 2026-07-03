// Estados de pedido. El backend los identifica por ID (seed fijo de OrderStatuses):
// el ID es la fuente de verdad; `value` es solo la representación interna del front.
export const ORDER_STATUS = {
  PENDIENTE: 1,
  EN_PREPARACION: 2,
  LISTO: 3,
  EN_CAMINO: 4,
  ENTREGADO: 5,
  CANCELADO: 6,
}

export const ORDER_STATUSES = [
  {
    id: 1,
    value: 'PENDIENTE',
    label: 'Pendiente',
    tone: 'warning',
    nextId: 2,
    action: 'Enviar a cocina',
  },
  {
    id: 2,
    value: 'EN PREPARACIÓN',
    label: 'En preparación',
    tone: 'info',
    nextId: 3,
    action: 'Marcar listo',
  },
  { id: 3, value: 'LISTO', label: 'Listo', tone: 'success', nextId: 4, action: 'Despachar' },
  {
    id: 4,
    value: 'EN CAMINO',
    label: 'En camino',
    tone: 'brand',
    nextId: 5,
    action: 'Marcar entregado',
  },
  { id: 5, value: 'ENTREGADO', label: 'Entregado', tone: 'success' },
  { id: 6, value: 'CANCELADO', label: 'Cancelado', tone: 'danger' },
]

export const ORDER_STATUS_BY_ID = Object.fromEntries(
  ORDER_STATUSES.map((status) => [status.id, status]),
)

export const FINAL_STATUS_IDS = [ORDER_STATUS.ENTREGADO, ORDER_STATUS.CANCELADO]

// Prioridad en la cola activa: lo listo para salir va primero.
const ACTIVE_PRIORITY = {
  [ORDER_STATUS.LISTO]: 1,
  [ORDER_STATUS.EN_PREPARACION]: 2,
  [ORDER_STATUS.PENDIENTE]: 3,
  [ORDER_STATUS.EN_CAMINO]: 4,
}

export function orderStatusMeta(statusId) {
  return (
    ORDER_STATUS_BY_ID[statusId] || {
      id: statusId,
      value: '',
      label: 'Sin estado',
      tone: 'neutral',
    }
  )
}

export function nextOrderStatus(statusId) {
  const nextId = ORDER_STATUS_BY_ID[statusId]?.nextId
  return nextId ? ORDER_STATUS_BY_ID[nextId] : null
}

// Avance rápido según el tipo de pedido: un Take Away no pasa por "En Camino",
// de Listo se entrega directo en mostrador.
export function orderAdvance(order) {
  const isTakeAway = order.shippingMethod === 'Take Away'

  if (isTakeAway && order.statusId === ORDER_STATUS.LISTO) {
    return { next: ORDER_STATUS_BY_ID[ORDER_STATUS.ENTREGADO], label: 'Entregar' }
  }

  const next = nextOrderStatus(order.statusId)
  if (!next) return null
  return { next, label: ORDER_STATUS_BY_ID[order.statusId].action }
}

export function orderStatusPriority(statusId) {
  return ACTIVE_PRIORITY[statusId] ?? 99
}

export function isFinalStatus(statusId) {
  return FINAL_STATUS_IDS.includes(statusId)
}

// Estados de evento (el backend los calcula y devuelve como texto).
const EVENT_STATUS_META = {
  Próximo: { label: 'Próximo', tone: 'info' },
  'Pendiente de cierre': { label: 'Pendiente de cierre', tone: 'warning' },
  Completado: { label: 'Completado', tone: 'success' },
  Cancelado: { label: 'Cancelado', tone: 'danger' },
}

export function eventStatusMeta(status) {
  return EVENT_STATUS_META[status] || { label: status || 'Sin estado', tone: 'neutral' }
}
