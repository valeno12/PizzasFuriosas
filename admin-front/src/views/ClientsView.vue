<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '../components/shared/PageHeader.vue'
import UiButton from '../components/ui/UiButton.vue'
import SearchInput from '../components/ui/SearchInput.vue'
import EntityAvatar from '../components/ui/EntityAvatar.vue'
import EmptyState from '../components/ui/EmptyState.vue'
import ErrorState from '../components/ui/ErrorState.vue'
import SkeletonBlock from '../components/ui/SkeletonBlock.vue'
import AppDrawer from '../components/ui/AppDrawer.vue'
import ConfirmDialog from '../components/ui/ConfirmDialog.vue'
import ClientEditorDrawer from '../components/clients/ClientEditorDrawer.vue'
import AddressEditorDrawer from '../components/clients/AddressEditorDrawer.vue'
import StatusBadge from '../components/ui/StatusBadge.vue'
import { useClientsStore } from '../stores/clients'
import { useToastsStore } from '../stores/toasts'
import { useDebounce } from '../composables/useDebounce'
import StatCard from '../components/ui/StatCard.vue'
import { getOrders } from '../services/ordersService'
import { getCustomerStats } from '../services/customersService'
import { formatDateTime, formatMoney } from '../utils/formatters'
import { whatsappUrl } from '../utils/links'

const route = useRoute()
const router = useRouter()
const clientsStore = useClientsStore()
const toasts = useToastsStore()
const { debounce } = useDebounce()

const searchTerm = ref('')
const selectedClientId = ref(null)
const detailOpen = ref(false)
const clientDrawerOpen = ref(false)
const addressDrawerOpen = ref(false)
const clientToEdit = ref(null)
const clientToDelete = ref(null)

const selectedClient = computed(
  () => clientsStore.items.find((client) => client.id === selectedClientId.value) || null,
)
const selectedClientAddresses = computed(() => selectedClient.value?.addresses || [])

onMounted(() => {
  loadClients()
  if (route.path.endsWith('/nuevo')) openCreate()
})

watch(
  () => route.path,
  (path) => {
    if (path.endsWith('/nuevo')) openCreate()
  },
)

function loadClients(options = {}) {
  return clientsStore.fetchClients({
    page: options.page || 1,
    pageSize: 24,
    search: searchTerm.value.trim(),
    append: options.append || false,
  })
}

function handleSearch(value) {
  searchTerm.value = value
  debounce(loadClients, 200)
}

// Historial completo del cliente seleccionado, paginado.
const ORDERS_PAGE_SIZE = 10
const clientOrders = ref([])
const isLoadingOrders = ref(false)
const ordersPage = ref(1)
const hasMoreOrders = ref(false)

// Resumen: total de pedidos, gastado y producto favorito.
const clientStats = ref(null)

async function fetchClientOrders(customerId, { page = 1, append = false } = {}) {
  isLoadingOrders.value = true
  if (!append) clientOrders.value = []
  try {
    const data = await getOrders({ customerId, page, pageSize: ORDERS_PAGE_SIZE })
    clientOrders.value = append ? [...clientOrders.value, ...data.items] : data.items
    ordersPage.value = data.page
    hasMoreOrders.value = data.page < data.totalPages
  } catch (err) {
    console.error('Error fetching client orders:', err)
  } finally {
    isLoadingOrders.value = false
  }
}

function loadMoreOrders() {
  if (!selectedClient.value || isLoadingOrders.value) return
  fetchClientOrders(selectedClient.value.id, { page: ordersPage.value + 1, append: true })
}

async function fetchClientStats(customerId) {
  clientStats.value = null
  try {
    clientStats.value = await getCustomerStats(customerId)
  } catch (err) {
    console.error('Error fetching client stats:', err)
  }
}

function orderSummary(order) {
  return order.items.map((i) => `${i.quantity}× ${i.name}`).join(', ')
}

// En desktop el detalle vive en el panel lateral; el drawer es solo para mobile.
const isMobile = () => window.matchMedia('(max-width: 1023px)').matches

async function openDetail(client) {
  selectedClientId.value = client.id
  detailOpen.value = isMobile()
  fetchClientOrders(client.id)
  fetchClientStats(client.id)
  await clientsStore.fetchAddresses(client.id)
}

function openCreate() {
  clientToEdit.value = null
  clientDrawerOpen.value = true
}

function openEdit(client = selectedClient.value) {
  if (!client) return
  clientToEdit.value = client
  clientDrawerOpen.value = true
}

function closeEditorDrawer() {
  clientDrawerOpen.value = false
  if (route.path.endsWith('/nuevo')) router.push('/admin/clientes')
}

async function saveClient(payload) {
  try {
    if (clientToEdit.value) {
      await clientsStore.editClient(clientToEdit.value.id, payload)
      toasts.success('Cliente actualizado.')
    } else {
      const created = await clientsStore.addClient(payload)
      selectedClientId.value = created.id
      toasts.success('Cliente creado.')
    }
    closeEditorDrawer()
  } catch {
    toasts.error(clientsStore.operationError || 'No se pudo guardar el cliente.')
  }
}

async function saveAddress(payload) {
  if (!selectedClient.value) return
  try {
    await clientsStore.addAddress(selectedClient.value.id, payload)
    addressDrawerOpen.value = false
    toasts.success('Dirección agregada.')
  } catch {
    toasts.error(clientsStore.operationError || 'No se pudo agregar la dirección.')
  }
}

async function deleteClient() {
  if (!clientToDelete.value) return
  try {
    await clientsStore.deleteClient(clientToDelete.value.id)
    if (selectedClientId.value === clientToDelete.value.id) selectedClientId.value = null
    detailOpen.value = false
    toasts.success('Cliente eliminado.')
    clientToDelete.value = null
  } catch {
    toasts.error(clientsStore.operationError || 'No se pudo eliminar el cliente.')
  }
}

function formatAddress(address) {
  return `${address.street} ${address.number}${address.apartment ? `, ${address.apartment}` : ''}`
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1200px]">
    <PageHeader title="Clientes" subtitle="Teléfonos, notas y direcciones">
      <template #right>
        <UiButton variant="primary" @click="openCreate">
          <span class="material-symbols-outlined !text-lg" aria-hidden="true">person_add</span>
          Cliente
        </UiButton>
      </template>
    </PageHeader>

    <div class="grid gap-4 px-4 pb-8 md:px-8 md:pb-10">
      <!-- Búsqueda + contador -->
      <div class="flex items-center gap-3">
        <div class="flex-1">
          <SearchInput
            :model-value="searchTerm"
            placeholder="Buscar nombre o teléfono"
            @update:model-value="handleSearch"
          />
        </div>
        <span class="shrink-0 text-sm text-muted"
          ><strong>{{ clientsStore.totalCount }}</strong> total</span
        >
      </div>

      <ErrorState v-if="clientsStore.error" :message="clientsStore.error" />

      <div v-else-if="clientsStore.isLoading && clientsStore.items.length === 0" class="grid gap-2">
        <SkeletonBlock v-for="i in 8" :key="i" height="64px" />
      </div>

      <EmptyState
        v-else-if="clientsStore.items.length === 0"
        title="No hay clientes aún"
        message="Creá uno nuevo o ajustá la búsqueda."
      >
        <template #actions>
          <UiButton variant="primary" @click="openCreate">Crear cliente</UiButton>
        </template>
      </EmptyState>

      <div v-else class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_360px] lg:items-start">
        <div class="list-panel">
          <button
            v-for="client in clientsStore.items"
            :key="client.id"
            type="button"
            :class="[
              'list-row transition-colors hover:bg-surface-2/50',
              selectedClient?.id === client.id ? 'bg-primary/10' : '',
            ]"
            @click="openDetail(client)"
          >
            <div class="flex min-w-0 items-center gap-3">
              <EntityAvatar :name="client.name" />
              <div class="min-w-0">
                <div class="list-row-title">{{ client.name }}</div>
                <div class="list-row-meta">
                  {{ client.phone || 'Sin teléfono' }}{{ client.notes ? ` · ${client.notes}` : '' }}
                </div>
              </div>
            </div>
            <span class="material-symbols-outlined !text-lg text-muted" aria-hidden="true"
              >chevron_right</span
            >
          </button>
        </div>

        <!-- Detalle desktop -->
        <EmptyState
          v-if="!selectedClient"
          class="hidden lg:sticky lg:top-5 lg:grid"
          :mascot="false"
          icon="person_search"
          title="Seleccioná un cliente"
          message="Detalles y direcciones aparecerán aquí."
        />
        <aside v-else class="hidden gap-4 lg:sticky lg:top-5 lg:grid">
          <div class="flex items-center gap-3.5">
            <EntityAvatar :name="selectedClient.name" size="lg" />
            <div class="min-w-0">
              <h2 class="truncate text-xl font-extrabold">{{ selectedClient.name }}</h2>
              <a
                v-if="whatsappUrl(selectedClient.phone)"
                :href="whatsappUrl(selectedClient.phone)"
                target="_blank"
                rel="noopener"
                class="mt-1 inline-flex items-center gap-1.5 rounded-[9px] border border-success/40 px-2.5 py-1 text-[0.82rem] font-bold text-success transition-colors hover:bg-success-soft"
              >
                <span class="material-symbols-outlined !text-base" aria-hidden="true">chat</span>
                {{ selectedClient.phone }}
              </a>
              <p v-else class="text-muted">Sin teléfono</p>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-2">
            <UiButton variant="secondary" @click="openEdit()">Editar</UiButton>
            <UiButton variant="secondary" @click="addressDrawerOpen = true">Dirección</UiButton>
          </div>

          <div>
            <div class="section-label"><span>Direcciones</span></div>
            <div v-if="clientsStore.isLoadingAddresses" class="grid gap-2">
              <SkeletonBlock v-for="i in 2" :key="i" height="56px" />
            </div>
            <p v-else-if="selectedClientAddresses.length === 0" class="py-2 text-sm text-muted">
              Sin direcciones guardadas.
            </p>
            <div v-else class="list-panel">
              <div v-for="address in selectedClientAddresses" :key="address.id" class="list-row">
                <span class="min-w-0">
                  <strong class="block truncate text-[0.9rem]">{{ formatAddress(address) }}</strong>
                  <span v-if="address.notes" class="list-row-meta">{{ address.notes }}</span>
                </span>
              </div>
            </div>
          </div>

          <div v-if="selectedClient.notes">
            <div class="section-label"><span>Notas</span></div>
            <p
              class="rounded-[10px] border border-line-soft bg-surface-2 px-3 py-2.5 text-[0.88rem] text-muted"
            >
              {{ selectedClient.notes }}
            </p>
          </div>

          <div v-if="clientStats" class="grid grid-cols-3 gap-2">
            <StatCard label="Pedidos" :value="clientStats.totalOrders" />
            <StatCard label="Gastado" :value="formatMoney(clientStats.totalSpent)" tone="accent" />
            <StatCard label="Favorito" :value="clientStats.favoriteProduct || '—'" />
          </div>

          <div>
            <div class="section-label"><span>Historial de pedidos</span></div>
            <div v-if="isLoadingOrders && clientOrders.length === 0" class="grid gap-2">
              <SkeletonBlock v-for="i in 2" :key="i" height="56px" />
            </div>
            <p v-else-if="clientOrders.length === 0" class="py-2 text-sm text-muted">
              Todavía no hizo pedidos.
            </p>
            <template v-else>
              <div class="list-panel">
                <div v-for="order in clientOrders" :key="order.id" class="list-row">
                  <div class="min-w-0">
                    <div class="mb-0.5 flex items-center gap-2">
                      <span class="font-mono text-[0.72rem] text-muted">#{{ order.id }}</span>
                      <span class="text-[0.72rem] text-muted">{{
                        formatDateTime(order.createdAt)
                      }}</span>
                      <StatusBadge :status-id="order.statusId" />
                    </div>
                    <div class="list-row-meta">{{ orderSummary(order) }}</div>
                  </div>
                  <span class="shrink-0 font-extrabold text-primary">{{
                    formatMoney(order.total)
                  }}</span>
                </div>
              </div>
              <div v-if="hasMoreOrders" class="mt-2 flex justify-center">
                <UiButton
                  variant="secondary"
                  size="sm"
                  :disabled="isLoadingOrders"
                  @click="loadMoreOrders"
                >
                  Cargar más
                </UiButton>
              </div>
            </template>
          </div>

          <UiButton variant="ghost" class="text-danger" @click="clientToDelete = selectedClient"
            >Eliminar cliente</UiButton
          >
        </aside>
      </div>

      <div v-if="clientsStore.hasMore" class="flex justify-center">
        <UiButton
          variant="secondary"
          :disabled="clientsStore.isLoading"
          @click="loadClients({ page: clientsStore.currentPage + 1, append: true })"
        >
          Cargar más
        </UiButton>
      </div>
    </div>

    <!-- Drawer detalle mobile -->
    <AppDrawer
      :open="detailOpen && Boolean(selectedClient)"
      :title="selectedClient?.name || 'Cliente'"
      :description="selectedClient?.phone || 'Sin teléfono'"
      @close="detailOpen = false"
    >
      <div v-if="selectedClient" class="grid gap-5">
        <a
          v-if="whatsappUrl(selectedClient.phone)"
          :href="whatsappUrl(selectedClient.phone)"
          target="_blank"
          rel="noopener"
          class="inline-flex w-fit items-center gap-1.5 rounded-[9px] border border-success/40 px-2.5 py-1.5 text-[0.85rem] font-bold text-success transition-colors hover:bg-success-soft"
        >
          <span class="material-symbols-outlined !text-base" aria-hidden="true">chat</span>
          {{ selectedClient.phone }}
        </a>
        <div class="grid grid-cols-2 gap-2">
          <UiButton variant="secondary" @click="openEdit()">Editar</UiButton>
          <UiButton variant="secondary" @click="addressDrawerOpen = true">Dirección</UiButton>
        </div>
        <section>
          <h3 class="text-base font-bold">Direcciones</h3>
          <div v-if="clientsStore.isLoadingAddresses" class="mt-2 grid gap-2">
            <SkeletonBlock v-for="i in 2" :key="i" height="56px" />
          </div>
          <p
            v-else-if="selectedClientAddresses.length === 0"
            class="mt-2 rounded-[10px] border border-line-soft bg-surface-2 px-3 py-2.5 text-[0.88rem] text-muted"
          >
            Sin direcciones guardadas.
          </p>
          <div v-else class="list-panel mt-2">
            <div v-for="address in selectedClientAddresses" :key="address.id" class="list-row">
              <span class="min-w-0">
                <strong class="block truncate">{{ formatAddress(address) }}</strong>
                <span v-if="address.notes" class="list-row-meta">{{ address.notes }}</span>
              </span>
            </div>
          </div>
        </section>
        <section v-if="selectedClient.notes">
          <h3 class="text-base font-bold">Notas</h3>
          <p
            class="mt-2 rounded-[10px] border border-line-soft bg-surface-2 px-3 py-2.5 text-[0.88rem] text-muted"
          >
            {{ selectedClient.notes }}
          </p>
        </section>
        <div v-if="clientStats" class="grid grid-cols-3 gap-2">
          <StatCard label="Pedidos" :value="clientStats.totalOrders" />
          <StatCard label="Gastado" :value="formatMoney(clientStats.totalSpent)" tone="accent" />
          <StatCard label="Favorito" :value="clientStats.favoriteProduct || '—'" />
        </div>
        <section>
          <h3 class="text-base font-bold">Historial de pedidos</h3>
          <div v-if="isLoadingOrders && clientOrders.length === 0" class="mt-2 grid gap-2">
            <SkeletonBlock v-for="i in 2" :key="i" height="56px" />
          </div>
          <p v-else-if="clientOrders.length === 0" class="mt-2 text-sm text-muted">
            Todavía no hizo pedidos.
          </p>
          <template v-else>
            <div class="list-panel mt-2">
              <div v-for="order in clientOrders" :key="order.id" class="list-row">
                <div class="min-w-0">
                  <div class="mb-0.5 flex items-center gap-2">
                    <span class="font-mono text-[0.72rem] text-muted">#{{ order.id }}</span>
                    <span class="text-[0.72rem] text-muted">{{
                      formatDateTime(order.createdAt)
                    }}</span>
                    <StatusBadge :status-id="order.statusId" />
                  </div>
                  <div class="list-row-meta">{{ orderSummary(order) }}</div>
                </div>
                <span class="shrink-0 font-extrabold text-primary">{{
                  formatMoney(order.total)
                }}</span>
              </div>
            </div>
            <div v-if="hasMoreOrders" class="mt-2 flex justify-center">
              <UiButton
                variant="secondary"
                size="sm"
                :disabled="isLoadingOrders"
                @click="loadMoreOrders"
              >
                Cargar más
              </UiButton>
            </div>
          </template>
        </section>
        <UiButton variant="ghost" class="text-danger" @click="clientToDelete = selectedClient"
          >Eliminar cliente</UiButton
        >
      </div>
    </AppDrawer>

    <ClientEditorDrawer
      :open="clientDrawerOpen"
      :client="clientToEdit"
      :is-saving="clientsStore.isSaving"
      :error-message="clientsStore.operationError"
      @close="closeEditorDrawer"
      @save="saveClient"
    />
    <AddressEditorDrawer
      :open="addressDrawerOpen"
      :customer-name="selectedClient?.name"
      :is-saving="clientsStore.isSaving"
      :error-message="clientsStore.operationError"
      @close="addressDrawerOpen = false"
      @save="saveAddress"
    />
    <ConfirmDialog
      :open="Boolean(clientToDelete)"
      title="Eliminar cliente"
      :message="clientToDelete ? `Se eliminará ${clientToDelete.name}.` : ''"
      confirm-label="Eliminar"
      :loading="clientsStore.isDeleting"
      @close="clientToDelete = null"
      @confirm="deleteClient"
    />
  </div>
</template>
