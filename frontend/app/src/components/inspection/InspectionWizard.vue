<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { ApiError } from "../../api/client";
import { agenciesApi } from "../../api/agencies";
import { authSession } from "../../api/auth/session";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import { inspectionHttpApi } from "../../api/inspections";
import type { ClientInputDto, InspectionDto, InspectionTemplateDto, RentalDto } from "../../api/inspections";
import { normalizeCommandNumber } from "../common/input/commandNumber";
import InspectionIdentity from "./InspectionIdentity.vue";
import InspectionPoint from "./InspectionPoint.vue";
import type { PointValue } from "./InspectionPoint.vue";
import InspectionSignaturePad from "./InspectionSignaturePad.vue";
import TemplateIllustration from "./TemplateIllustration.vue";

const props = defineProps<{ equipmentId: string }>();
const router = useRouter();
const equipment = ref<EquipmentDto | null>(null);
const rental = ref<RentalDto | null>(null);
const inspection = ref<InspectionDto | null>(null);
const template = ref<InspectionTemplateDto | null>(null);
const departure = ref<InspectionDto | null>(null);
const departureTemplate = ref<InspectionTemplateDto | null>(null);
const client = ref<ClientInputDto>({ displayName: "", email: "", phone: "", agencyCode: 83, commandValue: "" });
const operatorFirstName = ref("");
const points = ref<Record<string, PointValue>>({});
const signerName = ref("");
const accepted = ref(false);
const signatureFile = ref<File | null>(null);
const step = ref(0);
const busy = ref(false);
const loading = ref(true);
const error = ref("");
const isDeparture = computed(() => equipment.value?.status === "Available");
const orderedPoints = computed(() => [...(template.value?.points ?? [])].sort((a, b) => a.position - b.position));
const currentPoint = computed(() => orderedPoints.value[step.value - 1]);
const finalStep = computed(() => orderedPoints.value.length + 1);
const defaultPoint = (): PointValue => ({ result: null, observation: "", files: [], photoIds: [] });

function message(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.status === 401) return "Veuillez vous reconnecter.";
    if (cause.status === 403) return "Vous ne disposez pas des droits nécessaires.";
    if (cause.status === 404) return "L’équipement, la location ou le modèle est introuvable.";
    if (cause.status === 409) return "Le dossier a changé. Actualisez la page avant de continuer.";
    return cause.message;
  }
  return cause instanceof Error ? cause.message : "Une erreur est survenue.";
}

async function load(): Promise<void> {
  loading.value = true; error.value = "";
  try {
    const item = await equipmentsApi.getById(props.equipmentId);
    if (item.status !== "Available" && item.status !== "Borrowed")
      throw new Error("Cet équipement ne peut pas faire l’objet d’un état des lieux.");
    equipment.value = item;
    const current = await inspectionHttpApi.getCurrentRental(item.id);
    rental.value = current;
    if (item.status === "Borrowed" && !current) throw new Error("Aucune location en cours pour cet équipement.");
    if (current) client.value = current.client;
    if (item.status === "Available" && !current) {
      const agencies = await agenciesApi.list();
      const own = agencies.agencies.find(a => a.id === authSession.account.value?.agencyId);
      client.value.agencyCode = own?.code ?? agencies.agencies.find(a => a.code === 83)?.code ?? 83;
    }
    departure.value = item.status === "Borrowed" && current
      ? await inspectionHttpApi.getDeparture(current.id) : null;
    departureTemplate.value = departure.value
      ? await inspectionHttpApi.getTemplate(departure.value.templateId) : null;
    let draft: InspectionDto | null = null;
    if (current) {
      const result = await inspectionHttpApi.list({ rentalId: current.id,
        type: item.status === "Available" ? "Departure" : "Return", status: "Draft" });
      const summary = result.inspections[0];
      draft = summary ? await inspectionHttpApi.getById(summary.id) : null;
    }
    inspection.value = draft;
    operatorFirstName.value = draft?.operatorFirstName ?? "";
    // Every new inspection uses the latest category template; an existing draft retains its own template.
    template.value = draft
      ? await inspectionHttpApi.getTemplate(draft.templateId)
      : await inspectionHttpApi.getLatestTemplate(item.equipmentCategoryId);
    points.value = Object.fromEntries(template.value.points.map(point => {
      const answer = draft?.answers.find(a => a.templatePointId === point.id);
      return [point.id, answer
        ? { result: answer.result, observation: answer.observation ?? "", photoIds: answer.photoIds, files: [] }
        : defaultPoint()];
    }));
  } catch (cause) { error.value = message(cause); }
  finally { loading.value = false; }
}

function checkIdentity(): boolean {
  if (!operatorFirstName.value.trim()) { error.value = "Veuillez saisir le prénom de l’opérateur."; return false; }
  if (!isDeparture.value || inspection.value) return true;
  const number = normalizeCommandNumber(client.value.commandValue);
  if (!client.value.displayName.trim() || !client.value.email.trim() || !client.value.phone.trim()
      || client.value.agencyCode <= 0 || !number) {
    error.value = "Renseignez les coordonnées du client, l’agence et un numéro de commande de 1 à 6 chiffres.";
    return false;
  }
  client.value.commandValue = number;
  return true;
}

async function ensureInspection(): Promise<InspectionDto> {
  if (inspection.value) return inspection.value;
  let created: InspectionDto;
  if (isDeparture.value) {
    created = await inspectionHttpApi.createDeparture({ equipmentId: props.equipmentId,
      client: { ...client.value, displayName: client.value.displayName.trim(),
        email: client.value.email.trim(), phone: client.value.phone.trim() },
      operatorFirstName: operatorFirstName.value.trim() });
  } else {
    if (!rental.value) throw new Error("Location introuvable.");
    created = await inspectionHttpApi.createReturn(rental.value.id, operatorFirstName.value.trim());
  }
  inspection.value = created;
  template.value = await inspectionHttpApi.getTemplate(created.templateId);
  return created;
}

async function persist(): Promise<void> {
  const current = await ensureInspection();
  const answers = Object.entries(points.value).filter(([, value]) => value.result).map(([id, value]) => ({
    templatePointId: id, result: value.result!,
    observation: value.result === "NonCompliant" ? value.observation.trim() || null : null,
  }));
  inspection.value = await inspectionHttpApi.saveDraft(current.id,
    { operatorFirstName: operatorFirstName.value.trim(), answers });
  for (const [pointId, value] of Object.entries(points.value)) {
    if (!value.files.length) continue;
    const answer = inspection.value.answers.find(a => a.templatePointId === pointId);
    if (!answer) throw new Error("Enregistrez la réponse avant ses photographies.");
    for (const file of value.files) {
      const photo = await inspectionHttpApi.uploadPhoto(current.id, answer.id, file);
      value.photoIds.push(photo.id);
      value.files = value.files.filter(item => item !== file);
    }
  }
  inspection.value = await inspectionHttpApi.getById(current.id);
}

async function save(): Promise<boolean> {
  if (busy.value || !checkIdentity()) return false;
  busy.value = true; error.value = "";
  try { await persist(); return true; }
  catch (cause) { error.value = message(cause); return false; }
  finally { busy.value = false; }
}
async function next(): Promise<void> {
  if (currentPoint.value) {
    const value = points.value[currentPoint.value.id];
    if (!value?.result || (value.result === "NonCompliant" &&
      (!value.observation.trim() || !(value.files.length || value.photoIds.length)))) {
      error.value = "Renseignez le résultat et, en cas de non-conformité, une observation et une photographie.";
      return;
    }
  }
  if (await save()) { step.value = Math.min(step.value + 1, finalStep.value); window.scrollTo(0, 0); }
}
async function removePhoto(pointId: string, photoId: string): Promise<void> {
  if (!inspection.value || busy.value) return;
  busy.value = true; error.value = "";
  try {
    await inspectionHttpApi.removePhoto(inspection.value.id, photoId);
    points.value[pointId].photoIds = points.value[pointId].photoIds.filter(id => id !== photoId);
    inspection.value = await inspectionHttpApi.getById(inspection.value.id);
  } catch (cause) { error.value = message(cause); }
  finally { busy.value = false; }
}
async function fileBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error("Impossible de lire la signature."));
    reader.onload = () => resolve(String(reader.result).split(",")[1] ?? "");
    reader.readAsDataURL(file);
  });
}
async function validate(): Promise<void> {
  if (!accepted.value || !signerName.value.trim() || !signatureFile.value) {
    error.value = "Le nom du signataire, son accord et sa signature sont obligatoires."; return;
  }
  if (orderedPoints.value.some(point => {
    const value = points.value[point.id];
    return !value?.result || (value.result === "NonCompliant" &&
      (!value.observation.trim() || !value.files.length && !value.photoIds.length));
  })) { error.value = "Complétez tous les points avant la validation."; return; }
  if (!await save() || !inspection.value) return;
  busy.value = true;
  try {
    await inspectionHttpApi.sign(inspection.value.id, { signerName: signerName.value.trim(),
      accepted: accepted.value, signaturePngBase64: await fileBase64(signatureFile.value) });
    await inspectionHttpApi.validate(inspection.value.id);
    await router.push({ name: "Inspections" });
  } catch (cause) { error.value = message(cause); }
  finally { busy.value = false; }
}
onMounted(() => { void load(); });
</script>

<template>
  <main class="wizard">
    <h1>État des lieux de {{ isDeparture ? "départ" : "retour" }}</h1>
    <p v-if="loading" role="status">Chargement…</p>
    <div v-else-if="!template"><p role="alert">{{ error }}</p><button type="button" @click="load">Réessayer</button></div>
    <template v-else>
      <p>Étape {{ step + 1 }} sur {{ finalStep + 1 }} - {{ equipment?.name }}</p>
      <progress :value="step" :max="finalStep" />
      <template v-if="step === 0">
      <InspectionIdentity v-model:operator-first-name="operatorFirstName"
        v-model:client="client" :departure="isDeparture" :locked="Boolean(inspection)" />
      <section v-if="departure" class="wizard__previous">
        <h2>Défauts relevés au départ</h2>
        <ul v-if="departure.answers.some(answer => answer.result === 'NonCompliant')">
          <li v-for="answer in departure.answers.filter(answer => answer.result === 'NonCompliant')" :key="answer.id">
            <strong>{{ departureTemplate?.points.find(point => point.id === answer.templatePointId)?.description ?? 'Point de contrôle' }}</strong>
            : {{ answer.observation }}
          </li>
        </ul>
        <p v-else>Aucun défaut relevé au départ.</p>
      </section>
      </template>
      <section v-else-if="currentPoint">
        <h2>Point {{ step }} sur {{ orderedPoints.length }}</h2>
        <p class="wizard__point-detail">{{ currentPoint.descriptionDetail }}</p>
        <TemplateIllustration :photo-id="currentPoint.photoId" />
        <InspectionPoint :id="currentPoint.id" :label="currentPoint.description"
          :value="points[currentPoint.id]" :inspection-id="inspection?.id" :disabled="busy"
          @update="points[currentPoint.id] = $event"
          @remove-photo="removePhoto(currentPoint.id, $event)" />
      </section>
      <section v-else class="wizard__summary">
        <h2>Récapitulatif et signature</h2>
        <p>Client : {{ client.displayName }} - commande {{ client.commandValue }}</p>
        <ul><li v-for="point in orderedPoints" :key="point.id">
          {{ point.description }} : {{ points[point.id]?.result === 'Compliant' ? 'Conforme' : 'Non conforme' }}
          <span v-if="points[point.id]?.observation"> - {{ points[point.id].observation }}</span>
          <button type="button" @click="step = orderedPoints.indexOf(point) + 1">Modifier</button>
        </li></ul>
        <label>Nom du client signataire *<input v-model="signerName" required /></label>
        <label><input v-model="accepted" type="checkbox" /> Je reconnais avoir pris connaissance du présent état des lieux et en accepte le contenu.</label>
        <InspectionSignaturePad @change="signatureFile = $event" />
        <button type="button" :disabled="busy" @click="validate">Signer et valider l’état des lieux</button>
      </section>
      <p v-if="error" role="alert" class="wizard__error">{{ error }}</p>
      <nav class="wizard__actions">
        <button v-if="step > 0" type="button" :disabled="busy" @click="step--">Précédent</button>
        <button type="button" :disabled="busy" @click="save">Enregistrer le brouillon</button>
        <button v-if="step < finalStep" type="button" :disabled="busy" @click="next">Suivant</button>
      </nav>
    </template>
  </main>
</template>
<style scoped lang="scss">
.wizard__point-detail { white-space: pre-wrap; tab-size: 4; }
.wizard { display: grid; gap: 1.25rem; max-width: 48rem; margin-inline: auto; }
.wizard h1 { margin: 0; }
.wizard progress { width: 100%; accent-color: var(--color-primary); }
.wizard__summary { display: grid; gap: .9rem; }
.wizard__summary label { display: grid; gap: .4rem; }
.wizard__summary label:has([type="checkbox"]) { display: flex; align-items: flex-start; }
.wizard__summary input:not([type="checkbox"]) { min-height: 2.75rem; padding: .5rem;
  color: var(--color-text); background: var(--color-input-background); border: 1px solid var(--color-border); border-radius: .5rem; }
.wizard__summary li { margin-block: .5rem; }
.wizard__actions { display: flex; flex-wrap: wrap; gap: .75rem; }
button { min-height: 2.75rem; padding: .55rem .85rem; border: 1px solid var(--color-border);
  border-radius: .5rem; color: var(--color-text); background: var(--color-surface); font: inherit; cursor: pointer; }
button:focus-visible { outline: 3px solid var(--color-focus); }
button:disabled { opacity: .55; cursor: wait; }
.wizard__error { color: var(--color-danger); }
.wizard__previous { padding: 1rem; border-radius: .6rem; background: var(--color-warning-soft); }
</style>
