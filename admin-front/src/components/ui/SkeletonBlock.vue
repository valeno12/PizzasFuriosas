<script setup>
defineProps({
  height: { type: String, default: '120px' },
  radius: { type: String, default: '14px' },
})
</script>

<template>
  <div
    class="relative overflow-hidden bg-surface-2"
    :style="{ height, borderRadius: radius }"
    aria-hidden="true"
  >
    <div
      class="absolute inset-0 -translate-x-full animate-shimmer bg-gradient-to-r from-transparent via-white/10 to-transparent"
    ></div>
  </div>
</template>
