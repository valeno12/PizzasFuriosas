import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getApiErrorMessage } from '../services/apiErrors'
import {
  createPurchase,
  deletePurchase,
  getPurchases,
  updatePurchase,
} from '../services/purchasesService'

function mapPurchase(p) {
  return {
    id: p.id,
    date: p.date,
    description: p.description,
    amount: p.amount,
    category: p.category,
  }
}

export const useExpensesStore = defineStore('expenses', () => {
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

  async function fetchExpenses(options = {}) {
    const {
      page = 1,
      pageSize = 50,
      category = null,
      from = null,
      to = null,
      append = false,
    } = options
    if (!append) {
      isLoading.value = true
      error.value = null
    }
    try {
      const params = { page, pageSize }
      if (category) params.category = category
      if (from) params.from = from
      if (to) params.to = to
      const data = await getPurchases(params)
      const mapped = data.items.map(mapPurchase)
      items.value = append ? [...items.value, ...mapped] : mapped
      totalCount.value = data.totalCount
      currentPage.value = data.page
      totalPages.value = data.totalPages
      hasMore.value = data.page < data.totalPages
    } catch (err) {
      error.value = getApiErrorMessage(err, 'No se pudieron cargar los gastos.')
    } finally {
      isLoading.value = false
    }
  }

  async function addExpense(payload) {
    isSaving.value = true
    operationError.value = null
    try {
      const purchase = await createPurchase(payload)
      items.value.unshift(mapPurchase(purchase))
      totalCount.value += 1
      return purchase
    } catch (err) {
      operationError.value = getApiErrorMessage(err, 'No se pudo registrar el gasto.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function editExpense(id, payload) {
    isSaving.value = true
    operationError.value = null
    try {
      const purchase = await updatePurchase(id, payload)
      const index = items.value.findIndex((item) => item.id === id)
      if (index !== -1) items.value[index] = mapPurchase(purchase)
      return purchase
    } catch (err) {
      operationError.value = getApiErrorMessage(err, 'No se pudo actualizar el gasto.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function removeExpense(id) {
    isDeleting.value = true
    operationError.value = null
    const index = items.value.findIndex((item) => item.id === id)
    const backup = items.value[index]
    if (index !== -1) items.value.splice(index, 1)
    try {
      await deletePurchase(id)
      totalCount.value = Math.max(0, totalCount.value - 1)
    } catch (err) {
      if (backup) items.value.splice(index, 0, backup)
      operationError.value = getApiErrorMessage(err, 'No se pudo eliminar el gasto.')
      throw err
    } finally {
      isDeleting.value = false
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
    fetchExpenses,
    addExpense,
    editExpense,
    removeExpense,
  }
})
