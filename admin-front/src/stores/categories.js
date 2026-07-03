import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getApiErrorMessage } from '../services/apiErrors'
import {
  createCategory as createCategoryRequest,
  getCategories,
} from '../services/categoriesService'

export const useCategoriesStore = defineStore('categories', () => {
  const items = ref([])
  const isLoading = ref(false)
  const error = ref(null)

  async function fetchCategories() {
    isLoading.value = true
    error.value = null
    try {
      items.value = await getCategories()
    } catch (err) {
      console.error('Error fetching categories:', err)
      error.value = getApiErrorMessage(err, 'No se pudieron cargar las categorías.')
    } finally {
      isLoading.value = false
    }
  }

  async function createCategory(name) {
    const category = await createCategoryRequest(name)
    items.value.push(category)
    return category
  }

  return { items, isLoading, error, fetchCategories, createCategory }
})
