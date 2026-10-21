<script setup lang="ts">
import { computed } from "vue";
import type { DefectDto } from "../../api/defects/dto";
import type { InspectionResultDto } from "../../api/inspections";
import BaseIcon from "../common/icon/BaseIcon.vue";
import InspectionAnswerPhoto from "./InspectionAnswerPhoto.vue";

export type PointValue = {
  result: InspectionResultDto | null;
  observation: string;
  files: File[];
  photoIds: string[];
  existingDefectId: string | null;
};

const props = defineProps<{
  id: string;
  label: string;
  value: PointValue;
  openDefects?: DefectDto[];
  inspectionId?: string | null;
  disabled?: boolean;
}>();

const matchingDefects = computed(
  () =>
    props.openDefects?.filter(
      (defect) =>
        defect.description.trim().toLocaleLowerCase("fr") ===
        props.label.trim().toLocaleLowerCase("fr"),
    ) ?? [],
);

const emit = defineEmits<{ update: [value: PointValue]; removePhoto: [photoId: string] }>();

function update(patch: Partial<PointValue>): void {
  emit("update", { ...props.value, ...patch });
}

function addFiles(event: Event): void {
  const input = event.target as HTMLInputElement;
  update({ files: [...props.value.files, ...Array.from(input.files ?? [])] });
  input.value = "";
}
</script>

<template>
  <fieldset
    class="point"
    :disabled="disabled"
  >
    <legend>{{ label }}</legend>
    <p
      v-if="matchingDefects.length"
      class="point__warning"
      role="status"
    >
      {{ matchingDefects.length }} défaut(s) encore ouvert(s) pour ce point. Indiquez « Non conforme
      » jusqu’à leur résolution lors d’une maintenance.
    </p>
    <label
      ><input
        type="radio"
        :name="id"
        :checked="value.result === 'Compliant'"
        :disabled="disabled || Boolean(matchingDefects.length)"
        @change="update({ result: 'Compliant', observation: '', existingDefectId: null })"
      />
      Conforme</label
    >
    <label
      ><input
        type="radio"
        :name="id"
        :checked="value.result === 'NonCompliant'"
        @change="
          update({
            result: 'NonCompliant',
            existingDefectId: value.existingDefectId ?? matchingDefects[0]?.id ?? null,
          })
        "
      />
      Non conforme</label
    >
    <div
      v-if="value.result === 'NonCompliant'"
      class="point__defect"
    >
      <label v-if="openDefects?.length"
        >Défaut déjà constaté (facultatif)
        <select
          :value="value.existingDefectId ?? ''"
          @change="update({ existingDefectId: ($event.target as HTMLSelectElement).value || null })"
        >
          <option value="">Nouveau défaut</option>
          <option
            v-for="defect in openDefects"
            :key="defect.id"
            :value="defect.id"
          >
            {{ defect.description }} - {{ defect.observation }}
          </option>
        </select>
      </label>
      <label
        >Observation(s) *<textarea
          :value="value.observation"
          rows="3"
          @input="update({ observation: ($event.target as HTMLTextAreaElement).value })"
        />
      </label>
      <label
        >Photographies *<input
          type="file"
          accept="image/jpeg,image/png,image/webp"
          multiple
          @change="addFiles"
      /></label>
      <ul v-if="value.files.length || value.photoIds.length">
        <li
          v-for="(file, index) in value.files"
          :key="`${file.name}-${index}`"
        >
          {{ file.name }}
          <button
            type="button"
            @click="update({ files: value.files.filter((_, i) => i !== index) })"
          >
            <BaseIcon
              name="delete"
              :size="24"
            />
          </button>
        </li>
        <li
          v-for="photoId in value.photoIds"
          :key="photoId"
        >
          <InspectionAnswerPhoto
            v-if="inspectionId"
            :inspection-id="inspectionId"
            :photo-id="photoId"
          />
          Photo enregistrée
          <button
            type="button"
            @click="emit('removePhoto', photoId)"
          >
            <BaseIcon
              name="delete"
              :size="24"
            />
          </button>
        </li>
      </ul>
    </div>
  </fieldset>
</template>

<style scoped lang="scss">
.point {
  display: grid;
  gap: 0.7rem;
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.65rem;
}
.point legend {
  padding: 0 0.35rem;
  font-weight: 600;
}
.point label {
  display: block;
}
.point__defect {
  display: grid;
  gap: 0.7rem;
}
.point__defect label {
  display: grid;
  gap: 0.35rem;
}
.point__defect ul {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  width: 50%;
  flex-wrap: wrap;

  li {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
  }
}
.point__warning {
  margin: 0;
  padding: 0.7rem;
  border-radius: 0.5rem;
  background: var(--color-warning-soft);
}
textarea,
select,
input[type="file"] {
  box-sizing: border-box;
  width: 100%;
  padding: 0.55rem;
  color: var(--color-text);
  background: var(--color-input-background);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  font: inherit;
}
button {
  color: var(--color-danger);

  background: none;
  border: 0;
  cursor: pointer;

  &:hover {
    svg {
      scale: 1.1;
      transition: scale 150ms ease-in-out;
    }
  }
}
</style>
