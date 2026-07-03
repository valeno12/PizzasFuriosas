export function useDebounce() {
  let timeoutId = null

  /**
   * Ejecuta una función con un retraso, cancelando cualquier ejecución previa pendiente.
   * Útil para búsquedas por input.
   * @param {Function} fn Función a ejecutar
   * @param {number} delay Retraso en milisegundos (default: 300)
   */
  const debounce = (fn, delay = 300) => {
    if (timeoutId) {
      clearTimeout(timeoutId)
    }
    timeoutId = setTimeout(() => {
      fn()
    }, delay)
  }

  /**
   * Cancela el timeout actual si existe.
   */
  const clear = () => {
    if (timeoutId) {
      clearTimeout(timeoutId)
      timeoutId = null
    }
  }

  return { debounce, clear }
}
