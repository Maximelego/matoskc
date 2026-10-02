<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import type {
  InspectionAnswer,
  InspectionDirection,
  PreviousInspection,
  InspectionQuestion,
  InspectionTemplateDto,
} from "../../api/inspections/dto";
import { inspectionsApi } from "../../api/inspections/mock";
import InspectionSectionStep from "./InspectionSectionStep.vue";
import { sectionErrors } from "./inspectionValidation";

const props = defineProps<{ equipmentId: string }>();
const model = ref<InspectionTemplateDto | null>(null);
const selectedEquipment = ref<EquipmentDto | null>(null);
const previousInspection = ref<PreviousInspection | null>(null);
const direction = ref<InspectionDirection>("departure");
const loading = ref(true);
const loadError = ref("");
const step = ref(0);
const operatorFirstName = ref("");
const answers = ref<Record<string, InspectionAnswer>>({});
const errors = ref<Record<string, string[]>>({});
const identityError = ref("");
const sections = computed(() => model.value?.sections ?? []);
const summaryStep = computed(() => sections.value.length + 1);
const currentSection = computed(() => sections.value[step.value - 1]);

async function load(): Promise<void> {
  loading.value = true;
  loadError.value = "";
  try {
    const equipment = await equipmentsApi.getById(props.equipmentId);
    if (equipment.status !== "Available" && equipment.status !== "Borrowed")
      throw new Error("Cet équipement ne peut pas faire l’objet d’un état des lieux actuellement.");
    direction.value = equipment.status === "Available" ? "departure" : "return";
    const [template, previous] = await Promise.all([
      inspectionsApi.getTemplate(equipment.equipmentCategoryId),
      inspectionsApi.getPreviousInspection(equipment.id),
    ]);
    selectedEquipment.value = equipment;
    model.value = template;
    previousInspection.value = previous;
  } catch (cause) {
    loadError.value = cause instanceof Error ? cause.message : "Impossible de charger le modèle.";
  } finally {
    loading.value = false;
  }
}
function priorDefect(questionId: string): string | undefined {
  return previousInspection.value?.defects.find(defect => defect.questionId === questionId)?.observation;
}
function updateAnswer(answer: InspectionAnswer): void {
  answers.value = { ...answers.value, [answer.questionId]: answer };
  errors.value = { ...errors.value, [answer.questionId]: [] };
}
function next(): void {
  if (step.value === 0) {
    identityError.value =
      operatorFirstName.value.trim()
        ? ""
        : "Veuillez saisir votre prénom.";
    if (identityError.value) return;
  } else if (currentSection.value) {
    errors.value = sectionErrors(currentSection.value, answers.value);
    if (Object.values(errors.value).some((messages) => messages.length)) return;
  }
  if (step.value < summaryStep.value) step.value++;
  window.scrollTo({ top: 0, behavior: "smooth" });
}
function answerText(question: InspectionQuestion): string {
  const answer = answers.value[question.id];
  if (!answer) return "Non renseigné";
  if (answer.kind === "condition")
    return answer.value === "compliant"
      ? "Conforme"
      : answer.value === "nonCompliant"
        ? "Non conforme"
        : "Non renseigné";
  if (answer.kind === "number")
    return answer.value === null
      ? "Non renseigné"
      : `${answer.value} ${question.kind === "number" ? (question.unit ?? "") : ""}`.trim();
  return answer.value.trim() || "Non renseigné";
}
function observation(questionId: string): string {
  const answer = answers.value[questionId];
  return answer?.kind === "condition" ? answer.observation : "";
}
function photoCount(questionId: string): number {
  const answer = answers.value[questionId];
  return answer?.kind === "condition" ? answer.photos.length : 0;
}
onMounted(() => {
  void load();
});
</script>

<template>
  <main
    class="inspection-wizard"
    aria-labelledby="inspection-title"
  >
    <header>
      <h1 id="inspection-title">État des lieux de {{ direction === "return" ? "retour" : "départ" }}</h1>
      <p
        class="inspection-wizard__demo"
        role="status"
      >
        Démonstration locale : aucune donnée ni photographie n’est enregistrée.
      </p>
    </header>
    <p
      v-if="loading"
      role="status"
    >
      Chargement du modèle…
    </p>
    <div
      v-else-if="loadError"
      role="alert"
    >
      <p>{{ loadError }}</p>
      <button
        type="button"
        @click="load"
      >
        Réessayer
      </button>
    </div>
    <template v-else-if="model">
      <div class="inspection-wizard__progress">
        <p>Étape {{ step + 1 }} sur {{ summaryStep + 1 }}</p>
        <progress
          :value="step"
          :max="summaryStep"
          :aria-label="`Étape ${step + 1} sur ${summaryStep + 1}`"
        />
      </div>
      <section
        v-if="step === 0"
        class="inspection-wizard__identity"
        aria-labelledby="identity-title"
      >
        <h2 id="identity-title">Identification</h2>
        <label for="inspection-operator">Prénom de l’opérateur *</label>
        <input
          id="inspection-operator"
          v-model.trim="operatorFirstName"
          autocomplete="given-name"
        />
        <p v-if="selectedEquipment">Équipement : <strong>{{ selectedEquipment.name }}</strong> ({{ selectedEquipment.serialNumber }}).</p>
        <p>Catégorie : {{ model.title }}</p>
        <section v-if="previousInspection?.defects.length" class="inspection-wizard__prior" aria-label="Défauts préexistants">
          <h3>Défauts relevés lors du précédent état des lieux</h3>
          <p>Constat du {{ previousInspection.performedAt }}.</p>
          <ul><li v-for="defect in previousInspection.defects" :key="defect.questionId">{{ model.sections.flatMap(section => section.questions).find(question => question.id === defect.questionId)?.label ?? defect.questionId }} : {{ defect.observation }}</li></ul>
        </section>
        <p v-else>Aucun défaut antérieur connu dans les données de démonstration.</p>
        <p
          v-if="identityError"
          class="inspection-wizard__error"
          role="alert"
        >
          {{ identityError }}
        </p>
      </section>
      <InspectionSectionStep
        v-else-if="currentSection"
        :section="currentSection"
        :answers="answers"
        :errors="errors"
        :previous-defects="previousInspection?.defects ?? []"
        @update="updateAnswer"
      />
      <section
        v-else
        class="inspection-wizard__summary"
        aria-labelledby="summary-title"
      >
        <h2 id="summary-title">Récapitulatif</h2>
        <p>Opérateur : {{ operatorFirstName }}</p>
        <p>Matériel : {{ selectedEquipment?.name }} ({{ selectedEquipment?.serialNumber }})</p>
        <p>Sens : {{ direction === "return" ? "Retour" : "Départ" }}</p>
        <p>Modèle : version {{ model.version }}</p>
        <section
          v-for="(section, index) in sections"
          :key="section.id"
          class="inspection-wizard__summary-section"
        >
          <div class="inspection-wizard__summary-heading">
            <h3>{{ section.title }}</h3>
            <button
              type="button"
              @click="step = index + 1"
            >
              Modifier
            </button>
          </div>
          <dl>
            <div
              v-for="question in section.questions"
              :key="question.id"
            >
              <dt>{{ question.label }}</dt>
              <dd>{{ answerText(question) }}</dd>
              <dd v-if="priorDefect(question.id)">Défaut préexistant : {{ priorDefect(question.id) }}</dd>
              <dd v-if="observation(question.id)">Observation : {{ observation(question.id) }}</dd>
              <dd v-if="photoCount(question.id)">
                {{ photoCount(question.id) }} photographie(s) sélectionnée(s)
              </dd>
            </div>
          </dl>
        </section>
        <p class="inspection-wizard__demo">
          La signature et la validation finale seront ajoutées avec l’API métier.
        </p>
      </section>
      <nav
        class="inspection-wizard__navigation"
        aria-label="Navigation du formulaire"
      >
        <button
          v-if="step > 0"
          type="button"
          class="inspection-wizard__secondary"
          @click="step--"
        >
          Précédent
        </button>
        <button
          v-if="step < summaryStep"
          type="button"
          class="inspection-wizard__primary"
          @click="next"
        >
          {{ step === sections.length ? "Voir le récapitulatif" : "Continuer" }}
        </button>
      </nav>
    </template>
  </main>
</template>

<style scoped lang="scss">
.inspection-wizard {
  display: grid;
  gap: 1.5rem;
  width: min(100%, 52rem);
  min-width: 0;
  margin-inline: auto;
}
.inspection-wizard h1 {
  margin-bottom: 0.5rem;
}
.inspection-wizard__demo {
  margin: 0;
  padding: 0.75rem;
  border-radius: 0.5rem;
  color: var(--color-warning);
  background: var(--color-warning-soft);
}
.inspection-wizard__progress p {
  margin: 0 0 0.35rem;
  color: var(--color-text-secondary);
}
.inspection-wizard__progress progress {
  width: 100%;
  accent-color: var(--color-primary);
}
.inspection-wizard__identity {
  display: grid;
  gap: 0.75rem;
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
}
.inspection-wizard__identity h2,
.inspection-wizard__identity p {
  margin: 0;
}
.inspection-wizard__identity input {
  box-sizing: border-box;
  width: 100%;
  min-height: 2.75rem;
  padding: 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-input-background);
}
.inspection-wizard__prior { border: 1px solid var(--color-warning); border-radius: .5rem; padding: .75rem; background: var(--color-warning-soft); }
.inspection-wizard__prior h3 { margin: 0; }
.inspection-wizard__error {
  color: var(--color-danger);
}
.inspection-wizard__navigation {
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
}
.inspection-wizard__navigation button {
  min-height: 2.75rem;
  padding: 0.6rem 1rem;
  border-radius: 0.5rem;
  font: inherit;
  cursor: pointer;
}
.inspection-wizard__primary {
  margin-left: auto;
  border: 1px solid var(--color-primary);
  color: var(--color-text-on-primary);
  background: var(--color-primary);
}
.inspection-wizard__secondary {
  border: 1px solid var(--color-border);
  color: var(--color-text);
  background: var(--color-surface);
}
.inspection-wizard__summary {
  display: grid;
  gap: 0.75rem;
}
.inspection-wizard__summary > p {
  margin: 0;
}
.inspection-wizard__summary-section {
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
}
.inspection-wizard__summary-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}
.inspection-wizard__summary-heading h3 {
  margin: 0;
}
.inspection-wizard__summary-heading button {
  min-height: 2.75rem;
  color: var(--color-primary);
  background: none;
  border: 0;
  cursor: pointer;
}
.inspection-wizard__summary dl > div {
  margin-top: 0.75rem;
  overflow-wrap: anywhere;
}
.inspection-wizard__summary dt {
  color: var(--color-text-secondary);
}
.inspection-wizard__summary dd {
  margin: 0.2rem 0 0;
}
@media (max-width: 42rem) {
  .inspection-wizard {
    gap: 1rem;
  }
  .inspection-wizard__navigation {
    position: sticky;
    bottom: 0;
    z-index: 2;
    padding: 0.75rem 0;
    background: var(--color-background);
  }
  .inspection-wizard__navigation button {
    flex: 1;
  }
}
</style>
