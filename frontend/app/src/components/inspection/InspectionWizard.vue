<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { agenciesApi } from "../../api/agencies";
import { authSession } from "../../api/auth/session";
import { ApiError } from "../../api/client";
import { defectsApi } from "../../api/defects";
import type { DefectDto } from "../../api/defects/dto";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import type {
  ClientInputDto,
  InspectionDto,
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
const points = ref<Record<string, PointValue>>({});
const openDefects = ref<DefectDto[]>([]);
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
const currentPoint = computed(() => orderedPoints.value[step.value - 1]);

function hasOpenDefect(description: string): boolean {
  return openDefects.value.some(
    (defect) =>
      defect.description.trim().toLocaleLowerCase("fr") ===
      description.trim().toLocaleLowerCase("fr"),
  );
}

const finalStep = computed(() => orderedPoints.value.length + 1);

const defaultPoint = (): PointValue => ({
  result: null,
  observation: "",
  files: [],
  photoIds: [],
  existingDefectId: null,
});

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
  loading.value = true;
  error.value = "";

  try {
    const item = await equipmentsApi.getById(props.equipmentId);

    if (item.status !== "Available" && item.status !== "Borrowed")
      throw new Error("Cet équipement ne peut pas faire l’objet d’un état des lieux.");

    equipment.value = item;
    openDefects.value = [];

    for (let offset = 0; ; offset += 100) {
      const page = await defectsApi.list(item.id, "Open", offset, 100);
      openDefects.value.push(...page.items);
      if (offset + page.items.length >= page.total || !page.items.length) break;
    }

    const current = await inspectionHttpApi.getCurrentRental(item.id);

    rental.value = current;

    if (item.status === "Borrowed" && !current)
      throw new Error("Aucune location en cours pour cet équipement.");

    if (current) client.value = current.client;

    if (item.status === "Available" && !current) {
      const agencies = await agenciesApi.list();
      const own = agencies.agencies.find((a) => a.id === authSession.account.value?.agencyId);
      client.value.agencyCode =
        own?.code ?? agencies.agencies.find((a) => a.code === 83)?.code ?? 83;
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
    points.value = Object.fromEntries(
      template.value.points.map((point) => {
        const answer = draft?.answers.find((a) => a.templatePointId === point.id);
        return [
          point.id,
          answer
            ? {
                result: answer.result,
                observation: answer.observation ?? "",
                photoIds: answer.photoIds,
                existingDefectId: answer.existingDefectId ?? null,
                files: [],
              }
            : defaultPoint(),
        ];
      }),
    );
  } catch (cause) {
    error.value = message(cause);
  } finally {
    loading.value = false;
  }
}

function checkIdentity(): boolean {
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
  template.value = await inspectionHttpApi.getTemplate(created.templateId);

  return created;
}

async function persist(): Promise<void> {
  const current = await ensureInspection();
  const answers = Object.entries(points.value)
    .filter(([, value]) => value.result)
    .map(([id, value]) => ({
      templatePointId: id,
      result: value.result!,
      observation: value.result === "NonCompliant" ? value.observation.trim() || null : null,
      existingDefectId: value.result === "NonCompliant" ? value.existingDefectId : null,
    }));

  inspection.value = await inspectionHttpApi.saveDraft(current.id, {
    operatorFirstName: operatorFirstName.value.trim(),
    answers,
  });

  for (const [pointId, value] of Object.entries(points.value)) {
    if (!value.files.length) continue;

    const answer = inspection.value.answers.find((a) => a.templatePointId === pointId);
    if (!answer) throw new Error("Enregistrez la réponse avant ses photographies.");

    for (const file of value.files) {
      const photo = await inspectionHttpApi.uploadPhoto(current.id, answer.id, file);
      value.photoIds.push(photo.id);
      value.files = value.files.filter((item) => item !== file);
    }
  }
  inspection.value = await inspectionHttpApi.getById(current.id);
}

async function save(): Promise<boolean> {
  if (busy.value || !checkIdentity()) return false;
  if (
    orderedPoints.value.some(
      (point) => points.value[point.id]?.result === "Compliant" && hasOpenDefect(point.description),
    )
  ) {
    error.value =
      "Un défaut encore ouvert ne peut pas être déclaré conforme. Indiquez « Non conforme » ou faites résoudre le défaut lors d’un entretien.";
    return false;
  }

  busy.value = true;
  error.value = "";

  try {
    await persist();
    return true;
  } catch (cause) {
    error.value = message(cause);
    return false;
  } finally {
    busy.value = false;
  }
}

async function next(): Promise<void> {
  if (currentPoint.value) {
    const value = points.value[currentPoint.value.id];

    if (value?.result === "Compliant" && hasOpenDefect(currentPoint.value.description)) {
      error.value =
        "Ce point présente encore un défaut ouvert. Indiquez « Non conforme » jusqu’à sa résolution lors d’un entretien.";
      return;
    }

    if (
      !value?.result ||
      (value.result === "NonCompliant" &&
        (!value.observation.trim() || !(value.files.length || value.photoIds.length)))
    ) {
      error.value =
        "Renseignez le résultat et, en cas de non-conformité, une observation et une photographie.";
      return;
    }
  }

  if (await save()) {
    step.value = Math.min(step.value + 1, finalStep.value);
    window.scrollTo(0, 0);
  }
}

async function removePhoto(pointId: string, photoId: string): Promise<void> {
  if (!inspection.value || busy.value) return;
  busy.value = true;
  error.value = "";

  try {
    await inspectionHttpApi.removePhoto(inspection.value.id, photoId);
    points.value[pointId].photoIds = points.value[pointId].photoIds.filter((id) => id !== photoId);
    inspection.value = await inspectionHttpApi.getById(inspection.value.id);
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
  if (!accepted.value || !signerName.value.trim() || !signatureFile.value) {
    error.value = "Le nom du signataire, son accord et sa signature sont obligatoires.";
    return;
  }
  if (
    orderedPoints.value.some((point) => {
      const value = points.value[point.id];
      return (
        !value?.result ||
        (value.result === "NonCompliant" &&
          (!value.observation.trim() || (!value.files.length && !value.photoIds.length))) ||
        (value.result === "Compliant" && hasOpenDefect(point.description))
      );
    })
  ) {
    error.value = "Complétez tous les points avant la validation.";
    return;
  }
  if (!(await save()) || !inspection.value) return;
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
    error.value = message(cause);
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
          :defects="openDefects"
          :equipment-id="equipmentId"
        />
        <InspectionPointStep
          v-else-if="currentPoint"
          :point="currentPoint"
          :position="step"
          :total="orderedPoints.length"
          :value="points[currentPoint.id]"
          :open-defects="openDefects"
          :inspection-id="inspection?.id ?? null"
          :disabled="busy"
          @update="points[currentPoint.id] = $event"
          @remove-photo="removePhoto(currentPoint.id, $event)"
        />
        <InspectionReviewStep
          v-else
          v-model:signer-name="signerName"
          v-model:accepted="accepted"
          :client="client"
          :points="orderedPoints"
          :values="points"
          :busy="busy"
          @signature="signatureFile = $event"
          @edit="step = orderedPoints.findIndex((point) => point.id === $event) + 1"
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
          @click="save"
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
