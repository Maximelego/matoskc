<script setup lang="ts">
import type { KnownDefectDto, KnownDefectResultDto } from "../../../api/inspections";
import InspectionAnswerPhoto from "../InspectionAnswerPhoto.vue";

export type KnownDefectValue = KnownDefectDto & { files: File[] };
const props = defineProps<{
  defects: KnownDefectValue[];
  operatorNames: Record<string, string>;
  inspectionId: string | null;
  disabled: boolean;
}>();
const emit = defineEmits<{
  update: [defectId: string, patch: Partial<KnownDefectValue>];
  removePhoto: [defectId: string, photoId: string];
}>();
function select(defect: KnownDefectValue, result: KnownDefectResultDto): void {
  emit("update", defect.defectId, { result, observation: result === "Worsened" ? defect.observation : null });
}
function addFiles(defect: KnownDefectValue, event: Event): void {
  const input = event.target as HTMLInputElement;
  emit("update", defect.defectId, { files: [...defect.files, ...Array.from(input.files ?? [])] });
  input.value = "";
}
</script>

<template>
  <section class="known-step">
    <h2>Défauts déjà connus</h2>
    <p>Confirmez chaque défaut du matériel, indépendamment des points du questionnaire.</p>
    <p v-if="!defects.length">Aucun défaut ouvert à confirmer.</p>
    <article v-for="defect in defects" :key="defect.defectId" class="known-step__card">
      <h3>{{ defect.description }}</h3>
      <p>{{ defect.historicalObservation }}</p>
      <small>
        Constaté par {{ operatorNames[defect.defectId] ?? "opérateur non renseigné" }}
        le {{ new Date(defect.observedAt).toLocaleDateString('fr-FR') }}
      </small>
      <div class="known-step__photos">
        <template v-if="inspectionId">
          <InspectionAnswerPhoto v-for="photoId in defect.historicalPhotoIds" :key="photoId"
            :inspection-id="inspectionId" :photo-id="photoId" />
        </template>
      </div>
      <fieldset :disabled="disabled">
        <legend>Confirmation *</legend>
        <label><input type="radio" :name="defect.defectId" :checked="defect.result === 'Unchanged'"
          @change="select(defect, 'Unchanged')" /> Toujours présent, inchangé</label>
        <label><input type="radio" :name="defect.defectId" :checked="defect.result === 'Worsened'"
          @change="select(defect, 'Worsened')" /> Aggravé</label>
        <label><input type="radio" :name="defect.defectId" :checked="defect.result === 'MaintenanceCheckRequired'"
          @change="select(defect, 'MaintenanceCheckRequired')" /> À vérifier par la maintenance</label>
      </fieldset>
      <div v-if="defect.result === 'Worsened'" class="known-step__evidence">
        <label>Nouvelle observation *
          <textarea :value="defect.observation ?? ''" rows="3" :disabled="disabled" required
            @input="emit('update', defect.defectId, { observation: ($event.target as HTMLTextAreaElement).value })" />
        </label>
        <label>Nouvelle photographie *
          <input type="file" accept="image/jpeg,image/png,image/webp" multiple :disabled="disabled"
            @change="addFiles(defect, $event)" />
        </label>
        <ul v-if="defect.files.length || defect.photoIds.length">
          <li v-for="(file, index) in defect.files" :key="`${file.name}-${index}`">
            {{ file.name }} <button type="button" :disabled="disabled" @click="emit('update', defect.defectId, { files: defect.files.filter((_, i) => i !== index) })">Retirer</button>
          </li>
          <li v-for="photoId in defect.photoIds" :key="photoId">
            <InspectionAnswerPhoto v-if="inspectionId" :inspection-id="inspectionId" :photo-id="photoId" />
            <button type="button" :disabled="disabled" @click="emit('removePhoto', defect.defectId, photoId)">Supprimer la photo</button>
          </li>
        </ul>
      </div>
    </article>
  </section>
</template>

<style scoped lang="scss">
.known-step, .known-step__card, .known-step__evidence { display: grid; gap: 0.75rem; min-width: 0; }
.known-step__card { padding: 1rem; border: 1px solid var(--color-border); border-radius: 0.65rem; }
.known-step h2, .known-step h3, .known-step p { margin: 0; }
.known-step small { color: var(--color-text-secondary); }
.known-step__photos, .known-step__evidence ul { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.known-step fieldset, .known-step__evidence label { display: grid; gap: 0.5rem; }
.known-step__evidence ul { list-style: none; padding: 0; }
.known-step__evidence li { display: flex; align-items: center; gap: 0.5rem; }
textarea, input[type="file"] { box-sizing: border-box; width: 100%; padding: 0.55rem; color: var(--color-text); background: var(--color-input-background); border: 1px solid var(--color-border); border-radius: 0.5rem; font: inherit; }
button { min-height: 2.75rem; padding: 0.55rem; color: var(--color-text); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 0.5rem; cursor: pointer; }
</style>
