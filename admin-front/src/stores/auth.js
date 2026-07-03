import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../services/api'
import { getApiErrorMessage } from '../services/apiErrors'

const TOKEN_KEY = 'token'

// Decodifica el payload de un JWT sin librerías externas.
function decodeJwt(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((char) => '%' + ('00' + char.charCodeAt(0).toString(16)).slice(-2))
        .join(''),
    )
    return JSON.parse(json)
  } catch {
    return null
  }
}

function isExpired(payload) {
  return typeof payload.exp === 'number' && payload.exp * 1000 <= Date.now()
}

export const useAuthStore = defineStore('auth', () => {
  const isLoggedIn = ref(false)
  const user = ref(null)

  function hydrate() {
    const token = localStorage.getItem(TOKEN_KEY)
    if (!token) return

    const payload = decodeJwt(token)
    if (!payload || isExpired(payload)) {
      logout()
      return
    }

    // El backend emite claims con URIs estándar de .NET; el fallback corto
    // cubre tokens generados con nombres simples.
    user.value = {
      id:
        payload.nameid ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
      name:
        payload.unique_name ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
      email:
        payload.email ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
      role: payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
    }
    isLoggedIn.value = true
  }

  hydrate()

  async function login(email, password) {
    try {
      const response = await api.post('/auth/login', { email, password })
      const { data } = response.data

      localStorage.setItem(TOKEN_KEY, data.token)
      user.value = { id: data.id, name: data.name, email: data.email, role: data.role }
      isLoggedIn.value = true
      return { ok: true }
    } catch (error) {
      console.error('Login error:', error)
      // Distinguir "credenciales mal" (respuesta del backend) de "no hay conexión".
      const message = error.response
        ? getApiErrorMessage(error, 'No pudimos abrir el turno con esos datos.')
        : 'No nos pudimos conectar al servidor. Revisá tu conexión e intentá de nuevo.'
      return { ok: false, message }
    }
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY)
    isLoggedIn.value = false
    user.value = null
  }

  return { isLoggedIn, user, login, logout }
})
