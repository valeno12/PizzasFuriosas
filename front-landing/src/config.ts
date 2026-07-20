// Configuracion central de la landing. Para produccion, definir las variables
// PUBLIC_* en el hosting apuntando a la API publica y datos vigentes.
export const API_URL = import.meta.env.PUBLIC_API_URL ?? 'http://localhost:5054/api'

const DEFAULT_PHONE_DISPLAY = '+54 9 3434 53-7186'
const DEFAULT_WHATSAPP_PHONE = '5493434537186'

// wa.me requiere el telefono en formato internacional, solo digitos.
export const WHATSAPP_PHONE = (import.meta.env.PUBLIC_WHATSAPP_PHONE ?? DEFAULT_WHATSAPP_PHONE).replace(/\D/g, '')

export const BUSINESS = {
  name: 'Pizza Furiosa',
  city: 'Paraná, Entre Ríos',
  days: 'Jueves a domingo',
  phoneDisplay: import.meta.env.PUBLIC_PHONE_DISPLAY ?? DEFAULT_PHONE_DISPLAY,
  instagram: '@pizza_furiosa',
}

// Consulta de eventos por WhatsApp, con mensaje pre-armado.
export const EVENT_WHATSAPP_URL = `https://wa.me/${WHATSAPP_PHONE}?text=${encodeURIComponent(
  `¡Hola ${BUSINESS.name}! 🍕 Quería información sobre eventos.`,
)}`
