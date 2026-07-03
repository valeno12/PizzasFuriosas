// Rangos de fechas en hora local, serializados a ISO (el backend guarda UTC).

export function startOfToday() {
  const now = new Date()
  return new Date(now.getFullYear(), now.getMonth(), now.getDate())
}

export function todayRange() {
  const from = startOfToday()
  const to = new Date(from)
  to.setDate(to.getDate() + 1)
  to.setMilliseconds(-1)
  return { from: from.toISOString(), to: to.toISOString() }
}

export function lastDaysRange(days) {
  const { to } = todayRange()
  const from = startOfToday()
  from.setDate(from.getDate() - (days - 1))
  return { from: from.toISOString(), to }
}

export function monthRange() {
  const now = new Date()
  const from = new Date(now.getFullYear(), now.getMonth(), 1)
  const { to } = todayRange()
  return { from: from.toISOString(), to }
}

// Rango a partir de inputs type="date" (YYYY-MM-DD): desde las 00:00 del "desde"
// hasta las 23:59:59.999 del "hasta". Cualquiera de los dos puede faltar.
export function customRange(fromStr, toStr) {
  const range = {}
  if (fromStr) range.from = new Date(`${fromStr}T00:00:00`).toISOString()
  if (toStr) {
    const to = new Date(`${toStr}T00:00:00`)
    to.setDate(to.getDate() + 1)
    to.setMilliseconds(-1)
    range.to = to.toISOString()
  }
  return range
}

export function isToday(value) {
  if (!value) return false
  const date = new Date(value)
  const now = new Date()
  return (
    date.getFullYear() === now.getFullYear() &&
    date.getMonth() === now.getMonth() &&
    date.getDate() === now.getDate()
  )
}
