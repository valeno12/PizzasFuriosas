// Links de acción rápida para la operación: WhatsApp al cliente y Maps para el reparto.

// Normaliza teléfonos argentinos: wa.me necesita 54 9 + número sin 0/15.
export function whatsappUrl(phone, message = '') {
  const digits = (phone || '').replace(/\D/g, '')
  if (!digits) return null
  const full = digits.startsWith('54') ? digits : `549${digits}`
  const text = message ? `?text=${encodeURIComponent(message)}` : ''
  return `https://wa.me/${full}${text}`
}

export function mapsUrl(address) {
  if (!address?.street) return null
  const query = `${address.street} ${address.number || ''}`.trim()
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`
}

export function formatAddress(address) {
  if (!address) return ''
  return `${address.street} ${address.number}${address.apartment ? `, ${address.apartment}` : ''}`
}
