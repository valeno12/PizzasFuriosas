import api from './api'

export async function getBalance(params = {}) {
  const response = await api.get('/dashboard/balance', { params })
  return response.data.data
}

export async function getStatistics(params = {}) {
  const response = await api.get('/dashboard/statistics', { params })
  return response.data.data
}
