import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getApiErrorMessage } from '../services/apiErrors'
import {
  addEventPayment,
  cancelEvent as cancelEventRequest,
  completeEvent as completeEventRequest,
  createEvent,
  getEvent,
  getEvents,
  updateEvent as updateEventRequest,
} from '../services/eventsService'

export const useEventsStore = defineStore('events', () => {
  const items = ref([])
  const selectedEvent = ref(null)
  const isLoading = ref(false)
  const isLoadingDetail = ref(false)
  const isSaving = ref(false)
  const error = ref(null)
  const operationError = ref(null)

  const totalCount = ref(0)
  const currentPage = ref(1)
  const totalPages = ref(1)
  const hasMore = ref(false)

  async function fetchEvents(options = {}) {
    const { page = 1, pageSize = 20, append = false } = options

    if (!append) {
      isLoading.value = true
      error.value = null
    }

    try {
      const data = await getEvents({ page, pageSize })
      items.value = append ? [...items.value, ...data.items] : data.items
      totalCount.value = data.totalCount
      currentPage.value = data.page
      totalPages.value = data.totalPages
      hasMore.value = data.page < data.totalPages
    } catch (err) {
      console.error('Error fetching events:', err)
      error.value = getApiErrorMessage(err, 'No se pudieron cargar los eventos.')
    } finally {
      isLoading.value = false
    }
  }

  async function fetchEvent(id) {
    isLoadingDetail.value = true
    operationError.value = null

    try {
      selectedEvent.value = await getEvent(id)
      return selectedEvent.value
    } catch (err) {
      console.error('Error fetching event:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo cargar el detalle del evento.')
      return null
    } finally {
      isLoadingDetail.value = false
    }
  }

  async function addEvent(payload) {
    operationError.value = null
    isSaving.value = true

    try {
      const id = await createEvent(payload)
      await fetchEvents()
      await fetchEvent(id)
      return id
    } catch (err) {
      console.error('Error creating event:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo crear el evento.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function editEvent(id, payload) {
    operationError.value = null
    isSaving.value = true

    try {
      await updateEventRequest(id, payload)
      await fetchEvents({ page: currentPage.value })
      await fetchEvent(id)
    } catch (err) {
      console.error('Error updating event:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo actualizar el evento.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function registerPayment(id, payload) {
    operationError.value = null
    isSaving.value = true

    try {
      await addEventPayment(id, payload)
      await fetchEvents({ page: currentPage.value })
      await fetchEvent(id)
    } catch (err) {
      console.error('Error adding event payment:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo registrar el pago.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function complete(id, payload) {
    operationError.value = null
    isSaving.value = true

    try {
      await completeEventRequest(id, payload)
      await fetchEvents({ page: currentPage.value })
      await fetchEvent(id)
    } catch (err) {
      console.error('Error completing event:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo cerrar el evento.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  async function cancel(id) {
    operationError.value = null
    isSaving.value = true

    try {
      await cancelEventRequest(id)
      await fetchEvents({ page: currentPage.value })
      await fetchEvent(id)
    } catch (err) {
      console.error('Error canceling event:', err)
      operationError.value = getApiErrorMessage(err, 'No se pudo cancelar el evento.')
      throw err
    } finally {
      isSaving.value = false
    }
  }

  return {
    items,
    selectedEvent,
    isLoading,
    isLoadingDetail,
    isSaving,
    error,
    operationError,

    totalCount,
    currentPage,
    totalPages,
    hasMore,

    fetchEvents,
    fetchEvent,
    addEvent,
    editEvent,
    registerPayment,
    complete,
    cancel,
  }
})
