<script setup lang="ts">
import type { EquipmentDto } from "../../../api/equipments/dto";
import EquipmentThumbnail from "./EquipmentThumbnail.vue";

const props = defineProps<{
  equipment: EquipmentDto;
  selected?: boolean;
}>();

const statusLabels: Record<EquipmentDto["status"], string> = {
  Available: "Disponible",
  Borrowed: "Emprunté",
  ToBeDecided: "À déterminer",
  Unavailable: "Indisponible",
  Maintenance: "En maintenance",
  Decommissioned: "Réformé",
};
</script>

<template>
  <span class="equipment-card" :class="{ 'equipment-card--selected': selected }">
    <EquipmentThumbnail :equipment-id="equipment.id" />
    <strong>{{ equipment.name }}</strong>
    <span class="equipment-card__details">
      <span class="equipment-card__details-content">
        <span class="equipment-card__status">{{ statusLabels[props.equipment.status] }}</span>
        <span class="equipment-card__reference">N° de série : {{ equipment.serialNumber }}</span>
      </span>
    </span>
  </span>
</template>

<style scoped lang="scss">
.equipment-card {
  display: grid;
  gap: 0.35rem;
  min-width: 0;
  padding: 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
  color: var(--color-text);
  transition: border-color 150ms ease, transform 150ms ease;
}
.equipment-card--selected {
  border-color: var(--color-secondary);
  background: var(--color-secondary-soft);
}
.equipment-card__details {
  display: grid;
  grid-template-rows: 0fr;
  transition: grid-template-rows 250ms ease;
}
.equipment-card__details-content {
  display: grid;
  justify-items: start;
  gap: 0.35rem;
  min-height: 0;
  overflow: hidden;
}
.equipment-card--selected .equipment-card__details {
  grid-template-rows: 1fr;
}
.equipment-card__status {
  padding: 0.25rem 0.5rem;
  border-radius: 0.5rem;
  background: var(--color-secondary-soft);
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}
.equipment-card__reference {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  overflow-wrap: anywhere;
}
@media (prefers-reduced-motion: reduce) {
  .equipment-card__details { transition: none; }
}
</style>
