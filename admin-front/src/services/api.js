import axios from 'axios'
import { useAuthStore } from '../stores/auth'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5054/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Adjunta el JWT a cada request.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Ante un 401 (token vencido o inválido) cierra sesión y vuelve al login.
// El router se importa dinámico para no crear un ciclo router → vistas → api → router.
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      useAuthStore().logout()
      const { default: router } = await import('../router')
      if (router.currentRoute.value.path !== '/login') {
        router.push('/login')
      }
    }
    return Promise.reject(error)
  },
)

export default api
