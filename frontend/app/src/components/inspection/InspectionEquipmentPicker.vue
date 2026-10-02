<script setup lang="ts">
import { computed, ref } from "vue";
import type { InspectionEquipmentDto } from "../../api/inspections/dto";
import BaseIcon from "../common/icon/BaseIcon.vue";

const props = defineProps<{ equipment: readonly InspectionEquipmentDto[]; selectedId?: string }>();
const emit = defineEmits<{ select: [equipment: InspectionEquipmentDto] }>();
const search = ref("");
const matches = computed(() => props.equipment.filter((item) =>
  `${item.name} ${item.serialNumber}`.toLocaleLowerCase("fr").includes(search.value.trim().toLocaleLowerCase("fr")),
));
</script>
<template>
  <div class="inspection-equipment-picker">
    <label for="inspection-equipment-search">Rechercher un matériel disponible</label>
    <input id="inspection-equipment-search" v-model="search" type="search" placeholder="Nom ou numéro de série" />
    <p v-if="!matches.length">Aucun matériel disponible ne correspond à votre recherche.</p>
    <div class="inspection-equipment-picker__grid">
      <button
        v-for="item in matches" :key="item.id" type="button"
        class="inspection-equipment-picker__card"
        :class="{ 'inspection-equipment-picker__card--selected': selectedId === item.id }"
        :aria-pressed="selectedId === item.id"
        @click="emit('select', item)"
      >
        <span class="inspection-equipment-picker__photo">
          <img v-if="item.photoUrl" :src="item.photoUrl" :alt="`Photographie de ${item.name}`" />
          <span v-else class="inspection-equipment-picker__placeholder"><BaseIcon name="equipment" :size="44" /><small>Photo à venir</small></span>
        </span>
        <strong>{{ item.name }}</strong>
        <small>N° {{ item.serialNumber }}</small>
        <span v-if="selectedId === item.id" class="inspection-equipment-picker__selected">Sélectionné</span>
      </button>
    </div>
  </div>
</template>
<style scoped lang="scss">
.inspection-equipment-picker { display: grid; gap: 0.75rem; min-width: 0; }
.inspection-equipment-picker > input { box-sizing: border-box; width: 100%; min-height: 2.75rem; padding: 0.65rem; border: 1px solid var(--color-border); border-radius: 0.5rem; color: var(--color-text); background: var(--color-input-background); }
.inspection-equipment-picker__grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(100%, 13rem), 1fr)); gap: 0.75rem; }
.inspection-equipment-picker__card { display: grid; gap: 0.35rem; min-width: 0; padding: 0.65rem; text-align: left; color: var(--color-text); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 0.75rem; cursor: pointer; }
.inspection-equipment-picker__card--selected { border-color: var(--color-primary); background: var(--color-primary-soft); }
.inspection-equipment-picker__card:focus-visible { outline: 0.1875rem solid var(--color-focus); outline-offset: 0.125rem; }
.inspection-equipment-picker__photo { display: grid; place-items: center; width: 100%; aspect-ratio: 4 / 3; overflow: hidden; border-radius: 0.5rem; color: var(--color-text-secondary); background: var(--color-surface-secondary); }
.inspection-equipment-picker__placeholder { display: grid; place-items: center; gap: 0.25rem; }
.inspection-equipment-picker__photo img { width: 100%; height: 100%; object-fit: cover; }
.inspection-equipment-picker__selected { color: var(--color-primary); font-weight: var(--font-weight-semibold); }
@media (max-width: 42rem) { .inspection-equipment-picker__grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 23rem) { .inspection-equipment-picker__grid { grid-template-columns: 1fr; } }
</style>
