<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { defectsApi } from "../../api/defects";
import type { InspectionDto, InspectionTemplateDto, RentalDto } from "../../api/inspections";
import { inspectionHttpApi } from "../../api/inspections";
import CopyButton from "../common/copy/CopyButton.vue";
import TemplateIllustration from "../inspection/TemplateIllustration.vue";

const props = defineProps<{ inspection: InspectionDto }>();
const rental = ref<RentalDto | null>(null);
const template = ref<InspectionTemplateDto | null>(null);
const photos = ref<Record<string, string>>({});
const knownOperatorNames = ref<Record<string, string>>({});
const signatureUrl = ref("");
const error = ref("");
const loading = ref(false);
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "long", timeStyle: "short" });

function answerFor(pointId: string) {
  return props.inspection.answers.find((answer) => answer.templatePointId === pointId);
}

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
    knownOperatorNames.value = {};
    loading.value = true;
    error.value = "";
    let cancelled = false;
    onCleanup(() => {
      cancelled = true;
      release();
    });
    try {
      const [r, t, defects] = await Promise.all([
        inspectionHttpApi.getRental(props.inspection.rentalId),
        inspectionHttpApi.getTemplate(props.inspection.templateId),
        defectsApi.listAll(props.inspection.equipmentId).catch(() => []),
      ]);
      if (cancelled) return;
      rental.value = r;
      template.value = t;
      knownOperatorNames.value = Object.fromEntries(
        defects.map((defect) => [defect.id, defect.operatorFirstName]),
      );
      const entries = await Promise.all(
        [
          ...props.inspection.knownDefects.flatMap((defect) => [
            ...defect.historicalPhotoIds,
            ...defect.photoIds,
          ]),
          ...props.inspection.findings.flatMap((finding) => finding.photoIds),
        ].map(
          async (photoId) =>
            [photoId, URL.createObjectURL(await inspectionHttpApi.getPhoto(id, photoId))] as const,
        ),
      );
      if (cancelled) {
        entries.forEach(([, url]) => URL.revokeObjectURL(url));
        return;
      }
      photos.value = Object.fromEntries(entries);
      if (props.inspection.signedAt) {
        const url = URL.createObjectURL(await inspectionHttpApi.getSignature(id));
        if (cancelled) URL.revokeObjectURL(url);
        else signatureUrl.value = url;
      }
    } catch {
      if (!cancelled)
        error.value = "Certains éléments de l’état des lieux n’ont pas pu être chargés.";
    } finally {
      if (!cancelled) loading.value = false;
    }
  },
  { immediate: true },
);
onUnmounted(release);

async function downloadReport(): Promise<void> {
  try {
    const url = URL.createObjectURL(await inspectionHttpApi.getReport(props.inspection.id));
    const link = document.createElement("a");
    link.href = url;
    link.download = `etat-des-lieux-${props.inspection.id}.pdf`;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  } catch {
    error.value = "Impossible de télécharger le PDF.";
  }
}
</script>
<template>
  <article class="detail">
    <p
      v-if="loading"
      role="status"
    >
      Chargement du détail…
    </p>
    <p
      v-if="error"
      role="alert"
    >
      {{ error }}
    </p>
    <dl>
      <div>
        <dt>Type</dt>
        <dd>{{ inspection.type === "Departure" ? "Départ" : "Retour" }}</dd>
      </div>
      <div>
        <dt>Statut</dt>
        <dd :class="inspection.status">
          {{
            inspection.status === "Validated"
              ? "Validé"
              : inspection.status === "Cancelled"
                ? "Annulé"
                : "Brouillon"
          }}
        </dd>
      </div>
      <div>
        <dt>Opérateur</dt>
        <dd>{{ inspection.operatorFirstName }}</dd>
      </div>
      <div>
        <dt>Date</dt>
        <dd>{{ date.format(new Date(inspection.validatedAt ?? inspection.startedAt)) }}</dd>
      </div>
      <div v-if="inspection.cancellationReason">
        <dt>Motif d’annulation</dt>
        <dd>{{ inspection.cancellationReason }}</dd>
      </div>
      <div v-if="rental">
        <dt>Client</dt>
        <dd>{{ rental.client.displayName }}</dd>
      </div>
      <div v-if="rental">
        <dt>Numéro de commande</dt>
        <dd>
          {{ rental.client.commandValue }}
          <CopyButton
            :value="rental.client.commandValue"
            variant="icon-only"
            label="le numéro de commande"
          />
        </dd>
      </div>
      <div v-if="rental">
        <dt>Email</dt>
        <dd>
          {{ rental.client.email }}
          <CopyButton
            :value="rental.client.email"
            variant="icon-only"
            label="l’adresse électronique"
          />
        </dd>
      </div>
      <div v-if="rental">
        <dt>Téléphone</dt>
        <dd>
          {{ rental.client.phone }}
          <CopyButton
            :value="rental.client.phone"
            variant="icon-only"
            label="le téléphone"
          />
        </dd>
      </div>
    </dl>
    <section>
      <h3>Défauts préexistants</h3>
      <p v-if="!inspection.knownDefects.length">Aucun défaut connu.</p>
      <ul v-else>
        <li
          v-for="defect in inspection.knownDefects"
          :key="defect.id"
        >
          <strong>{{ defect.description }}</strong> : {{ defect.historicalObservation }}
          <p>Constaté par {{ knownOperatorNames[defect.defectId] ?? "opérateur non renseigné" }}</p>
          <p>Constaté le {{ date.format(new Date(defect.observedAt)) }}</p>
          <p>
            {{
              defect.result === "Unchanged"
                ? "Toujours présent, inchangé"
                : defect.result === "Worsened"
                  ? "Aggravé"
                  : defect.result === "MaintenanceCheckRequired"
                    ? "À vérifier par la maintenance"
                    : "À confirmer"
            }}
          </p>
          <p v-if="defect.observation">
            Nouvelle observation : {{ defect.observation }} · Aggravation constatée par
            {{ inspection.operatorFirstName }}
          </p>
          <a
            v-for="photoId in defect.historicalPhotoIds"
            :key="photoId"
            :href="photos[photoId]"
            target="_blank"
            rel="noopener noreferrer"
            >Photo historique</a
          >
          <a
            v-for="photoId in defect.photoIds"
            :key="photoId"
            :href="photos[photoId]"
            target="_blank"
            rel="noopener noreferrer"
            >Photo de l’aggravation</a
          >
        </li>
      </ul>
    </section>
    <section>
      <h3>Points contrôlés et nouveaux défauts</h3>
      <ul>
        <li
          v-for="point in [...(template?.points ?? [])].sort((a, b) => a.position - b.position)"
          :key="point.id"
        >
          <strong>{{ point.description }}</strong>
          <p class="detail__point-detail">{{ point.descriptionDetail }}</p>
          <TemplateIllustration :photo-id="point.photoId" />
          <template v-if="answerFor(point.id)">
            :
            {{
              answerFor(point.id)?.result === "Compliant"
                ? "Aucun nouveau défaut"
                : "Nouveau défaut constaté"
            }}
            <ul v-if="inspection.findings.some((finding) => finding.templatePointId === point.id)">
              <li
                v-for="finding in inspection.findings.filter(
                  (item) => item.templatePointId === point.id,
                )"
                :key="finding.id"
              >
                {{ finding.observation }}
                <span> · Constaté par {{ inspection.operatorFirstName }}</span>
                <a
                  v-for="photoId in finding.photoIds"
                  :key="photoId"
                  :href="photos[photoId]"
                  target="_blank"
                  rel="noopener noreferrer"
                  >Voir la photographie</a
                >
              </li>
            </ul>
          </template>
          <span v-else> : sans réponse</span>
        </li>
      </ul>
    </section>
    <section v-if="inspection.signedAt">
      <h3>Signature du client</h3>
      <p>{{ inspection.signerName }} - {{ date.format(new Date(inspection.signedAt)) }}</p>
      <p>{{ inspection.acceptanceText }}</p>
      <img
        v-if="signatureUrl"
        :src="signatureUrl"
        alt="Signature du client"
        class="detail__signature"
      />
    </section>
    <button
      v-if="inspection.hasReport"
      type="button"
      @click="downloadReport"
    >
      Télécharger le PDF
    </button>
  </article>
</template>
<style scoped lang="scss">
.detail {
  display: grid;
  gap: 1.25rem;
  overflow-wrap: anywhere;
}
.detail dl {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr));
  gap: 0.9rem;
  margin: 0;
}
.detail dt {
  color: var(--color-text-secondary);
}
.detail dd {
  margin: 0.25rem 0 0;
}
.detail li {
  padding: 0.65rem 0;
  border-bottom: 1px solid var(--color-border);
}
.detail li a {
  display: inline-block;
  margin-right: 0.65rem;
}
.detail__point-detail {
  white-space: pre-wrap;
  tab-size: 4;
}
.detail__signature {
  max-width: 100%;
  max-height: 12rem;
  background: white;
}
button {
  min-height: 2.75rem;
  padding: 0.55rem 0.8rem;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  cursor: pointer;
}
</style>
