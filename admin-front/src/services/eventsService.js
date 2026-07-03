import api from './api'

export async function getEvents(params = {}) {
  const response = await api.get('/events', { params })
  return response.data.data
}

export async function getEvent(id) {
  const response = await api.get(`/events/${id}`)
  return response.data.data
}

export async function createEvent(payload) {
  const response = await api.post('/events', payload)
  return response.data.data
}

export async function updateEvent(id, payload) {
  await api.put(`/events/${id}`, payload)
}

export async function completeEvent(id, payload) {
  await api.put(`/events/${id}/complete`, payload)
}

export async function cancelEvent(id) {
  await api.put(`/events/${id}/cancel`)
}

export async function addEventPayment(id, payload) {
  await api.post(`/events/${id}/payments`, payload)
}
