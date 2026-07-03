<script setup>
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import UiIconButton from './ui/UiIconButton.vue'

defineOptions({ name: 'AppShell' })

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const moreOpen = ref(false)

const primaryItems = [
  { to: '/', label: 'Hoy', icon: 'local_fire_department', match: (path) => path === '/' },
  {
    to: '/pedidos',
    label: 'Pedidos',
    icon: 'receipt_long',
    match: (path) => path.startsWith('/pedidos'),
  },
  {
    to: '/admin/productos',
    label: 'Productos',
    icon: 'local_pizza',
    match: (path) => path.startsWith('/admin/productos'),
  },
  {
    to: '/admin/eventos',
    label: 'Eventos',
    icon: 'event',
    match: (path) => path.startsWith('/admin/eventos'),
  },
]

const secondaryItems = [
  {
    to: '/admin/clientes',
    label: 'Clientes',
    icon: 'groups',
    match: (path) => path.startsWith('/admin/clientes'),
  },
  {
    to: '/admin/gastos',
    label: 'Gastos',
    icon: 'payments',
    match: (path) => path.startsWith('/admin/gastos'),
  },
  {
    to: '/admin/rendimientos',
    label: 'Rendimientos',
    icon: 'query_stats',
    match: (path) => path.startsWith('/admin/rendimientos'),
  },
]

const currentItem = computed(() =>
  [...primaryItems, ...secondaryItems].find((item) => item.match(route.path)),
)
const isSecondaryActive = computed(() => secondaryItems.some((item) => item.match(route.path)))

// Las rutas de wizard (meta.hideNav) ocultan la bottom nav para dar lugar a su footer fijo.
const hideNav = computed(() => Boolean(route.meta.hideNav))

watch(
  () => route.path,
  () => {
    moreOpen.value = false
  },
)

function logout() {
  moreOpen.value = false
  authStore.logout()
  router.push('/login')
}

const sideLinkClass = (active) => [
  'flex min-h-10 items-center gap-2.5 rounded-[10px] px-3 text-[0.92rem] font-bold transition-colors',
  active
    ? 'bg-primary text-primary-foreground'
    : 'text-foreground/85 hover:bg-surface-2 hover:text-foreground',
]
</script>

<template>
  <div class="min-h-dvh bg-background text-foreground">
    <!-- Sidebar desktop -->
    <aside
      class="fixed inset-y-0 left-0 z-40 hidden w-[var(--sidebar-width)] flex-col border-r border-line bg-surface md:flex"
      aria-label="Navegación principal"
    >
      <RouterLink to="/" class="flex h-16 items-center border-b border-line px-5">
        <img
          src="/nombre.png"
          alt="Pizzas Furiosas"
          class="h-7 w-auto object-contain brightness-[1.08] invert saturate-[1.6] sepia-[.16]"
        />
      </RouterLink>

      <nav class="min-h-0 flex-1 overflow-y-auto p-3">
        <p
          class="px-3 pb-2 pt-1.5 text-[0.64rem] font-extrabold uppercase tracking-[0.12em] text-muted"
        >
          Turno
        </p>
        <RouterLink
          v-for="item in primaryItems"
          :key="item.to"
          :to="item.to"
          :class="sideLinkClass(item.match(route.path))"
        >
          <span class="material-symbols-outlined !text-xl" aria-hidden="true">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
        </RouterLink>

        <p
          class="mt-4 px-3 pb-2 pt-1.5 text-[0.64rem] font-extrabold uppercase tracking-[0.12em] text-muted"
        >
          Administración
        </p>
        <RouterLink
          v-for="item in secondaryItems"
          :key="item.to"
          :to="item.to"
          :class="sideLinkClass(item.match(route.path))"
        >
          <span class="material-symbols-outlined !text-xl" aria-hidden="true">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
        </RouterLink>
      </nav>

      <button
        type="button"
        class="m-3 flex min-h-10 items-center gap-2.5 rounded-[10px] px-3 text-[0.92rem] font-bold text-foreground/85 transition-colors hover:bg-surface-2 hover:text-foreground"
        @click="logout"
      >
        <span class="material-symbols-outlined !text-xl" aria-hidden="true">logout</span>
        Salir
      </button>
    </aside>

    <!-- Topbar mobile -->
    <header
      class="sticky top-0 z-[35] flex h-14 items-center justify-between gap-3 border-b border-line bg-background/95 px-4 backdrop-blur-md md:hidden"
    >
      <RouterLink to="/" class="min-w-0">
        <img
          src="/nombre.png"
          alt="Pizzas Furiosas"
          class="h-7 w-auto object-contain brightness-[1.08] invert saturate-[1.6] sepia-[.16]"
        />
      </RouterLink>
      <div
        v-if="currentItem"
        class="inline-flex h-8 items-center gap-1.5 rounded-lg bg-surface px-2.5 text-[0.86rem] font-bold text-foreground/80"
      >
        <span class="material-symbols-outlined !text-lg text-primary" aria-hidden="true">{{
          currentItem.icon
        }}</span>
        {{ currentItem.label }}
      </div>
    </header>

    <main
      :class="[
        'min-h-dvh pt-5 md:pb-10 md:pl-[var(--sidebar-width)]',
        hideNav ? 'pb-0' : 'pb-[calc(72px+env(safe-area-inset-bottom))]',
      ]"
    >
      <slot />
    </main>

    <!-- Bottom nav mobile -->
    <nav
      v-if="!hideNav"
      class="fixed inset-x-0 bottom-0 z-[45] grid h-[calc(64px+env(safe-area-inset-bottom))] grid-cols-5 border-t border-line bg-background/95 pb-[env(safe-area-inset-bottom)] backdrop-blur-md md:hidden"
      aria-label="Navegación móvil"
    >
      <RouterLink
        v-for="item in primaryItems"
        :key="item.to"
        :to="item.to"
        :class="[
          'flex min-w-0 flex-col items-center justify-center gap-0.5 text-[0.72rem] font-bold',
          item.match(route.path) ? 'text-primary' : 'text-muted',
        ]"
      >
        <span class="material-symbols-outlined !text-[22px]" aria-hidden="true">{{
          item.icon
        }}</span>
        <span>{{ item.label }}</span>
      </RouterLink>
      <button
        type="button"
        :class="[
          'flex min-w-0 flex-col items-center justify-center gap-0.5 text-[0.72rem] font-bold',
          isSecondaryActive ? 'text-primary' : 'text-muted',
        ]"
        @click="moreOpen = true"
      >
        <span class="material-symbols-outlined !text-[22px]" aria-hidden="true">more_horiz</span>
        <span>Más</span>
      </button>
    </nav>

    <!-- Sheet "Más" mobile -->
    <div v-if="moreOpen" class="fixed inset-0 z-[60]">
      <button
        type="button"
        class="absolute inset-0 bg-black/70"
        aria-label="Cerrar más opciones"
        @click="moreOpen = false"
      ></button>
      <section
        class="absolute inset-x-0 bottom-0 grid gap-2.5 rounded-t-[20px] border-t border-line-strong bg-surface p-3.5 pb-[max(14px,env(safe-area-inset-bottom))]"
        aria-label="Administración"
      >
        <div class="flex items-center justify-between gap-3 px-1">
          <h2 class="font-display text-3xl text-foreground">Más</h2>
          <UiIconButton icon="close" label="Cerrar" @click="moreOpen = false" />
        </div>
        <div class="grid gap-2">
          <RouterLink
            v-for="item in secondaryItems"
            :key="item.to"
            :to="item.to"
            :class="[
              'flex min-h-[52px] items-center gap-3 rounded-xl px-3.5 font-extrabold',
              item.match(route.path)
                ? 'bg-primary text-primary-foreground'
                : 'bg-surface-2 text-foreground',
            ]"
          >
            <span class="material-symbols-outlined" aria-hidden="true">{{ item.icon }}</span>
            <span>{{ item.label }}</span>
          </RouterLink>
          <button
            type="button"
            class="flex min-h-[52px] items-center gap-3 rounded-xl bg-surface-2 px-3.5 font-extrabold text-danger"
            @click="logout"
          >
            <span class="material-symbols-outlined" aria-hidden="true">logout</span>
            <span>Salir</span>
          </button>
        </div>
      </section>
    </div>
  </div>
</template>
