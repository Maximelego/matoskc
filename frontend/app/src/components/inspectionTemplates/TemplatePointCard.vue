<script setup lang="ts">
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseInput from "../common/input/BaseInput.vue";
import TemplatePointPhoto from "./TemplatePointPhoto.vue";
import type { EditablePoint } from "./TemplatePointsEditor.vue";
const props = defineProps<{
  point: EditablePoint;
  index: number;
  total: number;
  agencyId: string;
  disabled: boolean;
  validationRequested: boolean;
}>();
const emit = defineEmits<{
  change: [point: EditablePoint];
  move: [direction: number];
  remove: [];
  busy: [value: boolean];
}>();
function update(patch: Partial<EditablePoint>): void {
  emit("change", { ...props.point, ...patch });
}
function insertTab(event: KeyboardEvent): void {
  if (event.key !== "Tab" || event.shiftKey) return;
  event.preventDefault();
  const input = event.target as HTMLTextAreaElement;
  const start = input.selectionStart;
  update({
    descriptionDetail:
      props.point.descriptionDetail.slice(0, start) +
      "\t" +
      props.point.descriptionDetail.slice(input.selectionEnd),
  });
  requestAnimationFrame(() => {
    input.selectionStart = input.selectionEnd = start + 1;
  });
}
</script>

<template>
  <article class="point">
    <header class="point__header">
      <h3>Point {{ index + 1 }}</h3>
      <div class="point__actions">
        <button
          type="button"
          :disabled="disabled || index === 0"
          :aria-label="`Monter le point ${index + 1}`"
          @click="emit('move', -1)"
        >
          <BaseIcon name="chevron-up" />
        </button>
        <button
          type="button"
          :disabled="disabled || index === total - 1"
          :aria-label="`Descendre le point ${index + 1}`"
          @click="emit('move', 1)"
        >
          <BaseIcon name="chevron-down" />
        </button>
        <button
          type="button"
          :disabled="disabled || total === 1"
          :aria-label="`Supprimer le point ${index + 1}`"
          @click="emit('remove')"
        >
          <BaseIcon name="delete" />
        </button>
      </div>
    </header>
    <BaseInput
      :id="`template-point-${point.key}`"
      :model-value="point.description"
      label="Intitulé du contrôle"
      :error="
        validationRequested && !point.description.trim()
          ? 'La description du point est obligatoire.'
          : undefined
      "
      required
      maxlength="500"
      placeholder="Ex. Vérifier l’état du câble"
      :disabled="disabled"
      @update:model-value="update({ description: $event })"
    />
    <label :for="`template-point-detail-${point.key}`">Détail du point *</label>
    <textarea
      :id="`template-point-detail-${point.key}`"
      :value="point.descriptionDetail"
      :disabled="disabled"
      :aria-invalid="validationRequested && !point.descriptionDetail.trim()"
      :aria-describedby="
        validationRequested && !point.descriptionDetail.trim()
          ? `template-detail-error-${point.key}`
          : undefined
      "
      required
      rows="5"
      placeholder="Décrivez les contrôles à effectuer…"
      @input="update({ descriptionDetail: ($event.target as HTMLTextAreaElement).value })"
      @keydown="insertTab"
    />
    <p
      v-if="validationRequested && !point.descriptionDetail.trim()"
      :id="`template-detail-error-${point.key}`"
      class="point__error"
      role="alert"
    >
      Le détail du point est obligatoire.
    </p>
    <TemplatePointPhoto
      :photo-id="point.photoId"
      :agency-id="agencyId"
      :disabled="disabled"
      @change="update({ photoId: $event })"
      @busy="emit('busy', $event)"
    />
  </article>
</template>

<style scoped lang="scss">
.point {
  display: grid;
  gap: 0.9rem;
  min-width: 0;
  padding: 1.25rem;
  border: 1px solid var(--color-border);
  border-radius: 0.85rem;
  background: var(--color-surface-secondary);
  transition:
    border-color 0.2s,
    box-shadow 0.2s;
}
.point:focus-within {
  border-color: var(--color-secondary);
  box-shadow: 0 0.35rem 1.25rem var(--color-shadow);
}
.point__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  flex-wrap: wrap;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--color-border);
}
.point h3,
.point p {
  margin: 0;
}
.point h3 {
  color: var(--color-secondary);
  font-size: 1.05rem;
}
.point__actions {
  display: flex;
  gap: 0.4rem;
}
.point__actions button {
  display: grid;
  place-items: center;
  min-width: 2.75rem;
  min-height: 2.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface-secondary);
  color: var(--color-text);
  cursor: pointer;
}
.point__actions button:hover:not(:disabled) {
  border-color: var(--color-secondary);
  color: var(--color-secondary);
}
.point__actions button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.point label {
  font-weight: 600;
}
.point textarea {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  min-height: 7rem;
  padding: 0.7rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-input-background);
  color: var(--color-text);
  font: inherit;
  resize: vertical;
  tab-size: 4;
}
.point textarea[aria-invalid="true"] {
  border-color: var(--color-danger);
}
.point textarea:focus-visible,
.point__actions button:focus-visible {
  outline: 3px solid var(--color-focus);
  outline-offset: 2px;
}
.point__error {
  color: var(--color-danger);
}
@media (max-width: 42rem) {
  .point {
    padding: 0.9rem;
  }
}
@media (prefers-reduced-motion: reduce) {
  .point {
    transition: none;
  }
}
</style>
