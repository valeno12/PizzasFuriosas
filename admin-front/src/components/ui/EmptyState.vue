<script setup>
defineProps({
  icon: { type: String, default: 'inbox' },
  title: { type: String, required: true },
  message: { type: String, default: '' },
  mascot: { type: Boolean, default: true },
})
</script>

<template>
  <div
    class="grid min-h-[220px] place-items-center rounded-2xl border border-line bg-surface p-8 text-center"
  >
    <div class="max-w-[420px]">
      <img
        v-if="mascot"
        src="/logo.png"
        alt=""
        class="mx-auto mb-3 h-14 w-14 object-contain drop-shadow-lg"
        aria-hidden="true"
      />
      <span
        v-else
        class="mb-3.5 inline-flex h-[52px] w-[52px] items-center justify-center rounded-2xl bg-surface-2 text-muted"
      >
        <span class="material-symbols-outlined" aria-hidden="true">{{ icon }}</span>
      </span>
      <h2 class="text-base font-extrabold text-foreground">{{ title }}</h2>
      <p v-if="message" class="mt-2 text-muted">{{ message }}</p>
      <div v-if="$slots.actions" class="mt-4 flex justify-center gap-2">
        <slot name="actions" />
      </div>
    </div>
  </div>
</template>
