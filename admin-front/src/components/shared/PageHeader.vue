<script setup>
defineOptions({ name: 'PageHeader' })

defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
})
</script>

<template>
  <div class="grid grid-cols-[minmax(0,1fr)_auto] items-end gap-3 px-4 pb-3 pt-5 md:px-8 md:pt-8">
    <div class="min-w-0">
      <h1
        class="truncate font-display text-[clamp(2.4rem,8vw,3.8rem)] leading-none tracking-[0.01em] text-foreground"
      >
        {{ title }}
      </h1>
      <p v-if="subtitle" class="mt-1.5 text-sm leading-snug text-muted">{{ subtitle }}</p>
    </div>
    <div v-if="$slots.right" class="flex shrink-0 items-center gap-2">
      <slot name="right" />
    </div>
  </div>
</template>
