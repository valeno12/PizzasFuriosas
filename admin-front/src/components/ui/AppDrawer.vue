<script setup>
import { onMounted, onUnmounted, watch } from 'vue'
import UiIconButton from './UiIconButton.vue'

const props = defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, required: true },
  description: { type: String, default: '' },
})
const emit = defineEmits(['close'])

function onKeydown(event) {
  if (event.key === 'Escape' && props.open) emit('close')
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
    <Transition name="drawer">
      <div
        v-if="open"
        class="fixed inset-0 z-[500] bg-black/45 backdrop-blur"
        @click.self="emit('close')"
      >
        <!-- La clase drawer-panel la usa la transición global para el slide -->
        <aside
          class="drawer-panel fixed inset-0 z-[510] flex flex-col border-line bg-surface transition-transform sm:inset-y-0 sm:left-auto sm:right-0 sm:w-[560px] sm:border-l"
          role="dialog"
          aria-modal="true"
          :aria-label="title"
        >
          <header class="flex shrink-0 items-center justify-between gap-3 border-b border-line p-5">
            <div class="min-w-0">
              <h2 class="truncate text-[1.08rem] font-extrabold text-foreground">{{ title }}</h2>
              <p v-if="description" class="mt-1 text-sm text-muted">{{ description }}</p>
            </div>
            <UiIconButton icon="close" label="Cerrar" @click="emit('close')" />
          </header>
          <main class="min-h-0 flex-1 overflow-y-auto p-5">
            <slot />
          </main>
          <footer
            v-if="$slots.footer"
            class="flex shrink-0 items-center justify-end gap-3 border-t border-line p-5"
          >
            <slot name="footer" />
          </footer>
        </aside>
      </div>
    </Transition>
  </Teleport>
</template>
