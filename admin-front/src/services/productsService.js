import api from './api'

export async function getProducts(params = {}) {
  const response = await api.get('/products', { params })
  return response.data.data
}

export async function createProduct(payload) {
  const response = await api.post('/products', payload)
  return response.data.data
}

export async function updateProduct(id, payload) {
  const response = await api.put(`/products/${id}`, payload)
  return response.data.data
}

export async function deleteProduct(id) {
  await api.delete(`/products/${id}`)
}

export async function uploadProductImage(id, file) {
  const formData = new FormData()
  formData.append('file', file)

  const response = await api.post(`/products/${id}/image`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })

  return response.data.data
}
