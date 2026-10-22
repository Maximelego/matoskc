<script setup lang="ts">
import type { KnownDefectDto } from "../../api/inspections";
import InspectionSummaryPhotos from "./InspectionSummaryPhotos.vue";

defineProps<{
  defects: KnownDefectDto[];
  operatorNames: Record<string, string>;
  currentOperator: string;
  photos: Record<string, string>;
  loading: boolean;
}>();
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium" });
const labels = {
  Unchanged: "Inchangé",
  Worsened: "Aggravé",
  MaintenanceCheckRequired: "À vérifier par la maintenance",
} as const;
</script>

<template>
  <section
    class="known-defects"
    aria-labelledby="known-defects-title"
  >
    <header class="known-defects__header">
      <h3 id="known-defects-title">Défauts déjà connus</h3>
      <span class="known-defects__count">{{ defects.length }}</span>
    </header>
    <p
      v-if="!defects.length"
      class="known-defects__empty"
    >
      Aucun défaut connu pour cet état des lieux.
    </p>
    <div
      v-else
      class="known-defects__list"
    >
      <article
        v-for="defect in defects"
        :key="defect.id"
        class="known-defects__card"
      >
        <div class="known-defects__title">
          <h4>{{ defect.description }}</h4>
          <span
            class="known-defects__badge"
            :class="`known-defects__badge--${defect.result ?? 'Pending'}`"
          >
            {{ defect.result ? labels[defect.result] : "À confirmer" }}
          </span>
        </div>
        <div class="known-defects__history">
          <span
            >Constat initial · {{ date.format(new Date(defect.observedAt)) }} ·
            {{ operatorNames[defect.defectId] ?? "Opérateur non renseigné" }}</span
          >
          <p>{{ defect.historicalObservation }}</p>
          <InspectionSummaryPhotos
            :photo-ids="defect.historicalPhotoIds"
            :photos="photos"
            :loading="loading"
            label="Photographie du constat initial"
          />
        </div>
        <div
          v-if="defect.result === 'Worsened'"
          class="known-defects__worsened"
        >
          <strong>Aggravation constatée par {{ currentOperator }}</strong>
          <p>{{ defect.observation }}</p>
          <InspectionSummaryPhotos
            :photo-ids="defect.photoIds"
            :photos="photos"
            :loading="loading"
            label="Photographie de l’aggravation"
          />
        </div>
      </article>
    </div>
  </section>
</template>

<style scoped lang="scss">
.known-defects {
  display: grid;
  gap: 0.85rem;
}
.known-defects__header,
.known-defects__title {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.known-defects h3,
.known-defects h4,
.known-defects p {
  margin: 0;
}
.known-defects__count {
  padding: 0.15rem 0.55rem;
  border-radius: 1rem;
  color: var(--color-text-secondary);
  background: var(--color-surface-secondary);
}
.known-defects__empty {
  padding: 1rem;
  border: 1px dashed var(--color-border);
  border-radius: 0.65rem;
  color: var(--color-text-secondary);
}
.known-defects__list {
  display: grid;
  gap: 0.7rem;
}
.known-defects__card {
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
}
.known-defects__badge {
  padding: 0.3rem 0.6rem;
  border-radius: 999px;
  font-size: var(--font-size-sm);
  background: var(--color-surface-secondary);
}
.known-defects__badge--Unchanged {
  color: var(--color-success);
  background: var(--color-success-soft);
}
.known-defects__badge--Worsened {
  color: var(--color-danger);
  background: var(--color-danger-soft);
}
.known-defects__badge--MaintenanceCheckRequired {
  color: var(--color-warning);
  background: var(--color-warning-soft);
}
.known-defects__history,
.known-defects__worsened {
  display: grid;
  gap: 0.35rem;
  min-width: 0;
}
.known-defects__history > span {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
.known-defects__worsened {
  padding-top: 0.8rem;
  border-top: 1px solid var(--color-border);
}
</style>
