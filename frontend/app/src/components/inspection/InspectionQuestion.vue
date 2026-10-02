<script setup lang="ts">
import { computed } from "vue";
import type { InspectionAnswer, InspectionQuestion, PreviousDefect } from "../../api/inspections/dto";
import { emptyAnswer } from "./inspectionValidation";

const props = defineProps<{ question: InspectionQuestion; answer?: InspectionAnswer; errors?: string[]; previousDefect?: PreviousDefect }>();
const emit = defineEmits<{ update: [answer: InspectionAnswer] }>();
const current = computed(() => props.answer ?? emptyAnswer(props.question));
const condition = computed(() => current.value.kind === "condition" ? current.value : null);
const text = computed(() => current.value.kind === "text" ? current.value : null);
const number = computed(() => current.value.kind === "number" ? current.value : null);
const fieldId = computed(() => `inspection-${props.question.id}`);

function setCondition(value: "compliant" | "nonCompliant"): void {
  if (!condition.value) return;
  emit("update", { ...condition.value, value,
    observation: value === "compliant" ? "" : condition.value.observation,
    photos: value === "compliant" && props.question.kind === "condition" && props.question.photoRequiredWhen !== "always" ? [] : condition.value.photos });
}
function setObservation(value: string): void {
  if (condition.value) emit("update", { ...condition.value, observation: value });
}
function addPhotos(event: Event): void {
  if (!condition.value) return;
  const input = event.target as HTMLInputElement;
  const files = Array.from(input.files ?? []).filter((file) => file.type.startsWith("image/"));
  emit("update", { ...condition.value, photos: [...condition.value.photos, ...files] });
  input.value = "";
}
function removePhoto(index: number): void {
  if (condition.value) emit("update", { ...condition.value, photos: condition.value.photos.filter((_, i) => i !== index) });
}
function setNumber(value: string): void {
  if (number.value) emit("update", { ...number.value, value: value === "" ? null : Number(value) });
}
</script>

<template>
  <div class="inspection-question" :class="{ 'inspection-question--invalid': errors?.length }">
    <aside v-if="previousDefect" class="inspection-question__prior">
      <strong>Défaut préexistant</strong><p>{{ previousDefect.observation }}</p>
      <a v-for="url in previousDefect.photoUrls" :key="url" :href="url" target="_blank" rel="noopener noreferrer">Voir la photographie du constat précédent</a>
    </aside>
    <fieldset v-if="question.kind === 'condition' && condition">
      <legend>{{ question.label }} <span v-if="question.required" aria-label="obligatoire">*</span></legend>
      <p v-if="question.help" class="inspection-question__help">{{ question.help }}</p>
      <div class="inspection-question__choices">
        <label><input type="radio" :name="fieldId" :checked="condition.value === 'compliant'" @change="setCondition('compliant')" /> Conforme</label>
        <label><input type="radio" :name="fieldId" :checked="condition.value === 'nonCompliant'" @change="setCondition('nonCompliant')" /> Non conforme</label>
      </div>
      <div v-if="condition.value === 'nonCompliant'" class="inspection-question__detail">
        <label :for="`${fieldId}-observation`">Description du défaut *</label>
        <textarea :id="`${fieldId}-observation`" :value="condition.observation" rows="3" required @input="setObservation(($event.target as HTMLTextAreaElement).value)" />
      </div>
      <div v-if="condition.value === 'nonCompliant' || question.photoRequiredWhen === 'always'" class="inspection-question__detail">
        <label :for="`${fieldId}-photo`">Photographie {{ question.photoRequiredWhen === 'always' || condition.value === 'nonCompliant' ? '*' : '' }}</label>
        <input :id="`${fieldId}-photo`" type="file" accept="image/*" capture="environment" multiple @change="addPhotos" />
        <ul v-if="condition.photos.length" class="inspection-question__photos">
          <li v-for="(photo, index) in condition.photos" :key="`${photo.name}-${index}`">
            <span>{{ photo.name }}</span>
            <button type="button" :aria-label="`Retirer ${photo.name}`" @click="removePhoto(index)">Retirer</button>
          </li>
        </ul>
      </div>
    </fieldset>
    <div v-else-if="question.kind === 'text' && text" class="inspection-question__detail">
      <label :for="fieldId">{{ question.label }} <span v-if="question.required" aria-label="obligatoire">*</span></label>
      <p v-if="question.help" class="inspection-question__help">{{ question.help }}</p>
      <textarea :id="fieldId" :value="text.value" :maxlength="question.maxLength" rows="3" @input="emit('update', { ...text, value: ($event.target as HTMLTextAreaElement).value })" />
    </div>
    <div v-else-if="question.kind === 'number' && number" class="inspection-question__detail">
      <label :for="fieldId">{{ question.label }} <span v-if="question.required" aria-label="obligatoire">*</span></label>
      <div class="inspection-question__number">
        <input :id="fieldId" type="number" inputmode="decimal" :min="question.min" :max="question.max" :value="number.value ?? ''" @input="setNumber(($event.target as HTMLInputElement).value)" />
        <span v-if="question.unit">{{ question.unit }}</span>
      </div>
    </div>
    <ul v-if="errors?.length" class="inspection-question__errors" role="alert">
      <li v-for="message in errors" :key="message">{{ message }}</li>
    </ul>
  </div>
</template>

<style scoped lang="scss">
.inspection-question {
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
  &__prior { padding: .75rem; margin-bottom: .75rem; border-radius: .5rem; background: var(--color-warning-soft); }
  &__prior p { margin: .35rem 0; }
  &--invalid { border-color: var(--color-danger); }
  fieldset { margin: 0; padding: 0; border: 0; min-width: 0; }
  legend, label { font-weight: var(--font-weight-semibold); }
  &__help { margin: 0.35rem 0; color: var(--color-text-secondary); }
  &__choices { display: flex; flex-wrap: wrap; gap: 0.75rem; margin-top: 0.75rem; }
  &__choices label { display: flex; align-items: center; gap: 0.5rem; min-height: 2.75rem; padding: 0.5rem 0.75rem; border: 1px solid var(--color-border); border-radius: 0.5rem; cursor: pointer; }
  &__detail { display: grid; gap: 0.5rem; margin-top: 0.75rem; min-width: 0; }
  textarea, input[type="number"], input[type="file"] { box-sizing: border-box; width: 100%; min-width: 0; padding: 0.65rem; color: var(--color-text); background: var(--color-input-background); border: 1px solid var(--color-border); border-radius: 0.5rem; font: inherit; }
  input[type="number"] { min-height: 2.75rem; }
  &__number { display: flex; align-items: center; gap: 0.5rem; }
  &__photos { padding-left: 1.25rem; overflow-wrap: anywhere; }
  &__photos li { margin-block: 0.4rem; }
  &__photos button { margin-left: 0.5rem; color: var(--color-primary); border: 0; background: none; cursor: pointer; }
  &__errors { margin-bottom: 0; padding-left: 1.25rem; color: var(--color-danger); }
}
@media (max-width: 42rem) {
  .inspection-question__choices { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
}
@media (max-width: 23rem) { .inspection-question__choices { grid-template-columns: 1fr; } }
</style>
