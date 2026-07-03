<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '../components/shared/PageHeader.vue'
import UiButton from '../components/ui/UiButton.vue'
import UiBadge from '../components/ui/UiBadge.vue'
import StatCard from '../components/ui/StatCard.vue'
import TabBar from '../components/ui/TabBar.vue'
import SearchInput from '../components/ui/SearchInput.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ErrorState from '../components/ui/ErrorState.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import ExpenseEditorDrawer from '../components/expenses/ExpenseEditorDrawer.vue'
import { useExpensesStore } from '../stores/expenses'
import { useToastsStore } from '../stores/toasts'
import { EXPENSE_CATEGORIES } from '../constants/expenses'
import { formatDate, formatMoney } from '../utils/formatters'
import { lastDaysRange, monthRange } from '../utils/dates'

const route = useRoute()
const router = useRouter()
const store = useExpensesStore()
const toasts = useToastsStore()

const activeCategory = ref(null) // null = todas
const activePeriod = ref('mes')
const searchTerm = ref('')
const drawerOpen = ref(false)
const expenseToEdit = ref(null)
const expenseToDelete = ref(null)

const categoryTabs = [
  { value: null, label: 'Todas' },
  ...EXPENSE_CATEGORIES.map((c) => ({ value: c, label: c })),
]

const PERIOD_TABS = [
  { value: 'mes', label: 'Este mes' },
  { value: '90d', label: '90 días' },
  { value: 'todo', label: 'Todo' },
]

function periodParams() {
  if (activePeriod.value === 'mes') return monthRange()
  if (activePeriod.value === '90d') return lastDaysRange(90)
  return {}
}

onMounted(() => {
  fetchExpenses()
  if (route.path.endsWith('/nuevo')) openCreate()
})

watch(
  () => route.path,
  (path) => {
    if (path.endsWith('/nuevo')) openCreate()
  },
)

function fetchExpenses(options = {}) {
  const { from = null, to = null } = periodParams()
  return store.fetchExpenses({
    page: options.page || 1,
    pageSize: 100,
    category: activeCategory.value,
    from,
    to,
    append: options.append || false,
  })
}

function selectCategory(category) {
  activeCategory.value = category
  fetchExpenses()
}

function selectPeriod(period) {
  activePeriod.value = period
  fetchExpenses()
}

// La búsqueda por texto filtra sobre lo ya cargado (la categoría filtra en el backend).
const visibleExpenses = computed(() => {
  const term = searchTerm.value.trim().toLowerCase()
  if (!term) return store.items
  return store.items.filter(
    (expense) =>
      expense.description.toLowerCase().includes(term) ||
      expense.category.toLowerCase().includes(term),
  )
})

const visibleTotal = computed(() =>
  visibleExpenses.value.reduce((sum, e) => sum + Number(e.amount || 0), 0),
)

function openCreate() {
  expenseToEdit.value = null
  drawerOpen.value = true
}

function openEdit(expense) {
  expenseToEdit.value = expense
  drawerOpen.value = true
}

function closeDrawer() {
  drawerOpen.value = false
  if (route.path.endsWith('/nuevo')) router.push('/admin/gastos')
}

async function saveExpense(payload) {
  try {
    if (expenseToEdit.value) await store.editExpense(expenseToEdit.value.id, payload)
    else await store.addExpense(payload)
    toasts.success(expenseToEdit.value ? 'Gasto actualizado.' : 'Gasto registrado.')
    closeDrawer()
  } catch {
    toasts.error(store.operationError || 'No se pudo guardar el gasto.')
  }
}

async function confirmDelete() {
  if (!expenseToDelete.value) return
  try {
    await store.removeExpense(expenseToDelete.value.id)
    toasts.success('Gasto eliminado.')
    expenseToDelete.value = null
  } catch {
    toasts.error(store.operationError || 'No se pudo eliminar el gasto.')
  }
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1200px]">
    <PageHeader title="Gastos" subtitle="Registrar compras y egresos del negocio">
      <template #right>
        <UiButton variant="primary" @click="openCreate">
          <span class="material-symbols-outlined !text-lg" aria-hidden="true">add</span>
          Gasto
        </UiButton>
      </template>
    </PageHeader>

    <div class="grid gap-4 px-4 pb-8 md:px-8 md:pb-10">
      <!-- Stats -->
      <div class="grid grid-cols-2 gap-2">
        <StatCard
          label="Total visible"
          icon="trending_down"
          :value="formatMoney(visibleTotal)"
          tone="danger"
        />
        <StatCard
          label="Registros"
          icon="receipt_long"
          :value="`${visibleExpenses.length} de ${store.totalCount}`"
        />
      </div>

      <!-- Filtros + búsqueda -->
      <div class="grid gap-3 sm:flex sm:flex-wrap sm:items-center">
        <TabBar
          :tabs="PERIOD_TABS"
          :model-value="activePeriod"
          @update:model-value="selectPeriod"
        />
        <TabBar
          :tabs="categoryTabs"
          :model-value="activeCategory"
          @update:model-value="selectCategory"
        />
        <div class="min-w-[180px] flex-1">
          <SearchInput v-model="searchTerm" placeholder="Buscar gasto" />
        </div>
      </div>

      <ErrorState v-if="store.error" :message="store.error" />

      <div v-else-if="store.isLoading && store.items.length === 0" class="grid gap-2">
        <SkeletonBlock v-for="i in 7" :key="i" height="64px" />
      </div>

      <EmptyState
        v-else-if="visibleExpenses.length === 0"
        title="No hay gastos cargados"
        message="El horno está tranquilo. Registrá un gasto cuando aparezca."
      >
        <template #actions>
          <UiButton variant="primary" @click="openCreate">Registrar gasto</UiButton>
        </template>
      </EmptyState>

      <!-- Tocar la fila abre el editor (mismo patrón que Clientes/Eventos) -->
      <div v-else class="list-panel">
        <button
          v-for="expense in visibleExpenses"
          :key="expense.id"
          type="button"
          class="list-row transition-colors hover:bg-surface-2/50"
          @click="openEdit(expense)"
        >
          <div class="min-w-0">
            <div class="mb-1 flex items-center gap-2">
              <UiBadge tone="neutral">{{ expense.category }}</UiBadge>
              <span class="font-mono text-[0.72rem] text-muted">{{
                formatDate(expense.date)
              }}</span>
            </div>
            <div class="list-row-title">{{ expense.description }}</div>
          </div>
          <div class="flex shrink-0 flex-col items-end gap-1">
            <strong class="text-base font-extrabold text-danger">{{
              formatMoney(expense.amount)
            }}</strong>
            <span
              class="text-[0.78rem] font-bold text-muted transition-colors hover:text-danger"
              role="button"
              @click.stop="expenseToDelete = expense"
            >
              Eliminar
            </span>
          </div>
        </button>
      </div>

      <div v-if="store.hasMore" class="flex justify-center">
        <UiButton
          variant="secondary"
          :disabled="store.isLoading"
          @click="fetchExpenses({ page: store.currentPage + 1, append: true })"
        >
          Cargar más
        </UiButton>
      </div>
    </div>

    <ExpenseEditorDrawer
      :open="drawerOpen"
      :expense="expenseToEdit"
      :is-saving="store.isSaving"
      :error-message="store.operationError"
      @close="closeDrawer"
      @save="saveExpense"
    />
    <ConfirmDialog
      :open="Boolean(expenseToDelete)"
      title="Eliminar gasto"
      :message="expenseToDelete ? `Se eliminará: ${expenseToDelete.description}.` : ''"
      confirm-label="Eliminar"
      :loading="store.isDeleting"
      @close="expenseToDelete = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
