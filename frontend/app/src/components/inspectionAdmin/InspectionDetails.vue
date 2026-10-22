<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from "vue";
import { defectsApi } from "../../api/defects";
import type { InspectionDto, InspectionTemplateDto, RentalDto } from "../../api/inspections";
import { inspectionHttpApi } from "../../api/inspections";
import CopyButton from "../common/copy/CopyButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import InspectionSummaryKnownDefects from "./InspectionSummaryKnownDefects.vue";
import InspectionSummaryPoints from "./InspectionSummaryPoints.vue";

const props = defineProps<{ inspection: InspectionDto; equipmentName?: string }>();
const rental = ref<RentalDto | null>(null);
const template = ref<InspectionTemplateDto | null>(null);
const photos = ref<Record<string, string>>({});
const knownOperatorNames = ref<Record<string, string>>({});
const signatureUrl = ref("");
const error = ref("");
const loading = ref(false);
const downloading = ref(false);
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "long", timeStyle: "short" });
const typeLabel = computed(() => (props.inspection.type === "Departure" ? "Départ" : "Retour"));
const statusLabel = computed(
  () =>
    ({
      Validated: "Validé",
      Draft: "Brouillon",
      Cancelled: "Annulé",
    })[props.inspection.status],
);

function release(): void {
  Object.values(photos.value).forEach((url) => URL.revokeObjectURL(url));
  if (signatureUrl.value) URL.revokeObjectURL(signatureUrl.value);
  photos.value = {};
  signatureUrl.value = "";
}

watch(
  () => props.inspection.id,
  async (id, _old, onCleanup) => {
    release();
    rental.value = null;
    template.value = null;
    knownOperatorNames.value = {};
    loading.value = true;
    error.value = "";
    let cancelled = false;
    onCleanup(() => {
      cancelled = true;
      release();
    });
    try {
      const [currentRental, currentTemplate, defects] = await Promise.all([
        inspectionHttpApi.getRental(props.inspection.rentalId),
        inspectionHttpApi.getTemplate(props.inspection.templateId),
        defectsApi.listAll(props.inspection.equipmentId).catch(() => []),
      ]);
      if (cancelled) return;
      rental.value = currentRental;
      template.value = currentTemplate;
      knownOperatorNames.value = Object.fromEntries(
        defects.map((defect) => [defect.id, defect.operatorFirstName]),
      );
      const ids = [
        ...new Set([
          ...props.inspection.knownDefects.flatMap((defect) => [
            ...defect.historicalPhotoIds,
            ...defect.photoIds,
          ]),
          ...props.inspection.findings.flatMap((finding) => finding.photoIds),
        ]),
      ];
      const [media, signature] = await Promise.all([
        Promise.all(
          ids.map(async (photoId) => {
            try {
              return [
                photoId,
                URL.createObjectURL(await inspectionHttpApi.getPhoto(id, photoId)),
              ] as const;
            } catch {
              return null;
            }
          }),
        ),
        props.inspection.signedAt
          ? inspectionHttpApi
              .getSignature(id)
              .then((blob) => URL.createObjectURL(blob))
              .catch(() => "")
          : Promise.resolve(""),
      ]);
      if (cancelled) {
        media.forEach((entry) => {
          if (entry) URL.revokeObjectURL(entry[1]);
        });
        if (signature) URL.revokeObjectURL(signature);
        return;
      }
      photos.value = Object.fromEntries(
        media.filter((entry): entry is NonNullable<typeof entry> => entry !== null),
      );
      signatureUrl.value = signature;
    } catch {
      if (!cancelled) error.value = "Les informations complémentaires n’ont pas pu être chargées.";
    } finally {
      if (!cancelled) loading.value = false;
    }
  },
  { immediate: true },
);
onUnmounted(release);

async function downloadReport(): Promise<void> {
  if (downloading.value) return;
  downloading.value = true;
  error.value = "";
  try {
    const url = URL.createObjectURL(await inspectionHttpApi.getReport(props.inspection.id));
    const link = document.createElement("a");
    link.href = url;
    link.download = `etat-des-lieux-${props.inspection.id}.pdf`;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  } catch {
    error.value = "Impossible de télécharger le PDF.";
  } finally {
    downloading.value = false;
  }
}
</script>

<template>
  <article class="summary">
    <header class="summary__hero">
      <div class="summary__hero-line">
        <p class="summary__eyebrow">État des lieux de {{ typeLabel.toLowerCase() }}</p>
        <span
          class="summary__status"
          :class="`summary__status--${inspection.status}`"
          >{{ statusLabel }}</span
        >
      </div>
      <h3>{{ equipmentName || "Récapitulatif de l’équipement" }}</h3>
      <p>
        {{ date.format(new Date(inspection.validatedAt ?? inspection.startedAt)) }} · Opérateur
        {{ inspection.operatorFirstName }}
      </p>
      <div
        class="summary__counts"
        aria-label="Résumé des constats"
      >
        <span
          ><strong>{{ inspection.knownDefects.length }}</strong> défaut(s) connu(s)</span
        >
        <span
          ><strong>{{ inspection.findings.length }}</strong> nouveau(x) défaut(s)</span
        >
        <span
          ><strong>{{ inspection.answers.length }}</strong> point(s) renseigné(s)</span
        >
      </div>
      <button
        v-if="inspection.hasReport"
        type="button"
        class="summary__report"
        :disabled="downloading"
        @click="downloadReport"
      >
        <BaseIcon
          name="download"
          :size="19"
        />
        {{ downloading ? "Préparation du PDF…" : "Télécharger le PDF" }}
      </button>
    </header>

    <p
      v-if="loading"
      role="status"
      class="summary__notice"
    >
      Chargement des informations complémentaires…
    </p>
    <p
      v-if="error"
      role="alert"
      class="summary__error"
    >
      {{ error }}
    </p>

    <section
      class="summary__section"
      aria-labelledby="summary-client-title"
    >
      <h3 id="summary-client-title">Client et commande</h3>
      <dl
        v-if="rental"
        class="summary__meta"
      >
        <div>
          <dt>Client</dt>
          <dd>{{ rental.client.displayName }}</dd>
        </div>
        <div>
          <dt>Numéro de commande</dt>
          <dd class="summary__copy">
            {{ rental.client.agencyCode }}/{{ rental.client.commandValue }}
            <CopyButton
              :value="`${rental.client.agencyCode}/${rental.client.commandValue}`"
              variant="icon-only"
              label="le numéro de commande"
            />
          </dd>
        </div>
        <div>
          <dt>Adresse électronique</dt>
          <dd class="summary__copy">
            {{ rental.client.email }}
            <CopyButton
              :value="rental.client.email"
              variant="icon-only"
              label="l’adresse électronique"
            />
          </dd>
        </div>
        <div>
          <dt>Téléphone</dt>
          <dd class="summary__copy">
            {{ rental.client.phone }}
            <CopyButton
              :value="rental.client.phone"
              variant="icon-only"
              label="le téléphone"
            />
          </dd>
        </div>
      </dl>
      <p
        v-else-if="!loading"
        class="summary__muted"
      >
        Informations client indisponibles.
      </p>
      <p
        v-if="inspection.cancellationReason"
        class="summary__reason"
      >
        Motif d’annulation : {{ inspection.cancellationReason }}
      </p>
    </section>

    <InspectionSummaryKnownDefects
      class="summary__section"
      :defects="inspection.knownDefects"
      :operator-names="knownOperatorNames"
      :current-operator="inspection.operatorFirstName"
      :photos="photos"
      :loading="loading"
    />

    <InspectionSummaryPoints
      v-if="template"
      class="summary__section"
      :points="template.points"
      :answers="inspection.answers"
      :findings="inspection.findings"
      :operator="inspection.operatorFirstName"
      :photos="photos"
      :loading="loading"
    />

    <section
      v-if="inspection.signedAt"
      class="summary__section summary__signature"
      aria-labelledby="summary-signature-title"
    >
      <h3 id="summary-signature-title">Signature du client</h3>
      <p>
        <strong>{{ inspection.signerName }}</strong> ·
        {{ date.format(new Date(inspection.signedAt)) }}
      </p>
      <p
        v-if="inspection.acceptanceText"
        class="summary__muted"
      >
        {{ inspection.acceptanceText }}
      </p>
      <img
        v-if="signatureUrl"
        :src="signatureUrl"
        alt="Signature du client"
      />
    </section>
  </article>
</template>

<style scoped lang="scss">
.summary {
  display: grid;
  gap: 1.35rem;
  min-width: 0;
  overflow-wrap: anywhere;
}
.summary h3,
.summary p {
  margin: 0;
}
.summary__hero {
  display: grid;
  gap: 0.7rem;
  padding: 1.1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.8rem;
  background: var(--color-surface-secondary);
}
.summary__hero-line {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.summary__eyebrow {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}
.summary__hero h3 {
  font-size: var(--font-size-xl);
}
.summary__hero > p {
  color: var(--color-text-secondary);
}
.summary__status {
  padding: 0.3rem 0.7rem;
  border-radius: 999px;
  font-size: var(--font-size-sm);
  background: var(--color-surface);
}
.summary__status--Validated {
  color: var(--color-success);
  background: var(--color-success-soft);
}
.summary__status--Cancelled {
  color: var(--color-danger);
  background: var(--color-danger-soft);
}
.summary__counts {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.25rem;
}
.summary__counts span {
  padding: 0.5rem 0.65rem;
  border-radius: 0.5rem;
  background: var(--color-surface);
}
.summary__report {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  justify-self: start;
  min-height: 2.75rem;
  padding: 0.55rem 0.9rem;
  border: 1px solid var(--color-primary);
  border-radius: 0.5rem;
  color: var(--color-text-on-primary);
  background: var(--color-primary);
  font: inherit;
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
}
.summary__report:hover:not(:disabled) {
  background: var(--color-primary-hover);
}
.summary__report:disabled {
  opacity: 0.6;
  cursor: wait;
}
.summary__report:focus-visible {
  outline: 3px solid var(--color-focus);
  outline-offset: 2px;
}
.summary__section {
  display: grid;
  gap: 0.85rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--color-border);
}
.summary__meta {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.85rem 1.25rem;
  margin: 0;
}
.summary__meta dt {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
.summary__meta dd {
  margin: 0.2rem 0 0;
  font-weight: var(--font-weight-semibold);
}
.summary__copy {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.35rem;
}
.summary__muted,
.summary__notice {
  color: var(--color-text-secondary);
}
.summary__reason {
  padding: 0.8rem;
  border-radius: 0.5rem;
  background: var(--color-danger-soft);
}
.summary__error {
  color: var(--color-danger);
}
.summary__signature img {
  max-width: min(100%, 24rem);
  max-height: 12rem;
  object-fit: contain;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
}
@media (max-width: 42rem) {
  .summary__meta {
    grid-template-columns: minmax(0, 1fr);
  }
  .summary__report {
    justify-self: stretch;
  }
}
</style>
