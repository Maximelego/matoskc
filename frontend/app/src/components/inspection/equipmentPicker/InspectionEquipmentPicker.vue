<script setup lang="ts">
import { computed, ref } from "vue";
import type { EquipmentDto } from "../../../api/equipments/dto.js";
import EquipmentCard from "./EquipmentCard.vue";

const props = defineProps<{ equipment: EquipmentDto[] }>();
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
  <div class="inspection-picker">
    <label for="inspection-search">Rechercher un équipement</label>
    <input
      id="inspection-search"
      v-model="search"
      type="search"
      placeholder="Nom ou numéro de série"
    />
    <p v-if="!matches.length">Aucun équipement ne correspond à votre recherche.</p>
    <div class="inspection-picker__grid">
      <RouterLink
        v-for="item in matches"
        :key="item.id"
        :to="`/inspections/${item.id}`"
        class="inspection-picker__card"
      >
        <EquipmentCard :equipment="item" />
      </RouterLink>
    </div>
  </div>
</template>

<style scoped lang="scss">
.inspection-picker {
  display: grid;
  gap: 0.75rem;
  min-width: 0;
}

.inspection-picker > input {
  box-sizing: border-box;
  width: 100%;
  min-height: 2.75rem;
  padding: 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-input-background);
}

.inspection-picker__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 13rem), 1fr));
  gap: 0.75rem;
}

.inspection-picker__card {
  text-decoration: none;
  color: var(--color-text);
}

@media (max-width: 42rem) {
  .inspection-picker__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
@media (max-width: 23rem) {
  .inspection-picker__grid {
    grid-template-columns: 1fr;
  }
}
</style>
