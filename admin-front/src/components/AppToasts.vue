<script setup>
import { useToastsStore } from '../stores/toasts'
import UiIconButton from './ui/UiIconButton.vue'

const store = useToastsStore()

const meta = {
  success: { icon: 'check_circle', label: 'Listo', tone: 'bg-success-soft text-success' },
  error: { icon: 'error', label: 'Error', tone: 'bg-danger-soft text-danger' },
  warning: { icon: 'warning', label: 'Atención', tone: 'bg-warning-soft text-warning' },
  info: { icon: 'info', label: 'Info', tone: 'bg-info-soft text-info' },
}

function toastMeta(variant) {
  return meta[variant] || meta.info
}
</script>

<template>
  <div
    class="pointer-events-none fixed z-[700] grid w-[min(calc(100vw-32px),420px)] gap-2.5 max-md:inset-x-3 max-md:top-16 md:right-4 md:top-4"
    aria-live="polite"
    aria-atomic="true"
  >
    <TransitionGroup name="toast">
      <article
        v-for="toast in store.items"
        :key="toast.id"
        class="pointer-events-auto grid grid-cols-[38px_1fr_auto] items-start gap-2.5 rounded-xl border border-line-strong bg-popover/95 p-3 shadow-xl backdrop-blur-md"
      >
        <span
          :class="['grid h-9 w-9 place-items-center rounded-full', toastMeta(toast.variant).tone]"
        >
          <span class="material-symbols-outlined !text-[19px]" aria-hidden="true">{{
            toastMeta(toast.variant).icon
          }}</span>
        </span>
        <div class="min-w-0">
          <p class="text-sm font-extrabold text-foreground">
            {{ toast.title || toastMeta(toast.variant).label }}
          </p>
          <p class="mt-0.5 text-sm text-muted">{{ toast.message }}</p>
        </div>
        <UiIconButton icon="close" label="Cerrar notificación" @click="store.remove(toast.id)" />
      </article>
    </TransitionGroup>
  </div>
</template>
