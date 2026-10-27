<script setup lang="ts">
import TemplatePointCard from "./TemplatePointCard.vue";
export interface EditablePoint {
  key: number;
  description: string;
  descriptionDetail: string;
  photoId: string | null;
}
const props = defineProps<{
  points: EditablePoint[];
  disabled: boolean;
  validationRequested: boolean;
  agencyId: string;
}>();
const emit = defineEmits<{ change: [points: EditablePoint[]]; busy: [value: boolean] }>();
let nextKey = 1;
function add(): void {
  emit("change", [
    ...props.points,
    { key: Date.now() + nextKey++, description: "", descriptionDetail: "", photoId: null },
  ]);
}
function update(index: number, point: EditablePoint): void {
  emit(
    "change",
    props.points.map((item, i) => (i === index ? point : item)),
  );
}
function remove(index: number): void {
  emit(
    "change",
    props.points.filter((_, i) => i !== index),
  );
}
function move(index: number, direction: number): void {
  const target = index + direction;
  if (target < 0 || target >= props.points.length) return;
  const copy = [...props.points];
  [copy[index], copy[target]] = [copy[target]!, copy[index]!];
  emit("change", copy);
}
</script>

<template>
  <fieldset
    class="points"
    :disabled="disabled"
  >
    <legend>Points de contrôle</legend>
    <p>Chaque point deviendra une question lors de l’état des lieux. Leur ordre est conservé.</p>
    <TemplatePointCard
      v-for="(point, index) in points"
      :key="point.key"
      :point="point"
      :index="index"
      :total="points.length"
      :agency-id="agencyId"
      :disabled="disabled"
      :validation-requested="validationRequested"
      @change="update(index, $event)"
      @move="move(index, $event)"
      @remove="remove(index)"
      @busy="emit('busy', $event)"
    />
    <button
      class="points__add"
      type="button"
      :disabled="disabled"
      @click="add"
    >
      + Ajouter un point
    </button>
  </fieldset>
</template>

<style scoped lang="scss">
.points {
  display: grid;
  gap: 1rem;
  min-width: 0;
  margin: 0;
  padding: 0;
  border: 0;
}
.points legend {
  margin-bottom: 0.35rem;
  font-size: 1.2rem;
  font-weight: 700;
}
.points p {
  margin: 0;
  color: var(--color-text-secondary);
}
.points__add {
  justify-self: start;
  min-height: 2.75rem;
  padding: 0.6rem 0.9rem;
  border: 1px solid var(--color-secondary);
  border-radius: 0.5rem;
  background: var(--color-surface);
  color: var(--color-secondary);
  font: inherit;
  font-weight: 600;
  cursor: pointer;
}
.points__add:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
@media (max-width: 42rem) {
  .points__add {
    width: 100%;
  }
}
</style>
