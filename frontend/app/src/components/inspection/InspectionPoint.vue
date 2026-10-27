<script setup lang="ts">
import type { InspectionResultDto } from "../../api/inspections";
import InspectionAnswerPhoto from "./InspectionAnswerPhoto.vue";

export type FindingValue = { id: string; observation: string; files: File[]; photoIds: string[] };
export type PointValue = { result: InspectionResultDto | null; findings: FindingValue[] };

const props = defineProps<{
  id: string;
  label: string;
  value: PointValue;
  inspectionId?: string | null;
  disabled?: boolean;
}>();
const emit = defineEmits<{
  update: [value: PointValue];
  removePhoto: [findingId: string, photoId: string];
}>();

function addFinding(): void {
  emit("update", {
    ...props.value,
    result: "NonCompliant",
    findings: [
      ...props.value.findings,
      { id: crypto.randomUUID(), observation: "", files: [], photoIds: [] },
    ],
  });
}
function updateFinding(id: string, patch: Partial<FindingValue>): void {
  emit("update", {
    ...props.value,
    findings: props.value.findings.map((finding) =>
      finding.id === id ? { ...finding, ...patch } : finding,
    ),
  });
}
function addFiles(id: string, event: Event): void {
  const input = event.target as HTMLInputElement;
  const finding = props.value.findings.find((item) => item.id === id);
  if (finding) updateFinding(id, { files: [...finding.files, ...Array.from(input.files ?? [])] });
  input.value = "";
}
</script>

<template>
  <fieldset
    class="point"
    :disabled="disabled"
  >
    <legend>{{ label }}</legend>

    <label>
      <input
        type="radio"
        :name="id"
        :checked="value.result === 'Compliant'"
        @change="emit('update', { result: 'Compliant', findings: [] })"
      />
      Conforme
    </label>
    <label>
      <input
        type="radio"
        :name="id"
        :checked="value.result === 'NonCompliant'"
        @change="emit('update', { ...value, result: 'NonCompliant' })"
      />
      Non conforme
    </label>
    <div
      v-if="value.result === 'NonCompliant'"
      class="point__findings"
    >
      <article
        v-for="(finding, index) in value.findings"
        :key="finding.id"
        class="point__finding"
      >
        <div class="point__heading">
          <h3>Défaut {{ index + 1 }}</h3>
          <button
            type="button"
            @click="
              emit('update', {
                ...value,
                findings: value.findings.filter((item) => item.id !== finding.id),
              })
            "
          >
            Supprimer ce défaut
          </button>
        </div>
        <label
          >Observation *
          <textarea
            :value="finding.observation"
            rows="3"
            required
            @input="
              updateFinding(finding.id, {
                observation: ($event.target as HTMLTextAreaElement).value,
              })
            "
          />
        </label>
        <label
          >Photographie *
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp"
            multiple
            @change="addFiles(finding.id, $event)"
          />
        </label>
        <ul v-if="finding.files.length || finding.photoIds.length">
          <li
            v-for="(file, fileIndex) in finding.files"
            :key="`${file.name}-${fileIndex}`"
          >
            {{ file.name }}
            <button
              type="button"
              @click="
                updateFinding(finding.id, {
                  files: finding.files.filter((_, i) => i !== fileIndex),
                })
              "
            >
              Retirer
            </button>
          </li>
          <li
            v-for="photoId in finding.photoIds"
            :key="photoId"
          >
            <InspectionAnswerPhoto
              v-if="inspectionId"
              :inspection-id="inspectionId"
              :photo-id="photoId"
            />
            <button
              type="button"
              @click="emit('removePhoto', finding.id, photoId)"
            >
              Supprimer la photo
            </button>
          </li>
        </ul>
      </article>
      <button
        type="button"
        @click="addFinding"
      >
        Ajouter un défaut
      </button>
    </div>
  </fieldset>
</template>

<style scoped lang="scss">
.point,
.point__findings,
.point__finding {
  display: grid;
  gap: 0.8rem;
  min-width: 0;
}
.point,
.point__finding {
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.65rem;
}
.point legend {
  font-weight: 600;
}
.point p {
  margin: 0;
  color: var(--color-text-secondary);
}
.point__heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.point__heading h3 {
  margin: 0;
}
.point__finding label {
  display: grid;
  gap: 0.35rem;
}
.point__finding ul {
  list-style: none;
  padding: 0;
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.point__finding li {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
textarea,
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
  min-height: 2.75rem;
  padding: 0.55rem 0.85rem;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  font: inherit;
  cursor: pointer;
}
button:focus-visible,
textarea:focus-visible,
input:focus-visible {
  outline: 3px solid var(--color-focus);
}
</style>
