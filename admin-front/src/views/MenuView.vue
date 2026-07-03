<script setup>
import { computed, defineAsyncComponent, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '../components/shared/PageHeader.vue'
import UiButton from '../components/ui/UiButton.vue'
import StatCard from '../components/ui/StatCard.vue'
import TabBar from '../components/ui/TabBar.vue'
import SearchInput from '../components/ui/SearchInput.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ErrorState from '../components/ui/ErrorState.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import { useMenu } from '../composables/useMenu'
import { useToastsStore } from '../stores/toasts'
import { formatMoney } from '../utils/formatters'

const ProductEditorDrawer = defineAsyncComponent(
  () => import('../components/products/ProductEditorDrawer.vue'),
)

const route = useRoute()
const router = useRouter()
const toasts = useToastsStore()
const {
  items,
  isLoading,
  isDeleting,
  error,
  operationError,
  hasMore,
  categories,
  activeCategoryId,
  searchTerm,
  loadData,
  loadMoreItems,
  handleSearchInput,
  selectCategory,
  toggleAvailability,
  addProduct,
  updateProduct,
  deleteProduct,
  uploadProductImage,
  createCategory,
} = useMenu()

const drawerOpen = ref(false)
const productToEdit = ref(null)
const productToDelete = ref(null)
const togglingIds = ref(new Set())

onMounted(() => {
  loadData()
  if (route.path.endsWith('/nuevo')) openCreate()
})

watch(
  () => route.path,
  (path) => {
    if (path.endsWith('/nuevo')) openCreate()
  },
)

const availableCount = computed(() => items.value.filter((item) => item.isAvailable).length)
const unavailableCount = computed(() => Math.max(0, items.value.length - availableCount.value))
const categoryTabs = computed(() => [
  { value: null, label: 'Todos' },
  ...categories.value.map((c) => ({ value: c.id, label: c.name })),
])

function openCreate() {
  productToEdit.value = null
  drawerOpen.value = true
}

function openEdit(product) {
  productToEdit.value = product
  drawerOpen.value = true
}

function closeDrawer() {
  drawerOpen.value = false
  if (route.path.endsWith('/nuevo')) router.push('/admin/productos')
}

// Cubre TODO el guardado (alta/edición + subida de imagen a Cloudinary): el isSaving
// del store se apaga antes de subir la imagen y el botón quedaba clickeable de nuevo,
// permitiendo crear el producto varias veces.
const isSavingProduct = ref(false)

async function saveProduct(payload) {
  if (isSavingProduct.value) return
  isSavingProduct.value = true

  const { imageFile, ...productData } = payload
  try {
    const saved = productToEdit.value
      ? await updateProduct(productToEdit.value.id, productData)
      : await addProduct(productData)
    if (imageFile && saved?.id) await uploadProductImage(saved.id, imageFile)
    toasts.success(productToEdit.value ? 'Producto actualizado.' : 'Producto creado.')
    closeDrawer()
  } catch {
    toasts.error(operationError.value || 'No se pudo guardar el producto.')
  } finally {
    isSavingProduct.value = false
  }
}

async function confirmDelete() {
  if (!productToDelete.value) return
  try {
    await deleteProduct(productToDelete.value.id)
    toasts.success('Producto eliminado.')
    productToDelete.value = null
  } catch {
    toasts.error(operationError.value || 'No se pudo eliminar el producto.')
  }
}

async function handleToggle(id) {
  if (togglingIds.value.has(id)) return
  togglingIds.value = new Set([...togglingIds.value, id])
  try {
    await toggleAvailability(id)
  } catch {
    toasts.error(operationError.value || 'No se pudo cambiar la disponibilidad.')
  } finally {
    const next = new Set(togglingIds.value)
    next.delete(id)
    togglingIds.value = next
  }
}

async function createNewCategory(name) {
  try {
    await createCategory(name)
    toasts.success('Categoría creada.')
  } catch {
    toasts.error('No se pudo crear la categoría.')
  }
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1180px]">
    <PageHeader title="Productos" subtitle="Activá disponibilidad sin entrar a editar">
      <template #right>
        <UiButton variant="primary" @click="openCreate">
          <span class="material-symbols-outlined !text-lg" aria-hidden="true">add</span>
          Producto
        </UiButton>
      </template>
    </PageHeader>

    <div class="grid gap-4 px-4 pb-8 md:px-8 md:pb-10">
      <!-- Stats -->
      <div class="grid grid-cols-3 gap-2">
        <StatCard label="Disponibles" icon="check_circle" :value="availableCount" />
        <StatCard label="Agotados" icon="remove_circle" :value="unavailableCount" />
        <StatCard label="Categorías" icon="category" :value="categories.length" />
      </div>

      <!-- Toolbar: categorías + búsqueda -->
      <div class="flex flex-wrap items-center gap-3">
        <TabBar
          :tabs="categoryTabs"
          :model-value="activeCategoryId"
          @update:model-value="selectCategory"
        />
        <div class="min-w-[200px] max-w-[320px] flex-1">
          <SearchInput
            :model-value="searchTerm"
            placeholder="Buscar producto"
            @update:model-value="handleSearchInput"
          />
        </div>
      </div>

      <ErrorState v-if="error" :message="error" />

      <section
        v-else-if="isLoading && items.length === 0"
        class="grid grid-cols-2 gap-3 pb-4 sm:grid-cols-3 min-[980px]:grid-cols-5 min-[1220px]:grid-cols-6"
      >
        <SkeletonBlock v-for="i in 12" :key="i" height="184px" />
      </section>

      <EmptyState
        v-else-if="items.length === 0"
        title="No hay productos"
        message="Creá productos o ajustá la búsqueda."
      >
        <template #actions>
          <UiButton variant="primary" @click="openCreate">Crear producto</UiButton>
        </template>
      </EmptyState>

      <section
        v-else
        class="grid grid-cols-2 gap-3 pb-4 sm:grid-cols-3 min-[980px]:grid-cols-5 min-[1220px]:grid-cols-6"
        aria-label="Productos"
      >
        <article
          v-for="item in items"
          :key="item.id"
          class="overflow-hidden rounded-2xl border border-line bg-surface"
        >
          <div class="relative aspect-square overflow-hidden bg-surface-2">
            <img
              v-if="item.image"
              :src="item.image"
              :alt="item.name"
              loading="lazy"
              :class="[
                'h-full w-full object-cover transition',
                !item.isAvailable ? 'opacity-50 grayscale' : '',
              ]"
            />
            <span
              v-else
              class="grid h-full w-full place-items-center text-[42px] text-primary material-symbols-outlined !text-[42px]"
              aria-hidden="true"
              >local_pizza</span
            >

            <!-- Switch de disponibilidad -->
            <button
              type="button"
              :class="[
                'absolute right-2 top-2 h-[22px] w-[38px] rounded-full border p-[3px] transition-colors',
                item.isAvailable
                  ? 'border-transparent bg-primary'
                  : 'border-line-strong bg-background/80',
                togglingIds.has(item.id) ? 'cursor-wait opacity-50' : '',
              ]"
              :disabled="togglingIds.has(item.id)"
              :aria-pressed="item.isAvailable"
              :aria-label="item.isAvailable ? 'Marcar agotado' : 'Activar producto'"
              @click="handleToggle(item.id)"
            >
              <span
                v-if="togglingIds.has(item.id)"
                class="material-symbols-outlined block animate-spin !text-sm text-foreground"
                aria-hidden="true"
                >progress_activity</span
              >
              <span
                v-else
                :class="[
                  'block h-3.5 w-3.5 rounded-full transition-transform',
                  item.isAvailable ? 'translate-x-4 bg-primary-foreground' : 'bg-foreground',
                ]"
              ></span>
            </button>

            <span
              v-if="!item.isAvailable"
              class="absolute bottom-2 left-2 rounded-full bg-danger px-2 py-1 text-[0.62rem] font-black uppercase tracking-[0.08em] text-danger-foreground"
            >
              Agotado
            </span>
          </div>

          <button
            type="button"
            class="grid min-h-[74px] w-full grid-cols-[minmax(0,1fr)_auto] items-start gap-2 p-2.5 text-left"
            @click="openEdit(item)"
          >
            <span class="min-w-0">
              <strong class="block truncate text-[0.94rem] leading-tight">{{ item.name }}</strong>
              <small class="mt-0.5 block text-xs text-muted">{{ item.category }}</small>
              <span class="mt-0.5 block font-black text-primary">{{
                formatMoney(item.price)
              }}</span>
            </span>
            <span class="inline-flex items-center gap-1 text-[0.72rem] font-bold text-muted">
              <span class="material-symbols-outlined !text-[15px]" aria-hidden="true">edit</span>
              Editar
            </span>
          </button>
        </article>
      </section>

      <div
        v-if="isLoading && items.length > 0"
        class="grid grid-cols-2 gap-3 sm:grid-cols-3 min-[980px]:grid-cols-5"
      >
        <SkeletonBlock v-for="i in 4" :key="i" height="184px" />
      </div>
      <div v-else-if="hasMore" class="flex justify-center">
        <UiButton variant="secondary" @click="loadMoreItems">Cargar más</UiButton>
      </div>
    </div>

    <ProductEditorDrawer
      :open="drawerOpen"
      :product="productToEdit"
      :categories="categories"
      :is-saving="isSavingProduct"
      :error-message="operationError"
      @close="closeDrawer"
      @save="saveProduct"
      @create-category="createNewCategory"
    />
    <ConfirmDialog
      :open="Boolean(productToDelete)"
      title="Eliminar producto"
      :message="
        productToDelete ? `Se eliminará ${productToDelete.name}. No aparecerá en la carta.` : ''
      "
      confirm-label="Eliminar"
      :loading="isDeleting"
      @close="productToDelete = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
