<script setup>
import { onMounted, onUnmounted, watch } from 'vue'
import UiButton from './UiButton.vue'
import UiIconButton from './UiIconButton.vue'

const props = defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, required: true },
  message: { type: String, required: true },
  confirmLabel: { type: String, default: 'Confirmar' },
  cancelLabel: { type: String, default: 'Cancelar' },
  tone: { type: String, default: 'danger' },
  loading: { type: Boolean, default: false },
})
const emit = defineEmits(['confirm', 'close'])

function onKeydown(event) {
  if (event.key === 'Escape' && props.open && !props.loading) emit('close')
}

watch(
  () => props.open,
  (value) => {
    document.body.style.overflow = value ? 'hidden' : ''
  },
)

onMounted(() => window.addEventListener('keydown', onKeydown))
onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="open"
        class="fixed inset-0 z-[500] bg-black/45 backdrop-blur"
        @click.self="!loading && emit('close')"
      >
        <section
          class="fixed left-1/2 top-1/2 z-[510] w-[min(calc(100%-32px),480px)] -translate-x-1/2 -translate-y-1/2 overflow-hidden rounded-2xl bg-surface shadow-2xl"
          role="alertdialog"
          aria-modal="true"
          :aria-label="title"
        >
          <header class="flex items-center justify-between gap-3 border-b border-line p-5">
            <h2 class="text-[1.08rem] font-extrabold text-foreground">{{ title }}</h2>
            <UiIconButton icon="close" label="Cerrar" :disabled="loading" @click="emit('close')" />
          </header>
          <div class="p-5">
            <p class="text-muted">{{ message }}</p>
          </div>
          <footer class="flex items-center justify-end gap-3 border-t border-line p-5">
            <UiButton variant="secondary" :disabled="loading" @click="emit('close')">{{
              cancelLabel
            }}</UiButton>
            <UiButton :variant="tone" :loading="loading" @click="emit('confirm')">{{
              confirmLabel
            }}</UiButton>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>
