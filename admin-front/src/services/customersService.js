import api from './api'

export async function getCustomers(params = {}) {
  const response = await api.get('/customers', { params })
  return response.data.data
}

export async function createCustomer(payload) {
  const response = await api.post('/customers', payload)
  return response.data.data
}

export async function updateCustomer(id, payload) {
  const response = await api.put(`/customers/${id}`, payload)
  return response.data.data
}

export async function deleteCustomer(id) {
  await api.delete(`/customers/${id}`)
}

export async function getCustomerStats(id) {
  const response = await api.get(`/customers/${id}/stats`)
  return response.data.data
}

export async function getCustomerAddresses(id) {
  const response = await api.get(`/customers/${id}/addresses`)
  return response.data.data
}

export async function createCustomerAddress(id, payload) {
  const response = await api.post(`/customers/${id}/addresses`, payload)
  return response.data.data
}
