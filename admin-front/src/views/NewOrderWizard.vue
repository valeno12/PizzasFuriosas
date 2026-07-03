<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import FormShell from '../components/shared/FormShell.vue'
import UiButton from '../components/ui/UiButton.vue'
import UiInput from '../components/ui/UiInput.vue'
import UiTextarea from '../components/ui/UiTextarea.vue'
import SearchInput from '../components/ui/SearchInput.vue'
import EntityAvatar from '../components/ui/EntityAvatar.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import { useClientsStore } from '../stores/clients'
import { useOrdersStore } from '../stores/orders'
import { useToastsStore } from '../stores/toasts'
import { getProducts } from '../services/productsService'
import { getOrder, getOrders } from '../services/ordersService'
import { isFinalStatus } from '../constants/status'
import { formatMoney, toTimeInputValue } from '../utils/formatters'
import { useDebounce } from '../composables/useDebounce'

const route = useRoute()
const router = useRouter()
const clientsStore = useClientsStore()
const ordersStore = useOrdersStore()
const toasts = useToastsStore()
const { debounce, clear } = useDebounce()

// Modo edición: la misma vista sirve para /pedidos/nuevo y /pedidos/:id/editar.
const editingId = route.params.id ? Number(route.params.id) : null
const isEditing = editingId !== null
const editingOrder = ref(null)
const isLoadingOrder = ref(false)

const STEPS = [
  { id: 1, label: 'Cliente' },
  { id: 2, label: 'Entrega' },
  { id: 3, label: 'Productos' },
  { id: 4, label: 'Confirmación' },
]

const currentStep = ref(1)
const formError = ref('')

// Paso 1: cliente
const customerMode = ref('existing')
const customerSearch = ref('')
const selectedCustomerId = ref(null)
const newCustomer = ref({ name: '', phone: '' })

// Paso 2: entrega
const delivery = ref({ shippingMethod: 'Take Away', paymentMethod: 'Efectivo', deliveryCost: 0 })
const scheduledTime = ref('') // HH:MM local; vacío = lo antes posible
const orderNotes = ref('')
const addresses = ref([])
const isLoadingAddresses = ref(false)
const selectedAddressId = ref(null)
const addressMode = ref('existing')
const newAddress = ref({ street: '', number: '', apartment: '', notes: '' })
const paymentOptions = ['Efectivo', 'Transferencia']

// Paso 3: productos. El carrito guarda el producto completo para que la
// búsqueda (que reemplaza el listado) no borre lo ya seleccionado.
const products = ref([])
const isLoadingProducts = ref(false)
const productSearch = ref('')
const cart = ref({}) // id -> { product, quantity }

async function fetchProducts() {
  isLoadingProducts.value = true
  try {
    const params = { pageSize: 100, isAvailable: true }
    const search = productSearch.value.trim()
    if (search) params.search = search
    const data = await getProducts(params)
    products.value = data.items
  } catch (err) {
    console.error('Error fetching products:', err)
    formError.value = 'No se pudieron cargar los productos.'
  } finally {
    isLoadingProducts.value = false
  }
}

function handleProductSearch(value) {
  productSearch.value = value
  debounce(fetchProducts, 300)
}

// Prefill completo del wizard con un pedido existente.
async function loadOrderForEditing() {
  isLoadingOrder.value = true
  try {
    const [order] = await Promise.all([getOrder(editingId), fetchProducts()])

    if (isFinalStatus(order.statusId)) {
      toasts.error('Un pedido entregado o cancelado no se puede editar.')
      router.push('/pedidos')
      return
    }

    editingOrder.value = order
    customerMode.value = 'existing'
    selectedCustomerId.value = order.customerId
    delivery.value = {
      shippingMethod: order.shippingMethod,
      paymentMethod: order.paymentMethod,
      deliveryCost: order.deliveryCost,
    }
    scheduledTime.value = toTimeInputValue(order.scheduledFor)
    orderNotes.value = order.notes || ''

    cart.value = Object.fromEntries(
      order.items.map((item) => [
        item.productId,
        {
          // Si el producto sigue existiendo se usa el actual; si no, uno mínimo con el precio pagado.
          product: products.value.find((p) => p.id === item.productId) || {
            id: item.productId,
            name: item.name,
            price: item.unitPrice,
            imageUrl: null,
          },
          quantity: item.quantity,
        },
      ]),
    )

    addresses.value = await clientsStore.fetchAddresses(order.customerId)
    if (order.address) {
      addressMode.value = 'existing'
      selectedAddressId.value = order.address.id
    } else {
      addressMode.value = addresses.value.length > 0 ? 'existing' : 'new'
    }

    currentStep.value = 2
  } catch (err) {
    console.error('Error loading order:', err)
    toasts.error('No se pudo cargar el pedido a editar.')
    router.push('/pedidos')
  } finally {
    isLoadingOrder.value = false
  }
}

onMounted(() => {
  if (isEditing) {
    loadOrderForEditing()
  } else {
    clientsStore.fetchClients({ pageSize: 16 })
    fetchProducts()
  }
})

onBeforeUnmount(clear)

// Al cambiar de paso se vuelve arriba (después de un listado largo quedabas al fondo).
watch(currentStep, () => window.scrollTo({ top: 0, behavior: 'instant' }))

const selectedCustomer = computed(
  () => clientsStore.items.find((c) => c.id === selectedCustomerId.value) || null,
)
const selectedAddress = computed(
  () => addresses.value.find((a) => a.id === selectedAddressId.value) || null,
)
const stepTitle = computed(() => STEPS.find((s) => s.id === currentStep.value)?.label || '')

const productGroups = computed(() => {
  const groups = new Map()
  for (const product of products.value) {
    const category = product.categoryName || 'General'
    if (!groups.has(category)) groups.set(category, [])
    groups.get(category).push(product)
  }
  return Array.from(groups, ([category, items]) => ({ category, products: items }))
})

const selectedProducts = computed(() =>
  Object.values(cart.value).filter((entry) => entry.quantity > 0),
)
const itemsTotal = computed(() =>
  selectedProducts.value.reduce((sum, e) => sum + Number(e.product.price) * e.quantity, 0),
)
const selectedItemsCount = computed(() =>
  selectedProducts.value.reduce((sum, e) => sum + e.quantity, 0),
)
const orderTotal = computed(
  () =>
    itemsTotal.value +
    (delivery.value.shippingMethod === 'Delivery' ? Number(delivery.value.deliveryCost || 0) : 0),
)
const customerNameForReview = computed(() => {
  if (isEditing) return editingOrder.value?.customerName
  return customerMode.value === 'existing' ? selectedCustomer.value?.name : newCustomer.value.name
})

const canGoNext = computed(() => {
  if (currentStep.value === 1) {
    if (isEditing) return true
    return customerMode.value === 'existing'
      ? Boolean(selectedCustomerId.value)
      : Boolean(newCustomer.value.name.trim() && newCustomer.value.phone.trim())
  }
  if (currentStep.value === 2) {
    if (delivery.value.shippingMethod !== 'Delivery') return true
    return addressMode.value === 'existing'
      ? Boolean(selectedAddressId.value)
      : Boolean(newAddress.value.street.trim() && newAddress.value.number.trim())
  }
  if (currentStep.value === 3) return selectedProducts.value.length > 0
  return true
})

const primaryLabel = computed(() => {
  if (currentStep.value === 4) return isEditing ? 'Guardar cambios' : 'Confirmar pedido'
  if (currentStep.value === 3) return 'Revisar'
  return 'Continuar'
})

const exitTarget = isEditing ? '/pedidos' : '/'

function setCustomerMode(mode) {
  customerMode.value = mode
  formError.value = ''
  if (mode === 'new') {
    selectedCustomerId.value = null
    selectedAddressId.value = null
    addresses.value = []
    addressMode.value = 'new'
  }
}

function handleCustomerSearch(value) {
  customerSearch.value = value
  debounce(
    () => clientsStore.fetchClients({ pageSize: 16, search: customerSearch.value.trim() }),
    200,
  )
}

// Último pedido del cliente elegido, para ofrecer "repetir".
const lastOrder = ref(null)

async function fetchLastOrder(customerId) {
  lastOrder.value = null
  try {
    const data = await getOrders({ customerId, pageSize: 1 })
    lastOrder.value = data.items[0] || null
  } catch (err) {
    console.error('Error fetching last order:', err)
  }
}

function lastOrderSummary() {
  if (!lastOrder.value) return ''
  return lastOrder.value.items.map((i) => `${i.quantity}× ${i.name}`).join(', ')
}

// Rellena el carrito y la entrega con el último pedido. Los productos que ya
// no existen o están agotados se saltean avisando.
function repeatLastOrder() {
  const order = lastOrder.value
  if (!order) return

  const entries = []
  const skipped = []
  for (const item of order.items) {
    const product = products.value.find((p) => p.id === item.productId)
    if (product) entries.push([product.id, { product, quantity: item.quantity }])
    else skipped.push(item.name)
  }

  cart.value = Object.fromEntries(entries)
  delivery.value.shippingMethod = order.shippingMethod
  delivery.value.paymentMethod = order.paymentMethod
  if (order.shippingMethod === 'Delivery') {
    delivery.value.deliveryCost = order.deliveryCost
    if (order.address && addresses.value.some((a) => a.id === order.address.id)) {
      addressMode.value = 'existing'
      selectedAddressId.value = order.address.id
    }
  }

  if (skipped.length > 0) {
    toasts.warning(`Sin disponibilidad: ${skipped.join(', ')}. El resto se cargó.`)
  } else {
    toasts.success('Último pedido cargado. Revisá y confirmá.')
  }
}

async function selectCustomer(customer) {
  selectedCustomerId.value = customer.id
  selectedAddressId.value = null
  addresses.value = []
  isLoadingAddresses.value = true
  formError.value = ''
  fetchLastOrder(customer.id)
  try {
    addresses.value = await clientsStore.fetchAddresses(customer.id)
    addressMode.value = addresses.value.length > 0 ? 'existing' : 'new'
  } finally {
    isLoadingAddresses.value = false
    if (currentStep.value === 1) currentStep.value = 2
  }
}

function formatAddress(address) {
  if (!address) return ''
  return `${address.street} ${address.number}${address.apartment ? `, ${address.apartment}` : ''}`
}

function useNewAddress() {
  addressMode.value = 'new'
  selectedAddressId.value = null
}

function setShippingMethod(method) {
  delivery.value.shippingMethod = method
  if (method === 'Take Away') selectedAddressId.value = null
}

function setQuantity(product, quantity) {
  const parsed = Math.max(0, Number(quantity || 0))
  const next = { ...cart.value }
  if (parsed === 0) delete next[product.id]
  else next[product.id] = { product, quantity: parsed }
  cart.value = next
}

// Evita perder un pedido a medio armar por un toque de más en "Cancelar" o "Atrás".
const confirmExit = ref(false)

const hasProgress = computed(() => {
  if (isEditing) return false // el pedido ya existe, no se pierde nada
  return (
    selectedProducts.value.length > 0 ||
    Boolean(selectedCustomerId.value) ||
    Boolean(newCustomer.value.name.trim())
  )
})

function requestExit() {
  if (hasProgress.value) confirmExit.value = true
  else router.push(exitTarget)
}

function handleBack() {
  formError.value = ''
  const firstStep = isEditing ? 2 : 1
  if (currentStep.value <= firstStep) requestExit()
  else currentStep.value -= 1
}

function goToStep(stepId) {
  if (stepId <= currentStep.value) {
    formError.value = ''
    currentStep.value = stepId
  }
}

function nextStep() {
  formError.value = ''
  if (!canGoNext.value) {
    formError.value = 'Faltan datos para avanzar.'
    return
  }
  currentStep.value = Math.min(4, currentStep.value + 1)
}

// Combina el HH:MM elegido con la fecha de hoy (los pedidos son siempre del turno actual).
function scheduledForIso() {
  if (!scheduledTime.value) return null
  const [hours, minutes] = scheduledTime.value.split(':').map(Number)
  const date = new Date()
  date.setHours(hours, minutes, 0, 0)
  return date.toISOString()
}

function buildPayload() {
  const isExistingCustomer = customerMode.value === 'existing'
  const isDelivery = delivery.value.shippingMethod === 'Delivery'
  const useExistingAddress = isDelivery && addressMode.value === 'existing'
  return {
    customerId: isExistingCustomer ? selectedCustomerId.value : null,
    customerName: isExistingCustomer ? null : newCustomer.value.name.trim(),
    customerPhone: isExistingCustomer ? null : newCustomer.value.phone.trim(),
    addressId: useExistingAddress ? selectedAddressId.value : null,
    newAddress:
      isDelivery && !useExistingAddress
        ? {
            street: newAddress.value.street.trim(),
            number: newAddress.value.number.trim(),
            apartment: newAddress.value.apartment.trim() || null,
            notes: newAddress.value.notes.trim() || null,
          }
        : null,
    shippingMethod: delivery.value.shippingMethod,
    deliveryCost: isDelivery ? Number(delivery.value.deliveryCost || 0) : 0,
    paymentMethod: delivery.value.paymentMethod,
    notes: orderNotes.value.trim() || null,
    scheduledFor: scheduledForIso(),
    items: selectedProducts.value.map((entry) => ({
      productId: entry.product.id,
      quantity: entry.quantity,
    })),
  }
}

async function submitOrder() {
  formError.value = ''
  if (!canGoNext.value || selectedProducts.value.length === 0) {
    formError.value = 'Revisá el pedido antes de confirmar.'
    return
  }
  try {
    if (isEditing) {
      await ordersStore.editOrder(editingId, buildPayload())
      toasts.success(`Pedido #${editingId} actualizado.`)
    } else {
      await ordersStore.addOrder(buildPayload())
      toasts.success('Pedido creado.')
    }
    router.push(exitTarget)
  } catch {
    toasts.error(ordersStore.operationError || 'No se pudo guardar el pedido.')
  }
}

function handlePrimary() {
  if (currentStep.value === 4) submitOrder()
  else nextStep()
}

const choiceButtonClass = (active) => [
  'flex min-h-20 flex-col items-center justify-center gap-2 rounded-[14px] border-2 text-[0.9rem] font-bold transition-colors',
  active ? 'border-primary bg-primary-soft text-primary' : 'border-line bg-surface text-muted',
]

const modeTabClass = (active) => [
  'h-9 whitespace-nowrap rounded-[9px] px-3.5 text-sm font-bold transition-colors',
  active ? 'bg-primary text-primary-foreground' : 'text-muted hover:text-foreground',
]
</script>

<template>
  <FormShell
    :title="stepTitle"
    :subtitle="isEditing ? `Editando pedido #${editingId}` : ''"
    :step="currentStep"
    :total-steps="4"
    :can-submit="canGoNext"
    :primary-label="primaryLabel"
    :is-loading="ordersStore.isSaving"
    :total-amount="currentStep >= 2 ? formatMoney(orderTotal) : null"
    @back="handleBack"
    @cancel="requestExit"
    @primary="handlePrimary"
  >
    <!-- Indicador de pasos -->
    <div class="no-scrollbar flex items-center overflow-x-auto px-4 pt-3">
      <template v-for="(step, idx) in STEPS" :key="step.id">
        <button
          type="button"
          :disabled="step.id > currentStep"
          class="flex shrink-0 items-center gap-2"
          @click="goToStep(step.id)"
        >
          <span
            :class="[
              'grid h-6 w-6 place-items-center rounded-full text-[0.7rem] font-extrabold transition-colors',
              currentStep === step.id
                ? 'bg-primary text-primary-foreground'
                : step.id < currentStep
                  ? 'bg-primary-soft text-primary ring-2 ring-primary/30'
                  : 'bg-surface-2 text-muted',
            ]"
          >
            <span
              v-if="step.id < currentStep"
              class="material-symbols-outlined !text-[13px] leading-none"
              aria-hidden="true"
              >check</span
            >
            <span v-else>{{ step.id }}</span>
          </span>
          <span
            :class="[
              'text-[0.8rem] font-bold',
              currentStep === step.id ? 'text-foreground' : 'text-muted',
            ]"
          >
            {{ step.label }}
          </span>
        </button>
        <span v-if="idx < STEPS.length - 1" class="mx-1 h-px w-5 shrink-0 bg-line-soft"></span>
      </template>
    </div>

    <!-- Error global -->
    <div
      v-if="formError"
      class="mx-4 mt-2.5 rounded-xl border border-danger/30 bg-danger-soft px-3 py-2.5 text-sm font-bold text-danger"
    >
      {{ formError }}
    </div>

    <!-- Cargando el pedido a editar -->
    <div v-if="isLoadingOrder" class="grid gap-2 px-4 pt-4 sm:px-6">
      <SkeletonBlock v-for="i in 4" :key="i" height="64px" />
    </div>

    <div v-else class="mt-3 overflow-hidden">
      <Transition name="fade-slide" mode="out-in">
        <!-- PASO 1: Cliente -->
        <section v-if="currentStep === 1" class="grid gap-3.5 px-4 sm:px-6">
          <!-- En edición el cliente está fijo -->
          <template v-if="isEditing">
            <div
              class="flex items-center gap-3 rounded-xl border border-line bg-surface px-3 py-2.5"
            >
              <EntityAvatar :name="editingOrder?.customerName" />
              <div class="min-w-0">
                <div class="text-[0.95rem] font-bold">{{ editingOrder?.customerName }}</div>
                <div class="text-xs text-muted">
                  {{ editingOrder?.customerPhone || 'Sin teléfono' }}
                </div>
              </div>
            </div>
            <p class="text-sm text-muted">
              El cliente no se puede cambiar desde acá. Si el pedido es de otra persona, cancelalo y
              cargá uno nuevo.
            </p>
          </template>

          <div
            v-if="!isEditing"
            class="inline-flex w-fit gap-0.5 rounded-xl border border-line-soft bg-surface p-[3px]"
          >
            <button
              type="button"
              :class="modeTabClass(customerMode === 'existing')"
              @click="setCustomerMode('existing')"
            >
              Existente
            </button>
            <button
              type="button"
              :class="modeTabClass(customerMode === 'new')"
              @click="setCustomerMode('new')"
            >
              Nuevo
            </button>
          </div>

          <template v-if="!isEditing && customerMode === 'existing'">
            <SearchInput
              :model-value="customerSearch"
              placeholder="Buscar cliente o teléfono"
              @update:model-value="handleCustomerSearch"
            />

            <div v-if="clientsStore.isLoading" class="grid gap-2">
              <SkeletonBlock v-for="i in 5" :key="i" height="56px" />
            </div>
            <div
              v-else-if="clientsStore.items.length === 0"
              class="py-5 text-center text-sm text-muted"
            >
              No hay clientes para esa búsqueda.
              <button
                type="button"
                class="mx-auto mt-2 block text-sm font-bold text-primary"
                @click="setCustomerMode('new')"
              >
                Crear cliente
              </button>
            </div>
            <div v-else class="list-panel">
              <button
                v-for="client in clientsStore.items"
                :key="client.id"
                type="button"
                :class="[
                  'list-row transition-colors hover:bg-surface-2/50',
                  selectedCustomerId === client.id ? 'bg-primary/10' : '',
                ]"
                @click="selectCustomer(client)"
              >
                <div class="flex min-w-0 items-center gap-2.5">
                  <EntityAvatar :name="client.name" />
                  <div class="min-w-0">
                    <div class="list-row-title">{{ client.name }}</div>
                    <div class="list-row-meta">{{ client.phone || 'Sin teléfono' }}</div>
                  </div>
                </div>
                <span
                  v-if="selectedCustomerId === client.id"
                  class="material-symbols-outlined !text-lg text-primary"
                  aria-hidden="true"
                  >check</span
                >
              </button>
            </div>
          </template>

          <template v-else-if="!isEditing">
            <div class="grid gap-4 sm:grid-cols-2">
              <UiInput v-model="newCustomer.name" label="Nombre" required />
              <UiInput
                v-model="newCustomer.phone"
                label="Teléfono"
                required
                type="tel"
                inputmode="numeric"
              />
            </div>
          </template>
        </section>

        <!-- PASO 2: Entrega -->
        <section v-else-if="currentStep === 2" class="grid gap-3.5 px-4 sm:px-6">
          <!-- Repetir último pedido del cliente -->
          <div
            v-if="!isEditing && lastOrder && selectedProducts.length === 0"
            class="flex items-center gap-3 rounded-xl border border-primary/25 bg-primary-soft px-3 py-2.5"
          >
            <span
              class="material-symbols-outlined shrink-0 !text-xl text-primary"
              aria-hidden="true"
              >history</span
            >
            <div class="min-w-0 flex-1">
              <div class="text-[0.85rem] font-bold">Último pedido</div>
              <div class="truncate text-[0.8rem] text-muted">
                {{ lastOrderSummary() }} · {{ formatMoney(lastOrder.total) }}
              </div>
            </div>
            <UiButton variant="primary" size="sm" @click="repeatLastOrder">Repetir</UiButton>
          </div>

          <div class="text-[0.7rem] font-bold uppercase tracking-[0.07em] text-muted">
            Método de entrega
          </div>
          <div class="grid grid-cols-2 gap-2">
            <button
              type="button"
              :class="choiceButtonClass(delivery.shippingMethod === 'Take Away')"
              @click="setShippingMethod('Take Away')"
            >
              <span class="material-symbols-outlined !text-[28px]" aria-hidden="true"
                >shopping_bag</span
              >
              Take away
            </button>
            <button
              type="button"
              :class="choiceButtonClass(delivery.shippingMethod === 'Delivery')"
              @click="setShippingMethod('Delivery')"
            >
              <span class="material-symbols-outlined !text-[28px]" aria-hidden="true"
                >pedal_bike</span
              >
              Delivery
            </button>
          </div>

          <div class="text-[0.7rem] font-bold uppercase tracking-[0.07em] text-muted">Pago</div>
          <div class="grid grid-cols-2 gap-2">
            <button
              v-for="option in paymentOptions"
              :key="option"
              type="button"
              :class="choiceButtonClass(delivery.paymentMethod === option)"
              @click="delivery.paymentMethod = option"
            >
              <span class="material-symbols-outlined !text-[28px]" aria-hidden="true">
                {{ option === 'Efectivo' ? 'payments' : 'sync_alt' }}
              </span>
              {{ option }}
            </button>
          </div>

          <UiInput
            v-if="delivery.shippingMethod === 'Delivery'"
            v-model="delivery.deliveryCost"
            label="Costo de envío"
            type="number"
            inputmode="numeric"
            min="0"
          />

          <UiInput
            v-model="scheduledTime"
            label="Hora de entrega (opcional)"
            type="time"
            helper="Dejalo vacío si es para lo antes posible."
          />

          <section v-if="delivery.shippingMethod === 'Delivery'" class="grid gap-3">
            <div
              class="inline-flex w-fit gap-0.5 rounded-xl border border-line-soft bg-surface p-[3px]"
            >
              <button
                type="button"
                :disabled="addresses.length === 0"
                :class="modeTabClass(addressMode === 'existing')"
                @click="addressMode = 'existing'"
              >
                Guardada
              </button>
              <button
                type="button"
                :class="modeTabClass(addressMode === 'new')"
                @click="useNewAddress"
              >
                Nueva
              </button>
            </div>

            <SkeletonBlock v-if="isLoadingAddresses" height="56px" />
            <div v-else-if="addressMode === 'existing'" class="list-panel">
              <button
                v-for="address in addresses"
                :key="address.id"
                type="button"
                :class="[
                  'list-row transition-colors hover:bg-surface-2/50',
                  selectedAddressId === address.id ? 'bg-primary/10' : '',
                ]"
                @click="selectedAddressId = address.id"
              >
                <div class="min-w-0">
                  <div class="list-row-title">{{ formatAddress(address) }}</div>
                  <div v-if="address.notes" class="list-row-meta">{{ address.notes }}</div>
                </div>
                <span
                  v-if="selectedAddressId === address.id"
                  class="material-symbols-outlined !text-lg text-primary"
                  aria-hidden="true"
                  >check</span
                >
              </button>
              <div v-if="addresses.length === 0" class="py-5 text-center text-sm text-muted">
                Sin direcciones guardadas.
              </div>
            </div>

            <div v-else class="grid gap-4 sm:grid-cols-2">
              <UiInput v-model="newAddress.street" label="Calle" required />
              <UiInput v-model="newAddress.number" label="Número" required />
              <UiInput v-model="newAddress.apartment" label="Depto / piso" />
              <UiTextarea v-model="newAddress.notes" label="Notas" />
            </div>
          </section>
        </section>

        <!-- PASO 3: Productos -->
        <section v-else-if="currentStep === 3" class="grid gap-3.5 px-4 sm:px-6">
          <SearchInput
            :model-value="productSearch"
            placeholder="Buscar producto"
            @update:model-value="handleProductSearch"
          />

          <div
            v-if="selectedProducts.length > 0"
            class="flex items-center justify-between gap-2.5 rounded-xl border border-primary/25 bg-primary-soft px-3 py-2.5 text-sm font-bold text-muted"
          >
            <span>{{ selectedItemsCount }} producto{{ selectedItemsCount !== 1 ? 's' : '' }}</span>
            <strong class="text-primary">{{ formatMoney(itemsTotal) }}</strong>
          </div>

          <div v-if="isLoadingProducts" class="grid gap-2">
            <SkeletonBlock v-for="i in 8" :key="i" height="56px" />
          </div>
          <EmptyState
            v-else-if="products.length === 0"
            icon="restaurant_menu"
            title="Sin productos disponibles"
            message="Activá productos en el menú o probá otra búsqueda."
          />
          <div v-else class="grid">
            <template v-for="group in productGroups" :key="group.category">
              <div
                class="pb-1 pt-3 text-[0.68rem] font-extrabold uppercase tracking-[0.09em] text-muted"
              >
                {{ group.category }}
              </div>
              <article
                v-for="product in group.products"
                :key="product.id"
                class="grid grid-cols-[44px_minmax(0,1fr)_auto] items-center gap-3 border-b border-line-soft py-2.5 last:border-b-0"
              >
                <div
                  class="grid h-11 w-11 shrink-0 place-items-center overflow-hidden rounded-[10px] bg-surface-2 text-primary"
                  aria-hidden="true"
                >
                  <img
                    v-if="product.imageUrl"
                    :src="product.imageUrl"
                    :alt="product.name"
                    loading="lazy"
                    class="h-full w-full object-cover"
                  />
                  <span v-else class="material-symbols-outlined">local_pizza</span>
                </div>
                <div class="min-w-0">
                  <div class="truncate text-[0.9rem] font-bold">{{ product.name }}</div>
                  <div class="mt-0.5 text-[0.82rem] font-bold text-primary">
                    {{ formatMoney(product.price) }}
                  </div>
                </div>
                <div
                  v-if="cart[product.id]"
                  class="grid grid-cols-[36px_38px_36px] items-center overflow-hidden rounded-[10px] border border-line-strong bg-field"
                  aria-label="Cantidad"
                >
                  <button
                    type="button"
                    class="grid min-h-9 place-items-center font-extrabold"
                    @click="setQuantity(product, cart[product.id].quantity - 1)"
                  >
                    <span class="material-symbols-outlined !text-lg" aria-hidden="true"
                      >remove</span
                    >
                  </button>
                  <span class="text-center font-extrabold">{{ cart[product.id].quantity }}</span>
                  <button
                    type="button"
                    class="grid min-h-9 place-items-center font-extrabold"
                    @click="setQuantity(product, cart[product.id].quantity + 1)"
                  >
                    <span class="material-symbols-outlined !text-lg" aria-hidden="true">add</span>
                  </button>
                </div>
                <button
                  v-else
                  type="button"
                  class="grid min-h-9 min-w-[78px] place-items-center rounded-[10px] border border-primary/40 font-extrabold text-primary"
                  @click="setQuantity(product, 1)"
                >
                  Agregar
                </button>
              </article>
            </template>
          </div>
        </section>

        <!-- PASO 4: Confirmación -->
        <section v-else class="grid gap-3.5 px-4 sm:px-6">
          <div class="list-panel">
            <div class="list-row">
              <span class="text-muted">Cliente</span>
              <strong>{{ customerNameForReview }}</strong>
            </div>
            <div class="list-row">
              <span class="text-muted">Entrega</span>
              <strong>{{ delivery.shippingMethod }}</strong>
            </div>
            <div v-if="delivery.shippingMethod === 'Delivery'" class="list-row">
              <span class="text-muted">Dirección</span>
              <strong>{{
                addressMode === 'existing'
                  ? formatAddress(selectedAddress)
                  : `${newAddress.street} ${newAddress.number}`
              }}</strong>
            </div>
            <div class="list-row">
              <span class="text-muted">Pago</span>
              <strong>{{ delivery.paymentMethod }}</strong>
            </div>
            <div v-if="scheduledTime" class="list-row">
              <span class="text-muted">Hora</span>
              <strong class="text-warning">Para las {{ scheduledTime }}</strong>
            </div>
          </div>

          <UiTextarea
            v-model="orderNotes"
            label="Notas del pedido (opcional)"
            maxlength="500"
            rows="2"
            placeholder="Sin cebolla, tocar timbre, salir 21:30..."
          />

          <div class="pt-2 text-[0.68rem] font-extrabold uppercase tracking-[0.09em] text-muted">
            Productos
          </div>
          <div class="list-panel">
            <div v-for="entry in selectedProducts" :key="entry.product.id" class="list-row">
              <span>
                <strong>{{ entry.quantity }}x {{ entry.product.name }}</strong>
                <span class="list-row-meta">{{ formatMoney(entry.product.price) }} c/u</span>
              </span>
              <strong>{{ formatMoney(entry.product.price * entry.quantity) }}</strong>
            </div>
            <div v-if="delivery.shippingMethod === 'Delivery'" class="list-row">
              <span class="text-muted">Envío</span>
              <span>{{ formatMoney(delivery.deliveryCost) }}</span>
            </div>
          </div>
        </section>
      </Transition>
    </div>

    <ConfirmDialog
      :open="confirmExit"
      title="Descartar pedido"
      message="Tenés un pedido a medio armar. Si salís ahora, se pierde lo cargado."
      confirm-label="Descartar"
      cancel-label="Seguir cargando"
      @close="confirmExit = false"
      @confirm="router.push(exitTarget)"
    />
  </FormShell>
</template>
