import { API_URL, WHATSAPP_PHONE, BUSINESS } from '../config'

type Product = {
  id: number
  name: string
  price: number
  categoryId: number
  categoryName: string
  imageUrl: string | null
}
type Category = { id: number; name: string }

const CART_KEY = 'pf-cart'
const money = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: 'ARS',
  maximumFractionDigits: 0,
})

// Estado
let products: Product[] = []
let activeCategory: number | null = null
let delivery = 'Take Away'
const cart = new Map<number, number>() // productId -> cantidad

// Helpers de DOM
const $ = <T extends Element>(sel: string) => document.querySelector<T>(sel)
const el = {
  filters: $<HTMLElement>('[data-filters]'),
  grid: $<HTMLElement>('[data-product-grid]'),
  loading: $<HTMLElement>('[data-menu-loading]'),
  error: $<HTMLElement>('[data-menu-error]'),
  lines: $<HTMLElement>('[data-order-lines]'),
  empty: $<HTMLElement>('[data-order-empty]'),
  total: $<HTMLElement>('[data-order-total]'),
  badge: $<HTMLElement>('[data-order-badge]'),
  waBtn: $<HTMLButtonElement>('[data-wa-send]'),
  name: $<HTMLInputElement>('[data-field-name]'),
  address: $<HTMLTextAreaElement>('[data-field-address]'),
  deliveryRow: $<HTMLElement>('[data-delivery]'),
  cartCount: $<HTMLElement>('[data-cart-count]'),
  mobileBar: $<HTMLElement>('[data-mobile-bar]'),
  mobileSummary: $<HTMLElement>('[data-mobile-summary]'),
}

function loadCart() {
  try {
    const raw = JSON.parse(localStorage.getItem(CART_KEY) || '[]') as { id: number; qty: number }[]
    raw.forEach((i) => cart.set(i.id, i.qty))
  } catch {
    /* carrito vacío */
  }
}

function saveCart() {
  const data = [...cart.entries()].map(([id, qty]) => ({ id, qty }))
  localStorage.setItem(CART_KEY, JSON.stringify(data))
}

function setQty(id: number, qty: number) {
  if (qty <= 0) cart.delete(id)
  else cart.set(id, qty)
  saveCart()
  renderGrid()
  renderOrder()
}

// Descarta del carrito lo que ya no exista o no esté disponible en la carta actual.
function reconcileCart() {
  const validIds = new Set(products.map((p) => p.id))
  for (const id of cart.keys()) if (!validIds.has(id)) cart.delete(id)
  saveCart()
}

const cartTotal = () =>
  [...cart.entries()].reduce((sum, [id, qty]) => {
    const p = products.find((x) => x.id === id)
    return p ? sum + p.price * qty : sum
  }, 0)

const cartCount = () => [...cart.values()].reduce((a, b) => a + b, 0)

function renderFilters(categories: Category[]) {
  if (!el.filters) return
  // Solo mostramos categorías que tengan al menos un producto disponible cargado.
  const usedCategoryIds = new Set(products.map((p) => p.categoryId))
  const visibleCategories = categories.filter((c) => usedCategoryIds.has(c.id))
  const tabs = [{ id: null as number | null, name: 'Todas' }, ...visibleCategories]
  el.filters.innerHTML = ''
  for (const tab of tabs) {
    const btn = document.createElement('button')
    btn.className = 'filter' + (activeCategory === tab.id ? ' active' : '')
    btn.textContent = tab.name
    btn.type = 'button'
    btn.addEventListener('click', () => {
      activeCategory = tab.id
      renderFilters(categories)
      renderGrid()
    })
    el.filters.appendChild(btn)
  }
}

function renderGrid() {
  if (!el.grid) return
  const list = products.filter((p) => activeCategory === null || p.categoryId === activeCategory)

  if (list.length === 0) {
    el.grid.innerHTML = '<p class="menu-state">No hay productos en esta categoría.</p>'
    return
  }

  el.grid.innerHTML = ''
  for (const p of list) {
    const qty = cart.get(p.id) || 0
    const card = document.createElement('article')
    card.className = 'product-card' + (qty > 0 ? ' in-cart' : '')

    // Imagen solo si el producto tiene foto real (la mayoría no tendrá)
    if (p.imageUrl) {
      const img = document.createElement('img')
      img.className = 'product-photo'
      img.src = p.imageUrl
      img.alt = p.name
      img.loading = 'lazy'
      card.appendChild(img)
    }

    const meta = document.createElement('div')
    meta.className = 'product-meta'
    meta.innerHTML = `<h3></h3><span class="price">${money.format(p.price)}</span>`
    meta.querySelector('h3')!.textContent = p.name
    card.appendChild(meta)

    if (qty > 0) {
      const row = document.createElement('div')
      row.className = 'qty-row'
      row.innerHTML = `
        <span>En el pedido</span>
        <div class="qty-controls">
          <button type="button" aria-label="Quitar uno">−</button>
          <b>${qty}</b>
          <button type="button" aria-label="Agregar uno">+</button>
        </div>`
      const [minus, plus] = row.querySelectorAll('button')
      minus.addEventListener('click', () => setQty(p.id, qty - 1))
      plus.addEventListener('click', () => setQty(p.id, qty + 1))
      card.appendChild(row)
    } else {
      const add = document.createElement('button')
      add.className = 'add-btn'
      add.type = 'button'
      add.textContent = '+ Agregar al pedido'
      add.addEventListener('click', () => setQty(p.id, 1))
      card.appendChild(add)
    }

    el.grid.appendChild(card)
  }
}

function renderOrder() {
  const count = cartCount()
  const total = cartTotal()

  // Badge del panel + pill del nav
  if (el.badge) el.badge.textContent = String(count)
  if (el.cartCount) {
    el.cartCount.textContent = String(count)
    el.cartCount.hidden = count === 0
  }

  // Barra mobile
  if (el.mobileBar) el.mobileBar.hidden = count === 0
  if (el.mobileSummary) {
    el.mobileSummary.textContent = `${count} producto${count !== 1 ? 's' : ''} · ${money.format(total)}`
  }

  // Líneas del pedido
  if (el.lines && el.empty) {
    el.empty.hidden = count > 0
    el.lines.innerHTML = ''
    for (const [id, qty] of cart.entries()) {
      const p = products.find((x) => x.id === id)
      if (!p) continue
      const line = document.createElement('div')
      line.className = 'order-line'
      line.innerHTML = `
        <div>
          <b></b>
          <small>${money.format(p.price)} c/u · <button type="button">Quitar</button></small>
        </div>
        <span>${money.format(p.price * qty)}</span>`
      line.querySelector('b')!.textContent = `${qty}x ${p.name}`
      line.querySelector('small button')!.addEventListener('click', () => setQty(id, 0))
      el.lines.appendChild(line)
    }
  }

  if (el.total) el.total.textContent = money.format(total)
  if (el.waBtn) el.waBtn.disabled = count === 0
}

// El mensaje lleva solo la info del pedido (qué, cuánto, entrega, contacto).
// Los precios y el total los define el pizzero al responder, no la web.
function buildWhatsappUrl() {
  const lines = [...cart.entries()]
    .map(([id, qty]) => {
      const p = products.find((x) => x.id === id)
      return p ? `• ${qty}x ${p.name}` : ''
    })
    .filter(Boolean)

  const name = el.name?.value.trim()
  const address = el.address?.value.trim()

  const parts = [
    `¡Hola ${BUSINESS.name}! 🍕 Quiero hacer este pedido:`,
    '',
    ...lines,
    '',
    `Entrega: ${delivery}`,
  ]
  if (name) parts.push(`Nombre: ${name}`)
  if (delivery !== 'Take Away' && address) parts.push(`Dirección: ${address}`)

  return `https://wa.me/${WHATSAPP_PHONE}?text=${encodeURIComponent(parts.join('\n'))}`
}

function clearCart() {
  cart.clear()
  saveCart()
  renderGrid()
  renderOrder()
}

function wireControls() {
  // Toggle de entrega
  el.deliveryRow?.querySelectorAll<HTMLButtonElement>('[data-delivery-option]').forEach((btn) => {
    btn.addEventListener('click', () => {
      delivery = btn.dataset.deliveryOption || 'Take Away'
      el.deliveryRow
        ?.querySelectorAll('button')
        .forEach((b) => b.classList.toggle('active', b === btn))
      // La dirección solo tiene sentido para envío
      if (el.address) el.address.hidden = delivery === 'Take Away'
    })
  })

  el.waBtn?.addEventListener('click', () => {
    if (cartCount() === 0) return
    window.open(buildWhatsappUrl(), '_blank', 'noopener')
    // El pedido ya se mandó: vaciamos para que no quede colgado en la próxima visita.
    clearCart()
  })
}

async function fetchMenu() {
  const [catsRes, prodsRes] = await Promise.all([
    fetch(`${API_URL}/categories`),
    fetch(`${API_URL}/products?pageSize=100&isAvailable=true`),
  ])
  if (!catsRes.ok || !prodsRes.ok) throw new Error('API error')
  const categories: Category[] = (await catsRes.json()).data
  products = (await prodsRes.json()).data.items
  return categories
}

export async function initMenu() {
  loadCart()
  wireControls()
  renderOrder() // pinta el carrito guardado aunque la carta todavía no cargó

  try {
    const categories = await fetchMenu()
    reconcileCart()
    if (el.loading) el.loading.hidden = true
    renderFilters(categories)
    renderGrid()
    renderOrder()
  } catch (err) {
    console.error('No se pudo cargar la carta:', err)
    if (el.loading) el.loading.hidden = true
    if (el.error) el.error.hidden = false
  }
}
