import { defineStore } from 'pinia'
import { ref } from 'vue'

let nextId = 1

export const useToastsStore = defineStore('toasts', () => {
  const items = ref([])

  function push(message, variant = 'info', options = {}) {
    if (!message) return null

    const toast = {
      id: nextId++,
      message,
      variant,
      title: options.title || variant,
      duration: options.duration ?? 3600,
    }

    items.value.push(toast)

    if (toast.duration > 0) {
      window.setTimeout(() => remove(toast.id), toast.duration)
    }

    return toast.id
  }

  function success(message, options) {
    return push(message, 'success', options)
  }

  function error(message, options) {
    return push(message, 'error', options)
  }

  function warning(message, options) {
    return push(message, 'warning', options)
  }

  function info(message, options) {
    return push(message, 'info', options)
  }

  function remove(id) {
    items.value = items.value.filter((item) => item.id !== id)
  }

  return { items, push, success, error, warning, info, remove }
})
