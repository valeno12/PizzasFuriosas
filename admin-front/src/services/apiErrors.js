export function getApiErrorMessage(error, fallback = 'Ocurrió un error inesperado.') {
  const response = error?.response?.data

  if (response?.errors) {
    const firstError = Object.values(response.errors).flat()[0]
    if (firstError) return firstError
  }

  return response?.message || error?.message || fallback
}
