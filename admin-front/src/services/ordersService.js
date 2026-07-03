import api from './api'
import { orderStatusMeta } from '../constants/status'

// Modelo de pedido que usa toda la app. El estado canónico es statusId;
// `status` (valor en mayúsculas) se deriva de él, nunca del texto de la API.
export function mapOrder(o) {
  return {
    id: o.id,
    customerId: o.customerId,
    customerName: o.customerName,
    customerPhone: o.customerPhone,
    shippingMethod: o.shippingMethod,
    paymentMethod: o.paymentMethod,
    deliveryCost: o.deliveryCost,
    statusId: o.statusId,
    status: orderStatusMeta(o.statusId).value,
    createdAt: o.createdAt,
    scheduledFor: o.scheduledFor,
    notes: o.notes,
    address: o.address,
    total: o.totalPrice,
    items: (o.items || []).map((item) => ({
      id: item.id,
      productId: item.productId,
      name: item.productName,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      subtotal: item.subtotal,
    })),
  }
}

export async function getOrders(params = {}) {
  const response = await api.get('/orders', { params })
  const data = response.data.data
  return { ...data, items: data.items.map(mapOrder) }
}

export async function getOrder(id) {
  const response = await api.get(`/orders/${id}`)
  return mapOrder(response.data.data)
}

export async function createOrder(payload) {
  const response = await api.post('/orders', payload)
  return response.data.data
}

export async function updateOrder(id, payload) {
  await api.put(`/orders/${id}`, payload)
}

export async function updateOrderStatus(id, statusId) {
  await api.put(`/orders/${id}/status`, { statusId })
}
