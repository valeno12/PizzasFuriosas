<script setup>
import { computed } from 'vue'
import UiBadge from './UiBadge.vue'
import { eventStatusMeta, orderStatusMeta } from '../../constants/status'

// Pedidos se identifican por statusId (número); eventos por su texto de estado.
const props = defineProps({
  statusId: { type: Number, default: null },
  eventStatus: { type: String, default: null },
})

const meta = computed(() =>
  props.eventStatus !== null ? eventStatusMeta(props.eventStatus) : orderStatusMeta(props.statusId),
)
</script>

<template>
  <UiBadge :tone="meta.tone">{{ meta.label }}</UiBadge>
</template>
