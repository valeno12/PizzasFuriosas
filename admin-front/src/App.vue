<script setup>
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth'
import AppShell from './components/AppShell.vue'
import AppToasts from './components/AppToasts.vue'

const authStore = useAuthStore()
const route = useRoute()
const isAuthRoute = computed(() => route.name === 'login')
</script>

<template>
  <RouterView v-if="isAuthRoute || !authStore.isLoggedIn" />

  <AppShell v-else>
    <RouterView v-slot="{ Component }">
      <Transition name="page" mode="out-in">
        <component :is="Component" />
      </Transition>
    </RouterView>
  </AppShell>

  <AppToasts />
</template>
