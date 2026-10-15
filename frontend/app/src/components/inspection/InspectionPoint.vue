<script setup lang="ts">
import type { InspectionResultDto } from "../../api/inspections";
import InspectionAnswerPhoto from "./InspectionAnswerPhoto.vue";
export type PointValue = { result: InspectionResultDto | null; observation: string; files: File[]; photoIds: string[] };
const props = defineProps<{ id: string; label: string; value: PointValue; inspectionId?: string | null; disabled?: boolean }>();
const emit = defineEmits<{ update: [value: PointValue]; removePhoto: [photoId: string] }>();
function update(patch: Partial<PointValue>): void { emit("update", { ...props.value, ...patch }); }
function addFiles(event: Event): void {
  const input = event.target as HTMLInputElement;
  update({ files: [...props.value.files, ...Array.from(input.files ?? [])] });
  input.value = "";
}
</script>
<template>
  <fieldset class="point" :disabled="disabled">
    <legend>{{ label }}</legend>
    <label><input type="radio" :name="id" :checked="value.result === 'Compliant'"
      @change="update({ result: 'Compliant', observation: '' })" /> Conforme</label>
    <label><input type="radio" :name="id" :checked="value.result === 'NonCompliant'"
      @change="update({ result: 'NonCompliant' })" /> Non conforme</label>
    <div v-if="value.result === 'NonCompliant'" class="point__defect">
      <label>Observation *<textarea :value="value.observation" rows="3"
        @input="update({ observation: ($event.target as HTMLTextAreaElement).value })" /></label>
      <label>Photographies *<input type="file" accept="image/jpeg,image/png,image/webp" multiple @change="addFiles" /></label>
      <ul v-if="value.files.length || value.photoIds.length">
        <li v-for="(file, index) in value.files" :key="`${file.name}-${index}`">
          {{ file.name }} <button type="button" @click="update({ files: value.files.filter((_, i) => i !== index) })">Retirer</button>
        </li>
        <li v-for="photoId in value.photoIds" :key="photoId">
          <InspectionAnswerPhoto v-if="inspectionId" :inspection-id="inspectionId" :photo-id="photoId" />
          Photo enregistrée <button type="button" @click="emit('removePhoto', photoId)">Retirer</button>
        </li>
      </ul>
    </div>
  </fieldset>
</template>
<style scoped lang="scss">
.point { display: grid; gap: .7rem; padding: 1rem; border: 1px solid var(--color-border); border-radius: .65rem; }
.point legend { padding: 0 .35rem; font-weight: 600; }
.point label { display: block; }
.point__defect { display: grid; gap: .7rem; }
.point__defect label { display: grid; gap: .35rem; }
textarea, input[type="file"] { box-sizing: border-box; width: 100%; padding: .55rem; color: var(--color-text);
  background: var(--color-input-background); border: 1px solid var(--color-border); border-radius: .5rem; font: inherit; }
button { color: var(--color-primary); background: none; border: 0; cursor: pointer; }
</style>
