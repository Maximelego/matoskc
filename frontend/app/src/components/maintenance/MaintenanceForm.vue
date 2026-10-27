<script setup lang="ts">
import { computed, ref } from "vue";
import type { DefectDto } from "../../api/defects/dto";
import type { RecordMaintenanceDto } from "../../api/maintenance/dto";
import BaseInput from "../common/input/BaseInput.vue";

const props = defineProps<{ selected: string[]; defects: DefectDto[]; disabled: boolean }>();
const emit = defineEmits<{ submit: [fields: Omit<RecordMaintenanceDto, "id">] }>();
const performedAt = ref("");
const performedBy = ref("");
const workDescription = ref("");
const attempted = ref(false);
const dateError = computed(() => {
  if (!attempted.value) return undefined;
  const value = new Date(performedAt.value).getTime();
  if (!performedAt.value || !Number.isFinite(value))
    return "La date d’intervention est obligatoire.";
  if (
    props.selected.some((id) => {
      const defect = props.defects.find((item) => item.id === id);
      return !defect || value < new Date(defect.reportedAt).getTime();
    })
  )
    return "La date doit suivre le constat des défauts sélectionnés.";
  return undefined;
});
const personError = computed(() =>
  attempted.value && !performedBy.value.trim() ? "L’intervenant est obligatoire." : undefined,
);
const descriptionError = computed(() =>
  attempted.value && !workDescription.value.trim() ? "Décrivez les travaux effectués." : undefined,
);
function submit(): void {
  attempted.value = true;
  if (
    dateError.value ||
    personError.value ||
    descriptionError.value ||
    performedBy.value.trim().length > 200 ||
    workDescription.value.trim().length > 10_000
  )
    return;
  emit("submit", {
    performedAt: new Date(performedAt.value).toISOString(),
    performedBy: performedBy.value.trim(),
    workDescription: workDescription.value.trim(),
    resolvedDefectIds: [...props.selected].sort(),
  });
}
function reset(): void {
  workDescription.value = "";
  attempted.value = false;
}
defineExpose({ reset });
</script>

<template>
  <form
    class="form"
    novalidate
    @submit.prevent="submit"
  >
    <header>
      <p class="form__eyebrow">Intervention</p>
      <h2>Enregistrer une maintenance</h2>
    </header>
    <p class="form__hint">
      Une intervention peut être prévue à une date future et enregistrée sans résoudre de défaut. La
      résolution ne modifie pas automatiquement la disponibilité du matériel.
    </p>
    <div class="form__fields">
      <BaseInput
        id="care-date"
        v-model="performedAt"
        label="Date et heure de l’intervention"
        :error="dateError"
        type="datetime-local"
        required
        :disabled="disabled"
      />
      <BaseInput
        id="care-person"
        v-model="performedBy"
        label="Intervenant"
        :error="personError"
        maxlength="200"
        required
        :disabled="disabled"
      />
    </div>
    <label for="care-work">Travaux effectués *</label>
    <textarea
      id="care-work"
      v-model="workDescription"
      rows="5"
      maxlength="10000"
      required
      :disabled="disabled"
      :aria-invalid="!!descriptionError"
      :aria-describedby="descriptionError ? 'care-work-error' : undefined"
    />
    <p
      v-if="descriptionError"
      id="care-work-error"
      class="form__error"
      role="alert"
    >
      {{ descriptionError }}
    </p>
    <footer class="form__footer">
      <span class="form__count"
        >{{ selected.length }} défaut(s) sélectionné(s) pour résolution</span
      >
      <button
        type="submit"
        :disabled="disabled"
      >
        {{ disabled ? "Enregistrement…" : "Enregistrer la maintenance" }}
      </button>
    </footer>
  </form>
</template>

<style scoped lang="scss">
.form {
  display: grid;
  gap: 0.9rem;
  min-width: 0;
  padding: 1.4rem;
  border: 1px solid var(--color-border);
  border-radius: 0.85rem;
  background: var(--color-surface);
}
.form h2,
.form p {
  margin: 0;
}
.form__eyebrow {
  margin-bottom: 0.25rem !important;
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
  font-weight: 700;
}
.form__hint {
  color: var(--color-text-secondary);
  line-height: 1.5;
}
.form__fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.9rem;
}
.form textarea {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  padding: 0.7rem;
  border: 1px solid var(--color-border);
  border-radius: 0.55rem;
  background: var(--color-input-background);
  color: var(--color-text);
  font: inherit;
  resize: vertical;
}
.form textarea[aria-invalid="true"] {
  border-color: var(--color-danger);
}
.form__error {
  color: var(--color-danger);
}
.form__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.8rem;
  flex-wrap: wrap;
}
.form__count {
  padding: 0.5rem 0.7rem;
  border-radius: 0.5rem;
  color: var(--color-secondary);
  background: var(--color-surface-secondary);
}
.form button {
  min-height: 2.75rem;
  padding: 0.65rem 1rem;
  border: 0;
  border-radius: 0.5rem;
  background: var(--color-primary);
  color: var(--color-text-on-primary);
  font: inherit;
  font-weight: 600;
  cursor: pointer;
}
.form button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
@media (max-width: 42rem) {
  .form {
    padding: 1rem;
  }
  .form__fields {
    grid-template-columns: 1fr;
  }
  .form__footer button {
    width: 100%;
  }
}
</style>
