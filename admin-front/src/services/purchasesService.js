import api from './api'

export async function getPurchases(params = {}) {
  const response = await api.get('/purchases', { params })
  return response.data.data
}

export async function createPurchase(payload) {
  const response = await api.post('/purchases', payload)
  return response.data.data
}

export async function updatePurchase(id, payload) {
  const response = await api.put(`/purchases/${id}`, payload)
  return response.data.data
}

export async function deletePurchase(id) {
  await api.delete(`/purchases/${id}`)
}
