<script setup lang="ts">
import type { DefectDto, DefectStatus } from "../../api/defects/dto";
import MaintenanceDefectCard from "./MaintenanceDefectCard.vue";

const props = defineProps<{
  defects: DefectDto[];
  status: DefectStatus;
  selected: string[];
  equipmentId: string;
  total: number;
  offset: number;
  limit: number;
  loading: boolean;
  disabled: boolean;
}>();
const emit = defineEmits<{
  "update:status": [status: DefectStatus];
  "update:selected": [ids: string[]];
  page: [offset: number];
}>();
function select(id: string, checked: boolean): void {
  emit(
    "update:selected",
    checked ? [...props.selected, id] : props.selected.filter((item) => item !== id),
  );
}
</script>

<template>
  <section
    class="defects"
    aria-labelledby="maintenance-defects-title"
  >
    <header class="defects__header">
      <div>
        <p class="defects__eyebrow">État du matériel</p>
        <h2 id="maintenance-defects-title">Défauts constatés</h2>
      </div>
      <label class="defects__filter"
        >Afficher
        <select
          :value="status"
          :disabled="disabled"
          @change="
            emit('update:status', ($event.target as HTMLSelectElement).value as DefectStatus)
          "
        >
          <option value="Open">Défauts ouverts</option>
          <option value="Resolved">Défauts résolus</option>
        </select>
      </label>
    </header>
    <p
      v-if="!loading && !defects.length"
      class="defects__empty"
    >
      Aucun défaut {{ status === "Open" ? "ouvert" : "résolu" }} pour cet équipement.
    </p>
    <div class="defects__list">
      <MaintenanceDefectCard
        v-for="defect in defects"
        :key="defect.id"
        :defect="defect"
        :equipment-id="equipmentId"
        :selected="selected.includes(defect.id)"
        :disabled="disabled"
        @select="select"
      />
    </div>
    <nav
      v-if="total > limit || offset"
      class="defects__pagination"
      aria-label="Pages des défauts"
    >
      <button
        type="button"
        :disabled="loading || !offset"
        @click="emit('page', offset - limit)"
      >
        Précédent
      </button>
      <span>{{ offset + 1 }}–{{ Math.min(offset + limit, total) }} / {{ total }}</span>
      <button
        type="button"
        :disabled="loading || offset + limit >= total"
        @click="emit('page', offset + limit)"
      >
        Suivant
      </button>
    </nav>
  </section>
</template>

<style scoped lang="scss">
.defects {
  display: grid;
  gap: 1rem;
  min-width: 0;
}
.defects__header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}
.defects__header h2,
.defects__eyebrow {
  margin: 0;
}
.defects__eyebrow {
  margin-bottom: 0.25rem;
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
  font-weight: 700;
}
.defects__filter {
  display: grid;
  gap: 0.35rem;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
.defects__filter select {
  box-sizing: border-box;
  min-height: 2.75rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-input-background);
  color: var(--color-text);
  font: inherit;
}
.defects__list {
  display: grid;
  gap: 0.7rem;
}
.defects__empty {
  margin: 0;
  padding: 1.4rem;
  border: 1px dashed var(--color-border);
  border-radius: 0.75rem;
  color: var(--color-text-secondary);
}
.defects__pagination {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  flex-wrap: wrap;
}
.defects__pagination button {
  min-height: 2.75rem;
  padding: 0.5rem 0.8rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface);
  color: var(--color-text);
  cursor: pointer;
}
.defects__pagination button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
@media (max-width: 42rem) {
  .defects__filter,
  .defects__filter select {
    width: 100%;
  }
}
</style>
