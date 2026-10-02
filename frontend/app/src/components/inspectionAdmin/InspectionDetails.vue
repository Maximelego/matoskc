<script setup lang="ts">
import type { InspectionRecord } from "../../api/inspections/admin";

const props = defineProps<{ inspection: InspectionRecord }>();
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "long", timeStyle: "short" });
const defects = () => props.inspection.sections.flatMap((section) => section.answers).filter((answer) => answer.value === "nonCompliant");
function answerLabel(value: string | number): string {
  if (value === "compliant") return "Conforme";
  if (value === "nonCompliant") return "Non conforme";
  return String(value);
}
</script>

<template>
  <article class="inspection-detail">
    <dl class="inspection-detail__metadata">
      <div><dt>Équipement</dt><dd>{{ inspection.equipmentName }}</dd></div>
      <div><dt>Numéro de série</dt><dd>{{ inspection.serialNumber }}</dd></div>
      <div><dt>Type</dt><dd>{{ inspection.direction === "return" ? "Retour" : "Départ" }}</dd></div>
      <div><dt>Réalisé le</dt><dd>{{ date.format(new Date(inspection.performedAt)) }}</dd></div>
      <div><dt>Opérateur</dt><dd>{{ inspection.operatorFirstName }}</dd></div>
      <div><dt>Modèle</dt><dd>Version {{ inspection.templateVersion }}</dd></div>
    </dl>

    <section class="inspection-detail__defects" aria-labelledby="inspection-defects-title">
      <h3 id="inspection-defects-title">Défauts constatés ({{ defects().length }})</h3>
      <p v-if="!defects().length">Aucun défaut constaté.</p>
      <ul v-else>
        <li v-for="answer in defects()" :key="answer.questionId">
          <strong>{{ answer.label }}</strong> — {{ answer.observation || "Aucune observation" }}
        </li>
      </ul>
    </section>

    <section v-for="section in inspection.sections" :key="section.id" class="inspection-detail__section">
      <h3>{{ section.title }}</h3>
      <dl>
        <div v-for="answer in section.answers" :key="answer.questionId" class="inspection-detail__answer">
          <dt>{{ answer.label }}</dt>
          <dd><strong>{{ answerLabel(answer.value) }}</strong></dd>
          <dd v-if="answer.observation">Observation : {{ answer.observation }}</dd>
          <dd v-if="answer.photoUrls.length">
            <a v-for="(url, index) in answer.photoUrls" :key="url" :href="url" target="_blank" rel="noopener noreferrer">
              Photographie {{ index + 1 }}
            </a>
          </dd>
        </div>
      </dl>
    </section>
  </article>
</template>

<style scoped lang="scss">
.inspection-detail { display: grid; gap: 1.25rem; min-width: 0; overflow-wrap: anywhere; }
.inspection-detail h3 { margin: 0 0 .65rem; }
.inspection-detail dl { margin: 0; }
.inspection-detail__metadata { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .75rem; }
.inspection-detail__metadata dt { color: var(--color-text-secondary); }
.inspection-detail__metadata dd, .inspection-detail__answer dd { margin: .2rem 0 0; }
.inspection-detail__defects { padding: 1rem; border-radius: .65rem; background: var(--color-warning-soft); }
.inspection-detail__defects p, .inspection-detail__defects ul { margin-bottom: 0; }
.inspection-detail__section { padding: 1rem; border: 1px solid var(--color-border); border-radius: .65rem; }
.inspection-detail__answer { padding: .75rem 0; border-top: 1px solid var(--color-border); }
.inspection-detail__answer a { display: inline-block; margin-right: .75rem; }
@media(max-width: 35rem) { .inspection-detail__metadata { grid-template-columns: 1fr; } }
</style>
