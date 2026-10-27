<script setup lang="ts">
import { computed } from "vue";
import type { InspectionTemplateDto } from "../../api/inspection-templates/dto";
const props = defineProps<{ template: InspectionTemplateDto; disabled: boolean }>();
const emit = defineEmits<{ copy: [] }>();
const ordered = computed(() => [...props.template.points].sort((a, b) => a.position - b.position));
</script>

<template>
  <section
    class="current"
    aria-labelledby="current-template-title"
  >
    <div class="current__heading">
      <div>
        <span class="current__eyebrow">Version active</span>
        <h2 id="current-template-title">Modèle courant</h2>
      </div>
      <span class="current__count"
        >{{ ordered.length }} point{{ ordered.length > 1 ? "s" : "" }}</span
      >
    </div>
    <p>
      Créé le {{ new Date(template.createdAt).toLocaleDateString("fr-FR") }}. Les états des lieux
      déjà créés conservent leur modèle.
    </p>
    <details>
      <summary>Consulter les points</summary>
      <ol>
        <li
          v-for="point in ordered"
          :key="point.id"
        >
          <strong>{{ point.description }}</strong
          ><span>{{ point.descriptionDetail }}</span>
        </li>
      </ol>
    </details>
    <button
      type="button"
      :disabled="disabled"
      @click="emit('copy')"
    >
      Reprendre ces points dans le nouveau modèle
    </button>
  </section>
</template>

<style scoped lang="scss">
.current {
  display: grid;
  gap: 0.8rem;
  min-width: 0;
  padding: 1.4rem;
  border: 1px solid var(--color-border);
  border-radius: 0.85rem;
  background: var(--color-surface);
}
.current__heading {
  display: flex;
  justify-content: space-between;
  gap: 0.7rem;
  flex-wrap: wrap;
}
.current__eyebrow {
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
  font-weight: 700;
}
.current__count {
  align-self: start;
  padding: 0.25rem 0.65rem;
  border-radius: 2rem;
  background: var(--color-surface-secondary);
  color: var(--color-secondary);
}
.current h2,
.current p {
  margin: 0;
}
.current p {
  color: var(--color-text-secondary);
}
.current details summary {
  cursor: pointer;
  font-weight: 600;
}
.current ol {
  display: grid;
  gap: 0.7rem;
  padding-left: 1.5rem;
}
.current li span {
  display: block;
  margin-top: 0.25rem;
  white-space: pre-wrap;
  tab-size: 4;
  color: var(--color-text-secondary);
  overflow-wrap: anywhere;
}
.current button {
  justify-self: start;
  min-height: 2.75rem;
  padding: 0.6rem 0.85rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface-secondary);
  color: var(--color-text);
  font: inherit;
  cursor: pointer;
}
.current button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
@media (max-width: 42rem) {
  .current {
    padding: 1rem;
  }
  .current button {
    width: 100%;
  }
}
</style>
