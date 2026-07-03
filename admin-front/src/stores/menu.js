import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getApiErrorMessage } from '../services/apiErrors'
import {
  createProduct,
  deleteProduct as deleteProductRequest,
  getProducts,
  updateProduct as updateProductRequest,
  uploadProductImage as uploadProductImageRequest,
} from '../services/productsService'

function mapProduct(p) {
  return {
    id: p.id,
    name: p.name,
    category: p.categoryName || 'General',
    categoryId: p.categoryId,
    price: p.price,
    isAvailable: p.isAvailable,
    image: p.imageUrl || null,
  }
}

export const useMenuStore = defineStore('menu', () => {
  const items = ref([])
  const isLoading = ref(false)
  const isSaving = ref(false)
  const isDeleting = ref(false)
  const error = ref(null)
  const operationError = ref(null)

  const totalCount = ref(0)
  const currentPage = ref(1)
  const totalPages = ref(1)
  const hasMore = ref(false)

  async function fetchItems(options = {}) {
    const {
      page = 1,
      pageSize = 24,
      search = '',
      categoryId = null,
      isAvailable = null,
      append = false,
    } = options

    if (!append) {
      isLoading.value = true
      error.value = null
    }

    try {
      const params = { page, pageSize }
      if (search) params.search = search
      if (categoryId) params.categoryId = categoryId
      if (isAvailable !== null) params.isAvailable = isAvailable

      const data = await getProducts(params)
      const mapped = data.items.map(mapProduct)

      items.value = append ? [...items.value, ...mapped] : mapped
      totalCount.value = data.totalCount
      currentPage.value = data.page
      totalPages.value = data.totalPages
      hasMore.value = data.page < data.totalPages
    } catch (err) {
      console.error('Error fetching menu:', err)
      error.value = getApiErrorMessage(err, 'No se pudieron cargar los productos del menú.')
    } finally {
      isLoading.value = false
    }
  }

  // Cambio optimista: el switch responde al toque y revierte si el backend falla.
  async function toggleAvailability(id) {
    const item = items.value.find((i) => i.id === id)
    if (!item) return

    operationError.value = null
    const original = item.isAvailable
    item.isAvailable = !item.isAvailable

    try {
      const updated = await updateProductRequest(id, {
        name: item.name,
        price: item.price,
        categoryId: item.categoryId,
        isAvailable: item.isAvailable,
      })
      Object.assign(item, mapProduct(updated))
    } catch (err) {
      console.error('Error updating availability:', err)
      item.isAvailable = original
      operationError.value = getApiErrorMessage(err, 'No se pudo cambiar la disponibilidad.')
      throw err
    }
  }

  async function addProduct(payload) {
    operationError.value = null
    isSaving.value = true

    try {
      const product = await createProduct({
        name: payload.name,
        price: Number(payload.price),
        categoryId: Number(payload.categoryId),
        isAvailable: true,
      })
      items.value.unshift(mapProduct(product))
      totalCount.value += 1
      return product
    } catch (err) {
      console.error('Error adding product:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo crear el producto.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function updateProduct(id, payload) {
    operationError.value = null
    isSaving.value = true

    try {
      const product = await updateProductRequest(id, {
        name: payload.name,
        price: Number(payload.price),
        categoryId: Number(payload.categoryId),
        isAvailable: payload.isAvailable,
      })
      const index = items.value.findIndex((i) => i.id === id)
      if (index !== -1) items.value[index] = mapProduct(product)
      return product
    } catch (err) {
      console.error('Error updating product:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo actualizar el producto.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function deleteProduct(id) {
    operationError.value = null
    isDeleting.value = true

    const index = items.value.findIndex((i) => i.id === id)
    const backup = items.value[index]
    if (index !== -1) items.value.splice(index, 1)

    try {
      await deleteProductRequest(id)
      totalCount.value = Math.max(0, totalCount.value - 1)
    } catch (err) {
      console.error('Error deleting product:', err)
      if (backup) items.value.splice(index, 0, backup)
      operationError.value = getApiErrorMessage(err, 'No se pudo eliminar el producto.')
      throw err
    } finally {
      isDeleting.value = false
    }
  }

  async function uploadProductImage(id, file) {
    operationError.value = null

    try {
      const imageUrl = await uploadProductImageRequest(id, file)
      const item = items.value.find((i) => i.id === id)
      if (item) item.image = imageUrl
      return imageUrl
    } catch (err) {
      console.error('Error uploading image:', err)
      operationError.value = getApiErrorMessage(
        err,
        'El producto se guardó, pero no se pudo subir la imagen.',
      )
      throw err
    }
  }

  return {
    items,
    isLoading,
    isSaving,
    isDeleting,
    error,
    operationError,
    totalCount,
    currentPage,
    totalPages,
    hasMore,
    fetchItems,
    toggleAvailability,
    addProduct,
    updateProduct,
    deleteProduct,
    uploadProductImage,
  }
})
