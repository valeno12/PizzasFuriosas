<script setup>
defineProps({
  label: { type: String, required: true },
  forId: { type: String, default: null },
  required: { type: Boolean, default: false },
  helper: { type: String, default: '' },
  error: { type: String, default: '' },
})
</script>

<template>
  <div class="grid gap-[7px]">
    <label class="text-[0.86rem] font-bold text-foreground" :for="forId">
      {{ label }} <span v-if="required" class="text-primary" aria-hidden="true">*</span>
    </label>
    <slot />
    <p v-if="helper && !error" class="text-[0.82rem] text-muted">{{ helper }}</p>
    <p v-if="error" class="text-[0.82rem] font-semibold text-danger">{{ error }}</p>
  </div>
</template>
