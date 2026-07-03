import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getApiErrorMessage } from '../services/apiErrors'
import {
  createOrder,
  getOrders,
  updateOrder as updateOrderRequest,
  updateOrderStatus as updateOrderStatusRequest,
} from '../services/ordersService'
import { orderStatusMeta } from '../constants/status'

export const useOrdersStore = defineStore('orders', () => {
  const items = ref([])
  const isLoading = ref(false)
  const isSaving = ref(false)
  const error = ref(null)
  const operationError = ref(null)

  const totalCount = ref(0)
  const currentPage = ref(1)
  const totalPages = ref(1)
  const hasMore = ref(false)

  let lastParams = {}

  async function fetchOrders(params = {}) {
    lastParams = params
    isLoading.value = true
    error.value = null

    try {
      const data = await getOrders({ pageSize: 100, ...params })
      items.value = data.items
      totalCount.value = data.totalCount
      currentPage.value = data.page
      totalPages.value = data.totalPages
      hasMore.value = data.page < data.totalPages
    } catch (err) {
      console.error('Error fetching orders:', err)
      error.value = getApiErrorMessage(err, 'No se pudieron cargar los pedidos.')
    } finally {
      isLoading.value = false
    }
  }

  // Cambio de estado optimista: actualiza al toque y revierte si el backend falla.
  async function updateOrderStatus(id, statusId) {
    operationError.value = null
    const order = items.value.find((o) => o.id === id)
    if (!order) return

    const original = { statusId: order.statusId, status: order.status }
    order.statusId = statusId
    order.status = orderStatusMeta(statusId).value

    try {
      await updateOrderStatusRequest(id, statusId)
    } catch (err) {
      console.error('Error updating order status:', err)
      Object.assign(order, original)
      operationError.value = getApiErrorMessage(err, 'No se pudo actualizar el estado del pedido.')
      throw err
    }
  }

  async function addOrder(payload) {
    isSaving.value = true
    operationError.value = null

    try {
      const orderId = await createOrder(payload)
      await fetchOrders(lastParams)
      return orderId
    } catch (err) {
      console.error('Error creating order:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo crear el pedido.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function editOrder(id, payload) {
    isSaving.value = true
    operationError.value = null

    try {
      await updateOrderRequest(id, payload)
      await fetchOrders(lastParams)
    } catch (err) {
      console.error('Error updating order:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo actualizar el pedido.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  return {
    items,
    isLoading,
    isSaving,
    error,
    operationError,
    totalCount,
    currentPage,
    totalPages,
    hasMore,
    fetchOrders,
    updateOrderStatus,
    addOrder,
    editOrder,
  }
})
