<script setup lang="ts">
import { computed } from "vue";
import type {
  InspectionAnswerDto,
  InspectionFindingDto,
  InspectionTemplateDto,
} from "../../api/inspections";
import TemplateIllustration from "../inspection/TemplateIllustration.vue";
import InspectionSummaryPhotos from "./InspectionSummaryPhotos.vue";

const props = defineProps<{
  points: InspectionTemplateDto["points"];
  answers: InspectionAnswerDto[];
  findings: InspectionFindingDto[];
  operator: string;
  photos: Record<string, string>;
  loading: boolean;
}>();
const orderedPoints = computed(() => [...props.points].sort((a, b) => a.position - b.position));
const answersByPoint = computed(
  () => new Map(props.answers.map((answer) => [answer.templatePointId, answer])),
);
const findingsByPoint = computed(() => {
  const grouped = new Map<string, InspectionFindingDto[]>();
  for (const finding of props.findings) {
    const items = grouped.get(finding.templatePointId) ?? [];
    items.push(finding);
    grouped.set(finding.templatePointId, items);
  }
  return grouped;
});
function count(pointId: string): number {
  return findingsByPoint.value.get(pointId)?.length ?? 0;
}
</script>

<template>
  <section
    class="points"
    aria-labelledby="points-title"
  >
    <header class="points__heading">
      <h3 id="points-title">Points contrôlés</h3>
      <span>{{ points.length }} point(s) · {{ findings.length }} nouveau(x) défaut(s)</span>
    </header>
    <p
      v-if="!points.length"
      class="points__empty"
    >
      Aucun point dans ce modèle.
    </p>
    <div
      v-else
      class="points__list"
    >
      <details
        v-for="point in orderedPoints"
        :key="point.id"
        class="points__item"
      >
        <summary>
          <span class="points__description">{{ point.description }}</span>
          <span
            class="points__badge"
            :class="
              answersByPoint.get(point.id)?.result === 'NonCompliant'
                ? 'points__badge--issue'
                : 'points__badge--clear'
            "
          >
            {{
              answersByPoint.get(point.id)?.result === "NonCompliant"
                ? `${count(point.id)} nouveau(x) défaut(s)`
                : answersByPoint.has(point.id)
                  ? "Aucun nouveau défaut"
                  : "Sans réponse"
            }}
          </span>
        </summary>
        <div class="points__content">
          <p
            v-if="point.descriptionDetail"
            class="points__detail"
          >
            {{ point.descriptionDetail }}
          </p>
          <TemplateIllustration
            v-if="point.photoId"
            :photo-id="point.photoId"
          />
          <div
            v-if="count(point.id)"
            class="points__findings"
          >
            <article
              v-for="(finding, index) in findingsByPoint.get(point.id)"
              :key="finding.id"
              class="points__finding"
            >
              <strong>Défaut {{ index + 1 }}</strong>
              <p>{{ finding.observation }}</p>
              <small>Constaté par {{ operator }}</small>
              <InspectionSummaryPhotos
                :photo-ids="finding.photoIds"
                :photos="photos"
                :loading="loading"
                label="Photographie du nouveau défaut"
              />
            </article>
          </div>
          <p v-else-if="answersByPoint.get(point.id)?.result === 'NonCompliant'">
            Aucun nouveau constat détaillé n’est associé à ce point.
          </p>
        </div>
      </details>
    </div>
  </section>
</template>

<style scoped lang="scss">
.points {
  display: grid;
  gap: 0.85rem;
  min-width: 0;
}
.points__heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.points h3,
.points p {
  margin: 0;
}
.points__heading > span {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
.points__empty {
  color: var(--color-text-secondary);
}
.points__list {
  display: grid;
  gap: 0.55rem;
}
.points__item {
  min-width: 0;
  border: 1px solid var(--color-border);
  border-radius: 0.65rem;
  background: var(--color-surface);
}
.points__item[open] {
  border-color: var(--color-border-strong);
}
.points summary {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.9rem;
  cursor: pointer;
}
.points summary:focus-visible {
  outline: 3px solid var(--color-focus);
  outline-offset: 2px;
}
.points__description {
  font-weight: var(--font-weight-semibold);
  overflow-wrap: anywhere;
}
.points__badge {
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  font-size: var(--font-size-sm);
  background: var(--color-surface-secondary);
}
.points__badge--clear {
  color: var(--color-success);
  background: var(--color-success-soft);
}
.points__badge--issue {
  color: var(--color-danger);
  background: var(--color-danger-soft);
}
.points__content {
  display: grid;
  gap: 0.75rem;
  padding: 0 0.9rem 0.9rem;
}
.points__detail {
  white-space: pre-wrap;
  tab-size: 4;
  color: var(--color-text-secondary);
}
.points__findings {
  display: grid;
  gap: 0.65rem;
}
.points__finding {
  display: grid;
  gap: 0.35rem;
  padding: 0.85rem;
  border-left: 3px solid var(--color-danger);
  border-radius: 0.4rem;
  background: var(--color-surface-secondary);
}
.points__finding small {
  color: var(--color-text-secondary);
}
@media (max-width: 42rem) {
  .points summary {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
