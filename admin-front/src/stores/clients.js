import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getApiErrorMessage } from '../services/apiErrors'
import {
  createCustomer,
  createCustomerAddress,
  deleteCustomer as deleteCustomerRequest,
  getCustomerAddresses,
  getCustomers,
  updateCustomer,
} from '../services/customersService'

function mapCustomer(c) {
  return {
    id: c.id,
    name: c.name,
    phone: c.phone,
    notes: c.notes,
    addresses: [],
  }
}

function mapAddress(a) {
  return {
    id: a.id,
    customerId: a.customerId,
    street: a.street,
    number: a.number,
    apartment: a.apartment,
    notes: a.notes,
  }
}

export const useClientsStore = defineStore('clients', () => {
  const items = ref([])
  const isLoading = ref(false)
  const isSaving = ref(false)
  const isDeleting = ref(false)
  const isLoadingAddresses = ref(false)
  const error = ref(null)
  const operationError = ref(null)

  const totalCount = ref(0)
  const currentPage = ref(1)
  const totalPages = ref(1)
  const hasMore = ref(false)

  async function fetchClients(options = {}) {
    const { page = 1, pageSize = 20, search = '', append = false } = options

    if (!append) {
      isLoading.value = true
      error.value = null
    }

    try {
      const params = { page, pageSize }
      if (search) params.search = search

      const data = await getCustomers(params)
      const mapped = data.items.map(mapCustomer)
      items.value = append ? [...items.value, ...mapped] : mapped
      totalCount.value = data.totalCount
      currentPage.value = data.page
      totalPages.value = data.totalPages
      hasMore.value = data.page < data.totalPages
    } catch (err) {
      console.error('Error fetching clients:', err)
      error.value = getApiErrorMessage(err, 'No se pudieron cargar los clientes.')
    } finally {
      isLoading.value = false
    }
  }

  async function fetchAddresses(customerId) {
    isLoadingAddresses.value = true
    operationError.value = null

    try {
      const addresses = (await getCustomerAddresses(customerId)).map(mapAddress)
      const customer = items.value.find((c) => c.id === customerId)
      if (customer) customer.addresses = addresses
      return addresses
    } catch (err) {
      console.error('Error fetching addresses:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudieron cargar las direcciones.')
      return []
    } finally {
      isLoadingAddresses.value = false
    }
  }

  async function addClient(payload) {
    operationError.value = null
    isSaving.value = true

    try {
      const customer = await createCustomer({
        name: payload.name,
        phone: payload.phone || null,
        notes: payload.notes || null,
      })
      items.value.unshift(mapCustomer(customer))
      totalCount.value += 1
      return customer
    } catch (err) {
      console.error('Error creating client:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo crear el cliente.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function editClient(id, payload) {
    operationError.value = null
    isSaving.value = true

    try {
      const customer = await updateCustomer(id, {
        name: payload.name,
        phone: payload.phone || null,
        notes: payload.notes || null,
      })
      const index = items.value.findIndex((c) => c.id === id)
      if (index !== -1) {
        items.value[index] = {
          ...items.value[index],
          ...mapCustomer(customer),
          addresses: items.value[index].addresses,
        }
      }
      return customer
    } catch (err) {
      console.error('Error updating client:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo actualizar el cliente.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function deleteClient(id) {
    operationError.value = null
    isDeleting.value = true

    const index = items.value.findIndex((c) => c.id === id)
    const backup = items.value[index]
    if (index !== -1) items.value.splice(index, 1)

    try {
      await deleteCustomerRequest(id)
      totalCount.value = Math.max(0, totalCount.value - 1)
    } catch (err) {
      console.error('Error deleting client:', err)
      if (backup) items.value.splice(index, 0, backup)
      operationError.value = getApiErrorMessage(err, 'No se pudo eliminar el cliente.')
      throw err
    } finally {
      isDeleting.value = false
    }
  }

  async function addAddress(customerId, payload) {
    operationError.value = null
    isSaving.value = true

    try {
      const address = await createCustomerAddress(customerId, {
        street: payload.street,
        number: payload.number,
        apartment: payload.apartment || null,
        notes: payload.notes || null,
      })
      const customer = items.value.find((c) => c.id === customerId)
      if (customer) customer.addresses.push(mapAddress(address))
      return address
    } catch (err) {
      console.error('Error creating address:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo agregar la dirección.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  return {
    items,
    isLoading,
    isSaving,
    isDeleting,
    isLoadingAddresses,
    error,
    operationError,
    totalCount,
    currentPage,
    totalPages,
    hasMore,
    fetchClients,
    fetchAddresses,
    addClient,
    editClient,
    deleteClient,
    addAddress,
  }
})
