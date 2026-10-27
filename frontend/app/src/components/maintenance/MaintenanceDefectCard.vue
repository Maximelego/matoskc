<script setup lang="ts">
import type { DefectDto } from "../../api/defects/dto";
import DefectPhoto from "./DefectPhoto.vue";

defineProps<{ defect: DefectDto; equipmentId: string; selected: boolean; disabled: boolean }>();
const emit = defineEmits<{ select: [id: string, selected: boolean] }>();
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium", timeStyle: "short" });
</script>

<template>
  <article
    class="defect"
    :class="{ 'defect--selected': selected }"
  >
    <div class="defect__top">
      <strong>{{ defect.description }}</strong>
      <span
        class="defect__badge"
        :class="{ 'defect__badge--resolved': defect.status === 'Resolved' }"
      >
        {{ defect.status === "Open" ? "Ouvert" : "Résolu" }}
      </span>
    </div>
    <p class="defect__observation">{{ defect.observation }}</p>
    <p class="defect__meta">
      Constaté par {{ defect.operatorFirstName }} · {{ date.format(new Date(defect.reportedAt)) }}
    </p>
    <label
      v-if="defect.status === 'Open'"
      class="defect__choice"
    >
      <input
        type="checkbox"
        :checked="selected"
        :disabled="disabled"
        @change="emit('select', defect.id, ($event.target as HTMLInputElement).checked)"
      />
      Résoudre pendant cette maintenance
    </label>
    <details
      v-if="defect.occurrences.length"
      class="defect__history"
    >
      <summary>Voir les constats ({{ defect.occurrences.length }})</summary>
      <ul>
        <li
          v-for="occurrence in defect.occurrences"
          :key="occurrence.id"
        >
          <p>{{ occurrence.observation }}</p>
          <small
            >{{ date.format(new Date(occurrence.observedAt)) }} ·
            {{ occurrence.operatorFirstName }}</small
          >
          <div class="defect__photos">
            <DefectPhoto
              v-for="id in occurrence.photoIds"
              :key="id"
              :equipment-id="equipmentId"
              :defect-id="defect.id"
              :photo-id="id"
            />
          </div>
        </li>
      </ul>
    </details>
    <small
      v-if="defect.resolvedAt"
      class="defect__meta"
      >Résolu le {{ date.format(new Date(defect.resolvedAt)) }}</small
    >
  </article>
</template>

<style scoped lang="scss">
.defect {
  display: grid;
  gap: 0.7rem;
  min-width: 0;
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
  transition:
    border-color 0.2s,
    box-shadow 0.2s;
}
.defect--selected {
  border-color: var(--color-secondary);
  box-shadow: inset 0.2rem 0 var(--color-secondary);
}
.defect__top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
  flex-wrap: wrap;
}
.defect__badge {
  padding: 0.2rem 0.65rem;
  border-radius: 2rem;
  color: var(--color-warning);
  background: var(--color-warning-soft);
  font-size: var(--font-size-sm);
}
.defect__badge--resolved {
  color: var(--color-success);
  background: var(--color-success-soft);
}
.defect__observation,
.defect__meta,
.defect__history p {
  margin: 0;
  overflow-wrap: anywhere;
}
.defect__meta,
.defect__history small {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
.defect__choice {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  width: fit-content;
  padding: 0.6rem 0.8rem;
  border-radius: 0.5rem;
  background: var(--color-surface-secondary);
  cursor: pointer;
}
.defect__choice input {
  width: 1.1rem;
  height: 1.1rem;
  accent-color: var(--color-secondary);
}
.defect__history summary {
  cursor: pointer;
  color: var(--color-secondary);
  font-weight: 600;
}
.defect__history ul {
  display: grid;
  gap: 0.7rem;
  padding-left: 1.2rem;
}
.defect__history li {
  padding: 0.5rem 0;
}
.defect__photos {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.5rem;
}
</style>
