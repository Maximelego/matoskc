<script setup lang="ts">
import { computed } from "vue";
import type { EquipmentStatus } from "../../api/equipments/dto";

const props = defineProps<{ status: EquipmentStatus }>();

const statuses = {
  Available: { label: "Disponible", tone: "success" },
  Unavailable: { label: "Indisponible", tone: "danger" },
  ToBeDecided: { label: "À déterminer", tone: "neutral" },
  Decommissioned: { label: "Réformé", tone: "neutral" },
  Maintenance: { label: "En maintenance", tone: "warning" },
  Borrowed: { label: "Emprunté", tone: "info" },
} as const;

const display = computed(
  () =>
    statuses[props.status as keyof typeof statuses] ?? {
      label: props.status,
      tone: "neutral",
    },
);
</script>

<template>
  <span
    class="status-badge"
    :class="`status-badge--${display.tone}`"
  >
    {{ display.label }}
  </span>
</template>

<style scoped lang="scss">
.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.625rem;
  border-radius: 999px;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  line-height: 1.3;
  white-space: nowrap;

  &--success {
    color: var(--color-success);
    background: var(--color-success-soft);
  }

  &--danger {
    color: var(--color-danger);
    background: var(--color-danger-soft);
  }

  &--warning {
    color: var(--color-warning);
    background: var(--color-warning-soft);
  }

  &--info {
    color: var(--color-info);
    background: var(--color-info-soft);
  }

  &--neutral {
    color: var(--color-text-secondary);
    background: var(--color-surface-secondary);
  }
}
</style>
