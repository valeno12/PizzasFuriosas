import api from './api'

export async function getCategories() {
  const response = await api.get('/categories')
  return response.data.data
}

export async function createCategory(name) {
  const response = await api.post('/categories', { name })
  return response.data.data
}
