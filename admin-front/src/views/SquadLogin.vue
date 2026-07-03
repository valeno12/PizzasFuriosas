<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import UiButton from '../components/ui/UiButton.vue'
import UiInput from '../components/ui/UiInput.vue'

const router = useRouter()
const authStore = useAuthStore()
const email = ref('')
const password = ref('')
const isLoading = ref(false)
const errorMsg = ref('')

async function handleLogin() {
  errorMsg.value = ''
  if (!email.value || !password.value) {
    errorMsg.value = 'Ingresá email y contraseña.'
    return
  }
  isLoading.value = true
  const result = await authStore.login(email.value, password.value)
  isLoading.value = false
  if (result.ok) router.push('/')
  else errorMsg.value = result.message
}
</script>

<template>
  <main class="flex min-h-dvh flex-col bg-background px-6 py-10 text-foreground">
    <section class="m-auto flex w-full max-w-[360px] flex-col items-center">
      <img src="/logo.png" alt="" class="mb-4 h-[118px] w-[118px] object-contain" />
      <img
        src="/nombre.png"
        alt="Pizzas Furiosas"
        class="mb-9 w-56 object-contain brightness-[1.08] invert saturate-[1.6] sepia-[.16]"
      />

      <form class="grid w-full gap-3" @submit.prevent="handleLogin">
        <div
          v-if="errorMsg"
          class="rounded-xl bg-danger-soft px-3 py-2.5 text-sm font-bold text-danger"
          role="alert"
        >
          {{ errorMsg }}
        </div>
        <UiInput
          v-model="email"
          label="Mail"
          type="email"
          required
          autocomplete="email"
          placeholder="vos@pizzafuriosa.ar"
        />
        <UiInput
          v-model="password"
          label="Contraseña"
          type="password"
          required
          autocomplete="current-password"
          placeholder="••••••••"
        />
        <UiButton type="submit" variant="primary" :loading="isLoading" class="w-full"
          >Entrar al horno</UiButton
        >
      </form>
    </section>
    <p class="mt-7 text-center text-[0.78rem] text-muted">Panel interno</p>
  </main>
</template>
