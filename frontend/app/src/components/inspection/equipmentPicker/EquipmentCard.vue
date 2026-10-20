<script lang="ts" setup>
import type { EquipmentDto } from "../../../api/equipments/dto.ts";
import EquipmentThumbnail from "./EquipmentThumbnail.vue";

defineProps<{ equipment: EquipmentDto }>();
</script>

<template>
  <div class="equipment-card">
    <EquipmentThumbnail :equipment-id="equipment.id" />
    <strong>{{ equipment.name }}</strong>
    <span
      :class="{
        'equipment-card__status': true,
        'equipment-card__status--borrowed': equipment.status === 'Borrowed',
        'equipment-card__status--available': equipment.status === 'Available',
      }"
    >
      Etat des lieux de {{ equipment.status === "Borrowed" ? "Retour" : "Départ" }}
    </span>
  </div>
</template>

<style lang="scss" scoped>
.equipment-card {
  display: grid;
  gap: 0.35rem;
  min-width: 0;
  height: 100%;
  padding: 0.65rem;
  text-decoration: none;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;

  &:hover {
    border-color: var(--color-primary);
    transform: translateY(-0.2rem);
    scale: 1.01;
    transition: all 0.2s ease-in-out;
  }
  &:focus-visible {
    outline: 0.2rem solid var(--color-primary);
    outline-offset: 0.125rem;
  }

  &__status {
    display: inline-block;
    padding: 0.25rem 0.5rem;
    border-radius: 0.5rem;
    border: 1px solid var(--color-border);
    font-size: 0.75rem;
    font-weight: bold;
    text-align: center;

    &--available {
      color: var(--color-success);
      background: var(--color-success-soft);
    }

    &--borrowed {
      color: var(--color-info);
      background: var(--color-info-soft);
    }
  }
}
</style>
