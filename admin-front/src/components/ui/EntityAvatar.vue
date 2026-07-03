<script setup>
import { computed } from 'vue'
import { initials } from '../../utils/formatters'

const props = defineProps({
  name: { type: String, default: '' },
  size: { type: String, default: 'md' }, // sm | md | lg
})

const sizes = {
  sm: 'h-8 w-8 text-[0.75rem]',
  md: 'h-9 w-9 text-[0.8rem]',
  lg: 'h-12 w-12 text-base',
}

const text = computed(() => initials(props.name))
</script>

<template>
  <span
    :class="[
      'grid shrink-0 place-items-center rounded-[10px] bg-primary-soft font-extrabold text-primary',
      sizes[size],
    ]"
    aria-hidden="true"
  >
    {{ text }}
  </span>
</template>
