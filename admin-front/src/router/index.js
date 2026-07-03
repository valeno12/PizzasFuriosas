import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import TonightView from '../views/TonightView.vue'

// Las entidades con alta (productos, clientes, gastos) montan la misma vista en su
// ruta base y en "/nuevo": la vista detecta el sufijo y abre el drawer de creación.
// Así la URL de "crear" es compartible sin duplicar componentes.
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/login', name: 'login', component: () => import('../views/SquadLogin.vue') },
    { path: '/', name: 'home', component: TonightView, meta: { requiresAuth: true } },
    {
      path: '/pedidos',
      name: 'orders',
      component: () => import('../views/OrdersView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/pedidos/nuevo',
      name: 'order-new',
      component: () => import('../views/NewOrderWizard.vue'),
      meta: { requiresAuth: true, hideNav: true },
    },
    {
      path: '/pedidos/:id(\\d+)/editar',
      name: 'order-edit',
      component: () => import('../views/NewOrderWizard.vue'),
      meta: { requiresAuth: true, hideNav: true },
    },
    {
      path: '/admin/productos',
      name: 'products',
      component: () => import('../views/MenuView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/productos/nuevo',
      name: 'product-new',
      component: () => import('../views/MenuView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/clientes',
      name: 'clients',
      component: () => import('../views/ClientsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/clientes/nuevo',
      name: 'client-new',
      component: () => import('../views/ClientsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/eventos',
      name: 'events',
      component: () => import('../views/EventsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/eventos/nuevo',
      name: 'event-new',
      component: () => import('../views/EventWizardView.vue'),
      meta: { requiresAuth: true, hideNav: true },
    },
    {
      path: '/admin/gastos',
      name: 'expenses',
      component: () => import('../views/ExpensesView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/gastos/nuevo',
      name: 'expense-new',
      component: () => import('../views/ExpensesView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin/rendimientos',
      name: 'performance',
      component: () => import('../views/DashboardView.vue'),
      meta: { requiresAuth: true },
    },
    { path: '/admin/cierre', redirect: '/admin/rendimientos' },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()
  if (to.meta.requiresAuth && !authStore.isLoggedIn) return '/login'
  if (to.path === '/login' && authStore.isLoggedIn) return '/'
  return true
})

export default router
