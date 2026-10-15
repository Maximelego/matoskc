<script setup lang="ts">
import BaseIcon from "../common/icon/BaseIcon.vue";
import TemplatePointPhoto from "./TemplatePointPhoto.vue";

export interface EditablePoint {
  key: number;
  description: string;
  descriptionDetail: string;
  photoId: string | null;
}

const props = defineProps<{ points: EditablePoint[]; disabled: boolean }>();
const emit = defineEmits<{ change: [points: EditablePoint[]]; busy: [value: boolean] }>();
let nextKey = 1;

function add() {
  emit("change", [
    ...props.points,
    { key: Date.now() + nextKey++, description: "", descriptionDetail: "", photoId: null },
  ]);
}

function remove(index: number) {
  emit(
    "change",
    props.points.filter((_, i) => i !== index),
  );
}

function move(index: number, direction: number) {
  const copy = [...props.points];
  const target = index + direction;

  if (target < 0 || target >= copy.length) return;
  [copy[index], copy[target]] = [copy[target]!, copy[index]!];
  emit("change", copy);
}
function insertTab(event: KeyboardEvent, point: EditablePoint) {
  if (event.key !== "Tab" || event.shiftKey) return;
  event.preventDefault();
  const input = event.target as HTMLTextAreaElement;
  const start = input.selectionStart;
  point.descriptionDetail =
    point.descriptionDetail.slice(0, start) +
    "\t" +
    point.descriptionDetail.slice(input.selectionEnd);
  requestAnimationFrame(() => {
    input.selectionStart = input.selectionEnd = start + 1;
  });
}
</script>

<template>
  <fieldset
    class="points"
    :disabled="disabled"
  >
    <legend>Points de contrôle</legend>
    <p class="hint">
      Chaque point deviendra une question lors de l’état des lieux. Leur ordre est conservé.
    </p>
    <div
      v-for="(point, index) in points"
      :key="point.key"
      class="point"
    >
      <label :for="`template-point-${point.key}`">Point {{ index + 1 }}</label>
      <div class="point__row">
        <div class="point__inputs">
          <input
            :id="`template-point-${point.key}`"
            v-model="point.description"
            required
            maxlength="500"
            placeholder="Ex. Vérifier l’état du câble"
          />
          <label :for="`template-point-detail-${point.key}`">Détail du point</label>
          <textarea
            :id="`template-point-detail-${point.key}`"
            v-model="point.descriptionDetail"
            class="point__details"
            required
            rows="5"
            placeholder="Décrivez les contrôles à effectuer…"
            @keydown="insertTab($event, point)"
          ></textarea>
          <TemplatePointPhoto
            :photo-id="point.photoId"
            :disabled="disabled"
            @change="point.photoId = $event"
            @busy="emit('busy', $event)"
          />
        </div>
        <div class="point__buttons">
          <button
            type="button"
            :disabled="index === 0"
            :aria-label="`Monter le point ${index + 1}`"
            @click="move(index, -1)"
          >
            <BaseIcon name="chevron-up" />
          </button>
          <button
            type="button"
            :disabled="index === points.length - 1"
            :aria-label="`Descendre le point ${index + 1}`"
            @click="move(index, 1)"
          >
            <BaseIcon name="chevron-down" />
          </button>
          <button
            type="button"
            :disabled="points.length === 1"
            :aria-label="`Supprimer le point ${index + 1}`"
            @click="remove(index)"
          >
            <BaseIcon name="delete" />
          </button>
        </div>
      </div>
    </div>
    <button
      type="button"
      class="add"
      @click="add"
    >
      + Ajouter un point
    </button>
  </fieldset>
</template>

<style scoped>
.points {
  border: 0;
  padding: 0;
  margin: 0;
  min-width: 0;
  display: grid;
  gap: 1rem;
}
legend {
  font-size: 1.2rem;
  font-weight: 700;
  margin-bottom: 0.4rem;
}
.hint {
  margin: 0;
  color: var(--color-text-secondary);
}
.point {
  display: grid;
  gap: 0.35rem;
}
.point label {
  font-weight: 600;
}
.point__row {
  display: flex;
  flex-direction: row;
  gap: 0.5rem;

  .point__inputs {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    flex: 1;
    min-width: 0;

    .point__details {
      min-height: 7rem;
      resize: vertical;
      tab-size: 4;
      white-space: pre-wrap;
    }
  }

  .point__buttons {
    display: flex;
    flex-direction: row;
    gap: 0.35rem;
    max-height: 3rem;
  }
}
.point__row input,
.point__row textarea {
  flex: 1;
  min-width: 0;
  padding: 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.4rem;
  background: var(--color-input-background);
  color: inherit;
}
button {
  border: 1px solid var(--color-border);
  border-radius: 0.4rem;
  padding: 0.55rem 0.75rem;
  background: var(--color-surface);
  color: inherit;
  cursor: pointer;
}
button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.add {
  justify-self: start;
  color: var(--color-primary);
}
@media (max-width: 42rem) {
  .point__row {
    flex-wrap: wrap;
  }
  .point__row input,
  .point__row textarea {
    flex-basis: 100%;
  }
}
</style>
