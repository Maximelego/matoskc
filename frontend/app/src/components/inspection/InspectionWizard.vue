<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { agenciesApi } from "../../api/agencies";
import { ApiError } from "../../api/client";
import { defectsApi } from "../../api/defects";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import type {
  ClientInputDto,
  InspectionDto,
  KnownDefectDto,
  InspectionTemplateDto,
  RentalDto,
} from "../../api/inspections";
import { inspectionHttpApi } from "../../api/inspections";
import {
  validateAgencyCode,
  validateCommandNumber,
  validateEmail,
  validatePhoneNumber,
} from "../../utils/validators.ts";
import { normalizeCommandNumber } from "../common/input/commandNumber";
import SlideAndFadeLateral from "../common/transition/SlideAndFadeLateral.vue";
import type { PointValue } from "./InspectionPoint.vue";
import type { KnownDefectValue } from "./inspectionWizard/InspectionKnownDefectsStep.vue";
import InspectionKnownDefectsStep from "./inspectionWizard/InspectionKnownDefectsStep.vue";
import InspectionDraftCancellation from "./inspectionWizard/InspectionDraftCancellation.vue";
import InspectionIdentityStep from "./inspectionWizard/InspectionIdentityStep.vue";
import InspectionPointStep from "./inspectionWizard/InspectionPointStep.vue";
import InspectionReviewStep from "./inspectionWizard/InspectionReviewStep.vue";

const props = defineProps<{ equipmentId: string }>();
const router = useRouter();

const equipment = ref<EquipmentDto | null>(null);
const rental = ref<RentalDto | null>(null);
const inspection = ref<InspectionDto | null>(null);
const template = ref<InspectionTemplateDto | null>(null);
const client = ref<ClientInputDto>({
  displayName: "",
  email: "",
  phone: "",
  agencyCode: 83,
  commandValue: "",
});
const operatorFirstName = ref("");
const equipmentAgencyCode = ref<number | null>(null);
const identityValidationRequested = ref(false);
const signatureValidationRequested = ref(false);
const points = ref<Record<string, PointValue>>({});
const knownDefects = ref<KnownDefectValue[]>([]);
const knownOperatorNames = ref<Record<string, string>>({});
const cancellationReason = ref("");
const signerName = ref("");
const accepted = ref(false);
const signatureFile = ref<File | null>(null);
const step = ref(0);
const busy = ref(false);
const loading = ref(true);
const error = ref("");
const isDeparture = computed(() => equipment.value?.status === "Available");
const orderedPoints = computed(() =>
  [...(template.value?.points ?? [])].sort((a, b) => a.position - b.position),
);
const currentPoint = computed(() => orderedPoints.value[step.value - 2]);
const finalStep = computed(() => orderedPoints.value.length + 2);

const defaultPoint = (): PointValue => ({ result: null, findings: [] });

function hydrateKnown(items: KnownDefectDto[]): void {
  knownDefects.value = items.map((item) => ({ ...item, files: [] }));
}
function hydratePoints(current: InspectionDto | null): void {
  points.value = Object.fromEntries(
    (template.value?.points ?? []).map((point) => {
      const answer = current?.answers.find((item) => item.templatePointId === point.id);
      return [
        point.id,
        answer
          ? {
              result: answer.result,
              findings:
                current?.findings
                  .filter((item) => item.templatePointId === point.id)
                  .map((item) => ({ ...item, files: [] })) ?? [],
            }
          : defaultPoint(),
      ];
    }),
  );
}
function completePoint(value: PointValue | undefined): boolean {
  return Boolean(
    value?.result &&
    (value.result === "Compliant" ||
      (value.findings.length > 0 &&
        value.findings.every(
          (finding) =>
            finding.observation.trim() && (finding.files.length || finding.photoIds.length),
        ))),
  );
}
function completeKnown(): boolean {
  return knownDefects.value.every(
    (defect) =>
      defect.result &&
      (defect.result !== "Worsened" ||
        (defect.observation?.trim() && (defect.files.length || defect.photoIds.length))),
  );
}
function updateKnown(defectId: string, patch: Partial<KnownDefectValue>): void {
  knownDefects.value = knownDefects.value.map((defect) =>
    defect.defectId === defectId ? { ...defect, ...patch } : defect,
  );
}

function message(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.status === 401) return "Veuillez vous reconnecter.";
    if (cause.status === 403) return "Vous ne disposez pas des droits nécessaires.";
    if (cause.status === 404) return "Ressource introuvable ou inaccessible.";
    if (cause.status === 409)
      return "Le dossier a changé ou l’agence du client ne correspond pas à celle de l’équipement. Actualisez la page avant de continuer.";
    return cause.message;
  }
  return cause instanceof Error ? cause.message : "Une erreur est survenue.";
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = "";
  equipmentAgencyCode.value = null;

  try {
    const item = await equipmentsApi.getById(props.equipmentId);

    if (item.status !== "Available" && item.status !== "Borrowed")
      throw new Error("Cet équipement ne peut pas faire l’objet d’un état des lieux.");

    equipment.value = item;
    knownOperatorNames.value = Object.fromEntries(
      (await defectsApi.listAll(item.id, "Open")).map((defect) => [
        defect.id,
        defect.operatorFirstName,
      ]),
    );
    const current = await inspectionHttpApi.getCurrentRental(item.id);

    rental.value = current;

    if (item.status === "Borrowed" && !current)
      throw new Error("Aucune location en cours pour cet équipement.");

    if (current) client.value = current.client;

    if (item.status === "Available") {
      const agencies = await agenciesApi.list();
      const owner = agencies.agencies.find((a) => a.id === item.agencyId);
      if (!owner) throw new Error("L’agence de cet équipement est introuvable ou inaccessible.");
      equipmentAgencyCode.value = owner.code;
      if (!current) client.value.agencyCode = owner.code;
    }

    let draft: InspectionDto | null = null;

    if (current) {
      const result = await inspectionHttpApi.list({
        rentalId: current.id,
        type: item.status === "Available" ? "Departure" : "Return",
        status: "Draft",
      });
      const summary = result.inspections[0];
      draft = summary ? await inspectionHttpApi.getById(summary.id) : null;
    }

    inspection.value = draft;
    operatorFirstName.value = draft?.operatorFirstName ?? "";

    // Every new inspection uses the latest category template; an existing draft retains its own template.
    template.value = draft
      ? await inspectionHttpApi.getTemplate(draft.templateId)
      : await inspectionHttpApi.getLatestTemplate(item.equipmentCategoryId);
    hydrateKnown(draft?.knownDefects ?? []);
    hydratePoints(draft);
  } catch (cause) {
    error.value = message(cause);
  } finally {
    loading.value = false;
  }
}

function checkIdentity(): boolean {
  identityValidationRequested.value = true;
  if (!operatorFirstName.value.trim()) {
    error.value = "Veuillez saisir le prénom de l’opérateur.";
    return false;
  }

  if (!isDeparture.value || inspection.value) return true;

  const number = normalizeCommandNumber(client.value.commandValue);

  if (!client.value.displayName.trim()) {
    error.value = "Veuillez saisir le nom du client.";
    return false;
  }
  const validationError =
    validateEmail(client.value.email) ??
    validatePhoneNumber(client.value.phone) ??
    validateAgencyCode(client.value.agencyCode) ??
    validateCommandNumber(number ?? client.value.commandValue);
  if (equipmentAgencyCode.value && client.value.agencyCode !== equipmentAgencyCode.value) {
    error.value = "Le code agence de la commande doit correspondre à l’agence de l’équipement.";
    return false;
  }
  if (validationError) {
    error.value = validationError;
    return false;
  }
  if (!number) return false;
  client.value.commandValue = number;
  return true;
}

async function ensureInspection(): Promise<InspectionDto> {
  if (inspection.value) return inspection.value;

  let created: InspectionDto;

  if (isDeparture.value) {
    created = await inspectionHttpApi.createDeparture({
      equipmentId: props.equipmentId,
      client: {
        ...client.value,
        displayName: client.value.displayName.trim(),
        email: client.value.email.trim(),
        phone: client.value.phone.trim(),
      },
      operatorFirstName: operatorFirstName.value.trim(),
    });
  } else {
    if (!rental.value) throw new Error("Location introuvable.");
    created = await inspectionHttpApi.createReturn(rental.value.id, operatorFirstName.value.trim());
  }
  inspection.value = created;
  hydrateKnown(created.knownDefects);
  template.value = await inspectionHttpApi.getTemplate(created.templateId);
  hydratePoints(created);

  return created;
}

async function persist(): Promise<void> {
  const current = await ensureInspection();
  const activeFindings = Object.entries(points.value).flatMap(([pointId, value]) =>
    value.result === "NonCompliant"
      ? value.findings.map((finding) => ({ ...finding, templatePointId: pointId }))
      : [],
  );
  const activeIds = new Set(activeFindings.map((finding) => finding.id));
  inspection.value = await inspectionHttpApi.saveDraft(current.id, {
    operatorFirstName: operatorFirstName.value.trim(),
    answers: Object.entries(points.value)
      .filter(([, value]) => value.result)
      .map(([templatePointId, value]) => ({
        templatePointId,
        result: value.result!,
        observation: null,
      })),
    knownDefects: knownDefects.value
      .filter((defect) => defect.result)
      .map((defect) => ({
        defectId: defect.defectId,
        result: defect.result!,
        observation: defect.result === "Worsened" ? defect.observation?.trim() || null : null,
      })),
    findings: activeFindings.map(({ id, templatePointId, observation }) => ({
      id,
      templatePointId,
      observation: observation.trim(),
    })),
    removedFindingIds: current.findings
      .filter((finding) => !activeIds.has(finding.id))
      .map((finding) => finding.id),
  });

  for (const defect of knownDefects.value) {
    if (defect.result !== "Worsened") continue;
    const target = inspection.value.knownDefects.find((item) => item.defectId === defect.defectId);
    if (!target) throw new Error("La confirmation du défaut n’a pas été enregistrée.");
    for (const file of [...defect.files]) {
      const photo = await inspectionHttpApi.uploadEvidencePhoto(current.id, target.id, file);
      defect.photoIds.push(photo.id);
      defect.files = defect.files.filter((item) => item !== file);
    }
  }
  for (const finding of activeFindings) {
    const local = points.value[finding.templatePointId]?.findings.find(
      (item) => item.id === finding.id,
    );
    if (!local) continue;
    for (const file of [...local.files]) {
      const photo = await inspectionHttpApi.uploadEvidencePhoto(current.id, finding.id, file);
      local.photoIds.push(photo.id);
      local.files = local.files.filter((item) => item !== file);
    }
  }
  inspection.value = await inspectionHttpApi.getById(current.id);
}

async function save(keepSignature = false): Promise<boolean> {
  if (busy.value || !checkIdentity()) return false;
  busy.value = true;
  error.value = "";
  try {
    await persist();
    if (!keepSignature) {
      accepted.value = false;
      signatureFile.value = null;
    }
    return true;
  } catch (cause) {
    if (cause instanceof ApiError && cause.status === 409 && inspection.value) {
      try {
        const fresh = await inspectionHttpApi.getById(inspection.value.id);
        inspection.value = fresh;
        hydrateKnown(fresh.knownDefects);
        hydratePoints(fresh);
        accepted.value = false;
        signatureFile.value = null;
        step.value = 1;
        error.value =
          "Les défauts du matériel ont changé. Vérifiez à nouveau leurs confirmations et les points avant de signer.";
        return false;
      } catch {
        /* Le message initial reste disponible. */
      }
    }
    error.value = message(cause);
    return false;
  } finally {
    busy.value = false;
  }
}

async function next(): Promise<void> {
  if (step.value === 1 && !completeKnown()) {
    error.value =
      "Confirmez chaque défaut connu. Une aggravation nécessite une observation et une nouvelle photo.";
    return;
  }
  if (currentPoint.value && !completePoint(points.value[currentPoint.value.id])) {
    error.value =
      "Indiquez le résultat et, pour chaque nouveau défaut, une observation et une photographie.";
    return;
  }
  if (await save()) {
    step.value = Math.min(step.value + 1, finalStep.value);
    window.scrollTo(0, 0);
  }
}

async function removePhoto(targetId: string, photoId: string): Promise<void> {
  if (!inspection.value || busy.value) return;
  busy.value = true;
  error.value = "";
  try {
    await inspectionHttpApi.removePhoto(inspection.value.id, photoId);
    const defect = knownDefects.value.find((item) => item.defectId === targetId);
    if (defect) defect.photoIds = defect.photoIds.filter((id) => id !== photoId);
    for (const point of Object.values(points.value)) {
      const finding = point.findings.find((item) => item.id === targetId);
      if (finding) finding.photoIds = finding.photoIds.filter((id) => id !== photoId);
    }
    inspection.value = await inspectionHttpApi.getById(inspection.value.id);
    accepted.value = false;
    signatureFile.value = null;
  } catch (cause) {
    error.value = message(cause);
  } finally {
    busy.value = false;
  }
}

async function cancelDraft(): Promise<void> {
  if (!inspection.value || busy.value || !cancellationReason.value.trim()) return;
  busy.value = true;
  error.value = "";

  try {
    await inspectionHttpApi.cancel(inspection.value.id, cancellationReason.value.trim());
    await router.push({ name: "Inspections" });
  } catch (cause) {
    error.value = message(cause);
  } finally {
    busy.value = false;
  }
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
  signatureValidationRequested.value = true;
  if (!accepted.value || !signerName.value.trim() || !signatureFile.value) {
    error.value = "Le nom du signataire, son accord et sa signature sont obligatoires.";
    return;
  }
  if (
    !completeKnown() ||
    orderedPoints.value.some((point) => !completePoint(points.value[point.id]))
  ) {
    error.value = "Complétez les confirmations et tous les points avant la validation.";
    return;
  }
  if (!(await save(true)) || !inspection.value) return;
  busy.value = true;
  try {
    await inspectionHttpApi.sign(inspection.value.id, {
      signerName: signerName.value.trim(),
      accepted: accepted.value,
      signaturePngBase64: await fileBase64(signatureFile.value),
    });
    await inspectionHttpApi.validate(inspection.value.id);
    await router.push({ name: "Inspections" });
  } catch (cause) {
    if (cause instanceof ApiError && cause.status === 409) {
      const fresh = await inspectionHttpApi.getById(inspection.value.id).catch(() => null);
      if (fresh) {
        inspection.value = fresh;
        hydrateKnown(fresh.knownDefects);
        hydratePoints(fresh);
        step.value = 1;
      }
      accepted.value = false;
      signatureFile.value = null;
      error.value =
        "Le dossier a changé. Vérifiez à nouveau les défauts connus et les points, puis signez de nouveau.";
    } else error.value = message(cause);
  } finally {
    busy.value = false;
  }
}

onMounted(() => {
  void load();
});
</script>

<template>
  <main class="wizard">
    <h1>État des lieux de {{ isDeparture ? "départ" : "retour" }}</h1>
    <p
      v-if="loading"
      role="status"
    >
      Chargement…
    </p>
    <div v-else-if="!template">
      <p role="alert">{{ error }}</p>
      <button
        type="button"
        @click="load"
      >
        Réessayer
      </button>
    </div>
    <template v-else>
      <p>
        Étape {{ step + 1 }} sur {{ finalStep + 1 }} - <strong>{{ equipment?.name }}</strong>
      </p>
      <progress
        :value="step"
        :max="finalStep"
      />
      <SlideAndFadeLateral>
        <InspectionIdentityStep
          v-if="step === 0"
          v-model:operator-first-name="operatorFirstName"
          v-model:client="client"
          :departure="isDeparture"
          :locked="Boolean(inspection)"
          :expected-agency-code="equipmentAgencyCode"
          :validation-requested="identityValidationRequested"
        />
        <InspectionKnownDefectsStep
          v-else-if="step === 1"
          :defects="knownDefects"
          :operator-names="knownOperatorNames"
          :inspection-id="inspection?.id ?? null"
          :disabled="busy"
          @update="updateKnown"
          @remove-photo="removePhoto"
        />
        <InspectionPointStep
          v-else-if="currentPoint"
          :point="currentPoint"
          :position="step - 1"
          :total="orderedPoints.length"
          :value="points[currentPoint.id]"
          :inspection-id="inspection?.id ?? null"
          :disabled="busy"
          @update="points[currentPoint.id] = $event"
          @remove-photo="removePhoto"
        />
        <InspectionReviewStep
          v-else
          v-model:signer-name="signerName"
          v-model:accepted="accepted"
          :client="client"
          :points="orderedPoints"
          :values="points"
          :known-defects="knownDefects"
          :known-operator-names="knownOperatorNames"
          :operator-first-name="operatorFirstName"
          :busy="busy"
          :validation-requested="signatureValidationRequested"
          @signature="signatureFile = $event"
          @edit="step = orderedPoints.findIndex((point) => point.id === $event) + 2"
          @edit-known="step = 1"
          @validate="validate"
        />
      </SlideAndFadeLateral>
      <p
        v-if="error"
        role="alert"
        class="wizard__error"
      >
        {{ error }}
      </p>
      <InspectionDraftCancellation
        v-if="inspection?.status === 'Draft'"
        v-model:reason="cancellationReason"
        :busy="busy"
        @cancel="cancelDraft"
      />
      <nav class="wizard__actions">
        <button
          v-if="step > 0"
          type="button"
          :disabled="busy"
          @click="step--"
        >
          Précédent
        </button>
        <button
          type="button"
          :disabled="busy"
          @click="save()"
        >
          Enregistrer le brouillon
        </button>
        <button
          v-if="step < finalStep"
          type="button"
          :disabled="busy"
          @click="next"
        >
          Suivant
        </button>
      </nav>
    </template>
  </main>
</template>

<style scoped lang="scss">
.wizard {
  display: grid;
  gap: 1.25rem;
  max-width: 48rem;
  min-width: 0;
  margin-inline: auto;
  overflow-wrap: anywhere;
}
.wizard h1 {
  margin: 0;
}
.wizard progress {
  width: 100%;
  accent-color: var(--color-primary);
}
.wizard__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}
button {
  min-height: 2.75rem;
  padding: 0.55rem 0.85rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-surface);
  font: inherit;
  cursor: pointer;
}
button:focus-visible {
  outline: 3px solid var(--color-focus);
}
button:disabled {
  opacity: 0.55;
  cursor: wait;
}
.wizard__error {
  color: var(--color-danger);
}
@media (max-width: 42rem) {
  .wizard__actions button {
    min-height: 3rem;
    flex: 1 1 10rem;
  }
}
</style>
