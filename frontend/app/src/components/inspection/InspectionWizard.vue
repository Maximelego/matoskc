<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { ApiError } from "../../api/client";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import { rentalsApi } from "../../api/rentals";
import type { RentalDto } from "../../api/rentals/dto";
import { inspectionTemplatesApi } from "../../api/inspection-templates";
import { inspectionHttpApi } from "../../api/inspections";
import type { InspectionDto, InspectionAnswer, InspectionDirection, PreviousInspection, InspectionQuestion, InspectionTemplateDto } from "../../api/inspections/dto";
import InspectionSectionStep from "./InspectionSectionStep.vue";
import InspectionSignaturePad from "./InspectionSignaturePad.vue";
import { sectionErrors } from "./inspectionValidation";
import { departureDefects, fromServerAnswer, toServerAnswer, toWizardTemplate } from "./inspectionHttpAdapter";

const props = defineProps<{ equipmentId: string }>();
const router = useRouter();
const model = ref<InspectionTemplateDto | null>(null);
const selectedEquipment = ref<EquipmentDto | null>(null);
const rental = ref<RentalDto | null>(null);
const inspection = ref<InspectionDto | null>(null);
const previousInspection = ref<PreviousInspection | null>(null);
const departure = ref<InspectionDto | null>(null);
const direction = ref<InspectionDirection>("departure");
const loading = ref(true);
const saving = ref(false);
const loadError = ref("");
const saveError = ref("");
const step = ref(0);
const operatorFirstName = ref("");
const customerName = ref("");
const customerEmail = ref("");
const contractReference = ref("");
const answers = ref<Record<string, InspectionAnswer>>({});
const photoIds = ref<Record<string, string[]>>({});
const errors = ref<Record<string, string[]>>({});
const identityError = ref("");
const sections = computed(() => model.value?.sections ?? []);
const summaryStep = computed(() => sections.value.length + 1);
const currentSection = computed(() => sections.value[step.value - 1]);
function message(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.status === 401) return "Veuillez vous connecter pour continuer.";
    if (cause.status === 403) return "Vous ne disposez pas des droits nécessaires.";
    if (cause.status === 404) return "Le dossier ou le modèle de vérification est introuvable.";
    if (cause.status === 409) return "Le dossier a changé depuis son ouverture. Actualisez la page.";
    if (cause.status >= 500) return "Le serveur rencontre un problème. Veuillez réessayer.";
    return cause.message;
  }
  return cause instanceof Error ? cause.message : "Une erreur est survenue.";
}
async function load(): Promise<void> {
  loading.value = true; loadError.value = "";
  try {
    const equipment = await equipmentsApi.getById(props.equipmentId);
    if (equipment.status !== "Available" && equipment.status !== "Borrowed")
      throw new Error("Cet équipement ne peut pas faire l’objet d’un état des lieux actuellement.");
    direction.value = equipment.status === "Available" ? "departure" : "return";
    const currentRental = await rentalsApi.getCurrentForEquipment(equipment.id);
    if (direction.value === "return" && !currentRental)
      throw new Error("Aucune location en cours n’est associée à cet équipement.");
    rental.value = currentRental;
    if (currentRental) {
      customerName.value = currentRental.customerName;
      customerEmail.value = currentRental.customerEmail ?? "";
      contractReference.value = currentRental.contractReference ?? "";
    }
    const prior = currentRental && direction.value === "return" ? await inspectionHttpApi.getDeparture(currentRental.id) : null;
    if (direction.value === "return" && !prior?.templateVersionId)
      throw new Error("L’état des lieux de départ de cette location est introuvable.");
    departure.value = prior;
    previousInspection.value = departureDefects(prior);
    const drafts = currentRental ? await inspectionHttpApi.list({ rentalId: currentRental.id, type: direction.value === "departure" ? "Departure" : "Return", status: "Draft" }) : { inspections: [] };
    const existing = drafts.inspections[0] ?? null;
    const versionId = prior?.templateVersionId ?? existing?.templateVersionId;
    const version = versionId
      ? await inspectionTemplatesApi.getVersionById(versionId)
      : await inspectionTemplatesApi.getCurrentForCategory(equipment.equipmentCategoryId);
    model.value = toWizardTemplate(version);
    selectedEquipment.value = equipment;
    inspection.value = existing;
    if (existing) {
      operatorFirstName.value = existing.operatorFirstName;
      for (const answer of existing.answers) {
        const question = model.value.sections.flatMap(section => section.questions).find(item => item.id === answer.templatePointId);
        if (question) answers.value[question.id] = fromServerAnswer(answer, question.kind);
        photoIds.value[answer.templatePointId] = answer.photoIds;
      }
    }
  } catch (cause) { loadError.value = message(cause); }
  finally { loading.value = false; }
}
function priorDefect(questionId: string): string | undefined {
  return previousInspection.value?.defects.find(defect => defect.questionId === questionId)?.observation;
}
function updateAnswer(answer: InspectionAnswer): void {
  answers.value = { ...answers.value, [answer.questionId]: answer };
  errors.value = { ...errors.value, [answer.questionId]: [] };
}
async function ensureInspection(): Promise<InspectionDto> {
  if (inspection.value) return inspection.value;
  let dossier = rental.value;
  if (!dossier) {
    dossier = await rentalsApi.create({ equipmentId: props.equipmentId, customerName: customerName.value.trim(),
      customerEmail: customerEmail.value.trim() || null, contractReference: contractReference.value.trim() || null });
    rental.value = dossier;
  }
  const created = await inspectionHttpApi.create({ rentalId: dossier.id,
    type: direction.value === "departure" ? "Departure" : "Return", operatorFirstName: operatorFirstName.value.trim() });
  inspection.value = created;
  return created;
}
async function persist(): Promise<void> {
  const draft = await ensureInspection();
  const stored = new Map(draft.answers.map(answer => [answer.templatePointId, answer]));
  for (const answer of Object.values(answers.value)) {
    if (answer.kind !== "condition") continue;
    const ids = [...(photoIds.value[answer.questionId] ?? [])];
    // Uploaded photos are tied to the point on the server before the draft references their IDs.
    for (const file of answer.photos) {
      const media = await inspectionHttpApi.uploadPhoto(draft.id, answer.questionId, file);
      ids.push(media.id);
    }
    photoIds.value[answer.questionId] = ids;
    answers.value[answer.questionId] = { ...answer, photos: [] };
  }
  inspection.value = await inspectionHttpApi.saveDraft(draft.id, {
    answers: Object.values(answers.value).map(answer => toServerAnswer(answer,
      photoIds.value[answer.questionId] ?? [],
      stored.get(answer.questionId)?.existingDefectId ?? null)),
  });
}
async function next(): Promise<void> {
  if (saving.value) return;
  if (step.value === 0) {
    identityError.value = !operatorFirstName.value.trim() ? "Veuillez saisir votre prénom." :
      direction.value === "departure" && !customerName.value.trim() ? "Veuillez saisir le nom du locataire." : "";
    if (identityError.value) return;
  } else if (currentSection.value) {
    errors.value = sectionErrors(currentSection.value, answers.value);
    for (const question of currentSection.value.questions) {
      if ((photoIds.value[question.id]?.length ?? 0) > 0)
        errors.value[question.id] = (errors.value[question.id] ?? []).filter(text => text !== "Ajoutez une photographie.");
    }
    if (Object.values(errors.value).some(messages => messages.length)) return;
  }
  saving.value = true; saveError.value = "";
  try { await persist(); if (step.value < summaryStep.value) step.value++; window.scrollTo({ top: 0, behavior: "smooth" }); }
  catch (cause) { saveError.value = message(cause); }
  finally { saving.value = false; }
}
async function validate(): Promise<void> {
  if (!inspection.value || saving.value) return;
  saving.value = true; saveError.value = "";
  try {
    await persist();
    const submissionId = localStorage.getItem(`inspection-submission-${inspection.value.id}`) ?? crypto.randomUUID();
    localStorage.setItem(`inspection-submission-${inspection.value.id}`, submissionId);
    await inspectionHttpApi.validate(inspection.value.id, { submissionId,
      ...(direction.value === "departure" ? { acceptance: { signerName: signerName.value.trim(),
        acceptedTextVersion: "v1", signatureId: signatureId.value } } : {}) });
    localStorage.removeItem(`inspection-submission-${inspection.value.id}`);
    await router.push("/inspections");
  } catch (cause) { saveError.value = message(cause); }
  finally { saving.value = false; }
}
const signerName = ref("");
const accepted = ref(false);
const signatureId = ref("");
const signatureFile = ref<File | null>(null);
async function uploadSignature(): Promise<void> {
  if (!signatureFile.value) return;
  saving.value = true; saveError.value = "";
  try { const current = await ensureInspection(); signatureId.value = (await inspectionHttpApi.uploadSignature(current.id, signatureFile.value)).id; }
  catch (cause) { saveError.value = message(cause); }
  finally { saving.value = false; }
}
function answerText(question: InspectionQuestion): string {
  const answer = answers.value[question.id];
  if (!answer) return "Non renseigné";
  if (answer.kind === "condition") return answer.value === "compliant" ? "Conforme" : answer.value === "nonCompliant" ? "Non conforme" : "Non renseigné";
  if (answer.kind === "number") return answer.value === null ? "Non renseigné" : String(answer.value);
  return answer.value.trim() || "Non renseigné";
}
function observation(id: string): string { const answer = answers.value[id]; return answer?.kind === "condition" ? answer.observation : ""; }
function photoCount(id: string): number { const answer = answers.value[id]; return (answer?.kind === "condition" ? answer.photos.length : 0) + (photoIds.value[id]?.length ?? 0); }
onMounted(() => { void load(); });
</script>

<template>
  <main
    class="inspection-wizard"
    aria-labelledby="inspection-title"
  >
    <header>
      <h1 id="inspection-title">État des lieux de {{ direction === "return" ? "retour" : "départ" }}</h1>

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
        <template v-if="direction === 'departure'">
          <label for="inspection-customer">Nom du locataire *</label>
          <input id="inspection-customer" v-model.trim="customerName" required :disabled="Boolean(rental)" />
          <label for="inspection-email">Adresse électronique du locataire (facultatif)</label>
          <input id="inspection-email" v-model.trim="customerEmail" type="email" :disabled="Boolean(rental)" />
          <label for="inspection-contract">Référence du contrat (facultative)</label>
          <input id="inspection-contract" v-model.trim="contractReference" :disabled="Boolean(rental)" />
        </template>
        <p v-else>Locataire : {{ customerName }}</p>
        <section v-if="previousInspection?.defects.length" class="inspection-wizard__prior" aria-label="Défauts préexistants">
          <h3>Défauts relevés lors du précédent état des lieux</h3>
          <p>Constat du {{ previousInspection.performedAt }}.</p>
          <ul><li v-for="defect in previousInspection.defects" :key="defect.questionId">{{ model.sections.flatMap(section => section.questions).find(question => question.id === defect.questionId)?.label ?? defect.questionId }} : {{ defect.observation }}</li></ul>
        </section>
        <p v-else>Aucun défaut relevé lors du départ de cette location.</p>
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
        <div v-if="direction === 'departure'" class="inspection-wizard__identity">
          <p>Le locataire doit consulter ce récapitulatif avant d’accepter et de signer.</p>
          <label for="inspection-signer">Nom du signataire *</label>
          <input id="inspection-signer" v-model.trim="signerName" />
          <label><input v-model="accepted" type="checkbox" /> Je reconnais avoir pris connaissance du constat.</label>
          <span>Signature du locataire *</span>
          <InspectionSignaturePad @change="signatureFile = $event; signatureId = ''" />
          <button type="button" :disabled="!signatureFile || saving" @click="uploadSignature">Enregistrer la signature</button>
          <p v-if="signatureId" role="status">Signature enregistrée.</p>
        </div>
        <button type="button" class="inspection-wizard__primary" :disabled="saving || (direction === 'departure' && (!accepted || !signerName.trim() || !signatureId))" @click="validate">
          Valider l’état des lieux
        </button>
      </section>
      <p v-if="saveError" role="alert">{{ saveError }}</p>
      <p v-if="saving" role="status">Enregistrement…</p>
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
          :disabled="saving"
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
