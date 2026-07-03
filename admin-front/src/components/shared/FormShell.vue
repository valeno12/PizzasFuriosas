<script setup>
import { computed } from 'vue'

defineOptions({ name: 'FormShell' })

const props = defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  step: { type: Number, default: null },
  totalSteps: { type: Number, default: null },
  canSubmit: { type: Boolean, default: true },
  primaryLabel: { type: String, default: 'Guardar' },
  isLoading: { type: Boolean, default: false },
  totalAmount: { type: [Number, String], default: null },
})

// La navegación la decide el padre (puede querer confirmar antes de descartar).
const emit = defineEmits(['primary', 'back', 'cancel'])

const progress = computed(() => {
  if (!props.step || !props.totalSteps) return null
  return (props.step / props.totalSteps) * 100
})

const stepLabel = computed(() => {
  if (!props.step || !props.totalSteps) return ''
  return `Paso ${props.step} de ${props.totalSteps}`
})
</script>

<template>
  <div class="flex min-h-full flex-col">
    <!-- Header sticky -->
    <header
      class="sticky top-0 z-20 border-b border-line bg-background/95 py-2 backdrop-blur-md md:pb-3 md:pt-6"
    >
      <div class="mx-auto flex max-w-[760px] items-center gap-3 px-4">
        <button
          type="button"
          class="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-[10px] text-foreground transition-colors hover:bg-surface-2"
          aria-label="Atrás"
          @click="emit('back')"
        >
          <span class="material-symbols-outlined !text-[22px]" aria-hidden="true">arrow_back</span>
        </button>
        <div class="min-w-0 flex-1">
          <div v-if="stepLabel" class="text-[0.72rem] font-bold tracking-wide text-muted">
            {{ stepLabel }}
          </div>
          <div
            class="truncate text-[1.1rem] font-extrabold text-foreground md:font-display md:text-3xl md:font-normal"
          >
            {{ title }}
          </div>
          <div v-if="subtitle" class="truncate text-[0.78rem] text-muted">{{ subtitle }}</div>
        </div>
        <button
          type="button"
          class="h-10 shrink-0 px-3 text-sm font-semibold text-muted transition-colors hover:text-foreground"
          @click="emit('cancel')"
        >
          Cancelar
        </button>
      </div>
      <!-- Barra de progreso del wizard -->
      <div v-if="progress !== null" class="h-[3px] bg-surface-2">
        <div
          class="h-full rounded-r-full bg-primary transition-[width] duration-300"
          :style="{ width: `${progress}%` }"
        />
      </div>
    </header>

    <!-- Contenido -->
    <main class="mx-auto w-full max-w-[760px] flex-1 pb-[120px] pt-6">
      <slot />
    </main>

    <!-- Footer fijo -->
    <footer
      class="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-background/95 px-4 pb-[max(12px,env(safe-area-inset-bottom))] pt-2.5 backdrop-blur-lg md:left-[var(--sidebar-width)]"
    >
      <div class="mx-auto grid max-w-[760px] gap-2 px-4">
        <div v-if="totalAmount !== null" class="flex items-center justify-between">
          <span class="text-[0.8rem] font-semibold text-muted">Total</span>
          <span class="text-2xl font-extrabold text-primary">{{ totalAmount }}</span>
        </div>
        <slot name="footer-extra" />
        <button
          type="button"
          class="inline-flex h-[52px] w-full items-center justify-center gap-2 rounded-[14px] bg-primary text-base font-extrabold text-primary-foreground transition hover:opacity-90 active:scale-[0.99] disabled:opacity-30"
          :disabled="!canSubmit || isLoading"
          @click="emit('primary')"
        >
          <span
            v-if="isLoading"
            class="material-symbols-outlined animate-spin !text-xl"
            aria-hidden="true"
            >progress_activity</span
          >
          {{ primaryLabel }}
        </button>
      </div>
    </footer>
  </div>
</template>
