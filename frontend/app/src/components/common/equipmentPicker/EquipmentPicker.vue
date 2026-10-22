<script setup lang="ts">
import { computed, ref } from "vue";
import type { EquipmentDto } from "../../../api/equipments/dto";
import EquipmentCard from "./EquipmentCard.vue";

const props = defineProps<{
  equipment: EquipmentDto[];
  selectedId?: string;
}>();
const emit = defineEmits<{ select: [equipment: EquipmentDto] }>();
const search = ref("");
const matches = computed(() =>
  props.equipment.filter((item) =>
    `${item.name} ${item.serialNumber}`
      .toLocaleLowerCase("fr")
      .includes(search.value.trim().toLocaleLowerCase("fr")),
  ),
);
</script>

<template>
  <div class="equipment-picker">
    <label for="equipment-picker-search">Rechercher un équipement</label>
    <input
      id="equipment-picker-search"
      v-model="search"
      type="search"
      placeholder="Nom ou numéro de série"
    />
    <p v-if="!matches.length">Aucun équipement ne correspond à votre recherche.</p>
    <div
      v-else
      class="equipment-picker__grid"
    >
      <button
        v-for="item in matches"
        :key="item.id"
        type="button"
        class="equipment-picker__choice"
        :aria-pressed="selectedId ? item.id === selectedId : undefined"
        :aria-label="`Sélectionner ${item.name}`"
        @click="emit('select', item)"
      >
        <EquipmentCard
          :equipment="item"
          :selected="item.id === selectedId"
        />
      </button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.equipment-picker {
  display: grid;
  gap: 0.75rem;
  min-width: 0;
}
.equipment-picker > input {
  box-sizing: border-box;
  width: 100%;
  min-height: 2.75rem;
  padding: 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-input-background);
}
.equipment-picker__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 13rem), 1fr));
  gap: 0.75rem;
}
.equipment-picker__choice {
  min-width: 0;
  padding: 0;
  border: 0;
  border-radius: 0.75rem;
  background: transparent;
  color: inherit;
  text-align: left;
  font: inherit;
  cursor: pointer;
}
.equipment-picker__choice:hover :deep(.equipment-card),
.equipment-picker__choice:focus-visible :deep(.equipment-card) {
  border-color: var(--color-secondary);
  transform: translateY(-0.2rem);
}
.equipment-picker__choice:hover :deep(.equipment-card__details),
.equipment-picker__choice:focus-visible :deep(.equipment-card__details) {
  grid-template-rows: 1fr;
}
.equipment-picker__choice:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
  outline-offset: 0.125rem;
}
@media (max-width: 42rem) {
  .equipment-picker__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
@media (max-width: 23rem) {
  .equipment-picker__grid {
    grid-template-columns: 1fr;
  }
}
@media (prefers-reduced-motion: reduce) {
  .equipment-picker__choice :deep(.equipment-card) {
    transition: none;
  }
}
</style>
