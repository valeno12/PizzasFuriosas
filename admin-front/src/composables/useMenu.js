import { computed, ref } from 'vue'
import { useMenuStore } from '../stores/menu'
import { useCategoriesStore } from '../stores/categories'
import { useDebounce } from './useDebounce'

// Orquesta el listado de productos con filtros de categoría y búsqueda server-side.
export function useMenu() {
  const store = useMenuStore()
  const categoriesStore = useCategoriesStore()
  const { debounce } = useDebounce()

  const activeCategoryId = ref(null) // null = todas
  const searchTerm = ref('')

  async function loadData() {
    await Promise.all([categoriesStore.fetchCategories(), fetchFilteredItems()])
  }

  function fetchFilteredItems() {
    return store.fetchItems({
      page: 1,
      search: searchTerm.value.trim(),
      categoryId: activeCategoryId.value,
    })
  }

  function loadMoreItems() {
    if (!store.hasMore || store.isLoading) return
    return store.fetchItems({
      page: store.currentPage + 1,
      search: searchTerm.value.trim(),
      categoryId: activeCategoryId.value,
      append: true,
    })
  }

  function handleSearchInput(value) {
    searchTerm.value = value
    debounce(fetchFilteredItems, 300)
  }

  function selectCategory(id) {
    activeCategoryId.value = id
    fetchFilteredItems()
  }

  return {
    items: computed(() => store.items),
    isLoading: computed(() => store.isLoading),
    isSaving: computed(() => store.isSaving),
    isDeleting: computed(() => store.isDeleting),
    error: computed(() => store.error),
    operationError: computed(() => store.operationError),
    hasMore: computed(() => store.hasMore),

    categories: computed(() => categoriesStore.items),
    activeCategoryId,
    searchTerm,

    loadData,
    loadMoreItems,
    handleSearchInput,
    selectCategory,

    toggleAvailability: store.toggleAvailability,
    addProduct: store.addProduct,
    updateProduct: store.updateProduct,
    deleteProduct: store.deleteProduct,
    uploadProductImage: store.uploadProductImage,
    createCategory: categoriesStore.createCategory,
  }
}
