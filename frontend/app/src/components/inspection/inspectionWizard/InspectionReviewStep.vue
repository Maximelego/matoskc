<script setup lang="ts">
import type { ClientInputDto, InspectionTemplateDto } from "../../../api/inspections";
import CopyButton from "../../common/copy/CopyButton.vue";
import BaseInput from "../../common/input/BaseInput.vue";
import BaseIcon from "../../common/icon/BaseIcon.vue";
import BaseSpinner from "../../common/spinner/BaseSpinner.vue";
import type { PointValue } from "../InspectionPoint.vue";
import InspectionSignaturePad from "../InspectionSignaturePad.vue";
import type { KnownDefectValue } from "./InspectionKnownDefectsStep.vue";

defineProps<{
  client: ClientInputDto;
  points: InspectionTemplateDto["points"];
  values: Record<string, PointValue>;
  knownDefects: KnownDefectValue[];
  knownOperatorNames: Record<string, string>;
  operatorFirstName: string;
  signerName: string;
  accepted: boolean;
  busy: boolean;
  validationRequested: boolean;
}>();
const emit = defineEmits<{
  "update:signerName": [value: string];
  "update:accepted": [value: boolean];
  signature: [file: File | null];
  edit: [pointId: string];
  editKnown: [];
  validate: [];
}>();
</script>

<template>
  <section class="review-step">
    <h2>Récapitulatif et signature</h2>
    <div class="review-step__client">
      <strong
        >Client : {{ client.displayName }} - {{ client.agencyCode }}/{{
          client.commandValue
        }}</strong
      >
      <CopyButton
        :value="client.commandValue"
        variant="full"
        label="Copier le numéro de commande"
      />
    </div>

    <section>
      <h3>Défauts préexistants</h3>
      <p v-if="!knownDefects.length">Aucun défaut connu.</p>
      <ul
        v-else
        class="review-step__list"
      >
        <li
          v-for="defect in knownDefects"
          :key="defect.defectId"
        >
          <strong>{{ defect.description }}</strong> :
          <span
            >constaté par {{ knownOperatorNames[defect.defectId] ?? "opérateur non renseigné" }} ·
          </span>
          {{
            defect.result === "Unchanged"
              ? "Toujours présent, inchangé"
              : defect.result === "Worsened"
                ? "Aggravé"
                : defect.result === "MaintenanceCheckRequired"
                  ? "À vérifier par la maintenance"
                  : "À confirmer"
          }}
          <span v-if="defect.result === 'Worsened'">
            {{ defect.observation }} · Aggravation constatée par {{ operatorFirstName }}
          </span>
        </li>
      </ul>
      <button
        type="button"
        :disabled="busy"
        @click="emit('editKnown')"
      >
        Modifier les confirmations
      </button>
    </section>
    <section>
      <h3>Nouveaux défauts et points contrôlés</h3>
      <ul class="review-step__list">
        <li
          v-for="point in points"
          :key="point.id"
          :class="
            values[point.id]?.result === 'Compliant'
              ? 'review-step__list-item--compliant'
              : 'review-step__list-item--non-compliant'
          "
        >
          <div>
            <strong>{{ point.description }}</strong> :
            {{
              values[point.id]?.result === "Compliant"
                ? "Aucun nouveau défaut"
                : `${values[point.id]?.findings.length ?? 0} nouveau(x) défaut(s)`
            }}
            <ul v-if="values[point.id]?.findings.length">
              <li
                v-for="finding in values[point.id].findings"
                :key="finding.id"
              >
                {{ finding.observation }} · Constaté par {{ operatorFirstName }}
              </li>
            </ul>
          </div>
          <button
            type="button"
            :disabled="busy"
            @click="emit('edit', point.id)"
          >
            <BaseIcon
              name="edit"
              :size="24"
            />
          </button>
        </li>
      </ul>
    </section>

    <BaseInput
      id="inspection-signer-name"
      :model-value="signerName"
      label="Nom du client signataire"
      :disabled="busy"
      required
      :error="
        validationRequested && !signerName.trim()
          ? 'Le nom du signataire est obligatoire.'
          : undefined
      "
      @update:model-value="emit('update:signerName', $event)"
    />
    <label class="review-step__acceptance">
      <input
        type="checkbox"
        :checked="accepted"
        :disabled="busy"
        @change="emit('update:accepted', ($event.target as HTMLInputElement).checked)"
      />
      Je reconnais avoir pris connaissance du présent état des lieux et en accepte le contenu.
    </label>
    <InspectionSignaturePad @change="emit('signature', $event)" />
    <div class="review-step__submit">
      <button
        type="button"
        :disabled="busy"
        @click="emit('validate')"
      >
        Signer et valider l’état des lieux
      </button>
      <BaseSpinner v-if="busy" />
      <span v-else></span>
    </div>
  </section>
</template>

<style scoped lang="scss">
.review-step {
  display: grid;
  gap: 0.9rem;
  min-width: 0;
  overflow-wrap: anywhere;
}
.review-step label {
  display: grid;
  gap: 0.4rem;
}
.review-step__client {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.review-step__acceptance {
  display: flex !important;
  align-items: flex-start;
}
.review-step input:not([type="checkbox"]) {
  min-height: 2.75rem;
  padding: 0.5rem;
  color: var(--color-text);
  background: var(--color-input-background);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
}
.review-step {
  ul {
    list-style: none;
    padding: 0;
    display: flex;
    flex-direction: column;
  }

  li {
    margin-left: 1rem;
    margin-right: 1rem;
    display: flex;
    justify-content: space-between;
    margin-block: 0.5rem;
    gap: 0.5rem;
    display: flex;
    align-items: center;

    &.review-step__list-item--compliant {
      color: var(--color-success);
    }
    &.review-step__list-item--non-compliant {
      color: var(--color-danger);
    }
  }

  button {
    min-height: 2.75rem;
    padding: 0.55rem 0.85rem;
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    color: var(--color-text);
    background: var(--color-surface);
    cursor: pointer;
  }
}
.review-step button:focus-visible {
  outline: 3px solid var(--color-focus);
}
.review-step button:disabled {
  opacity: 0.55;
  cursor: wait;
}
.review-step button:hover:not(:disabled) {
  background: var(--color-secondary-soft-hover);
  transition: background 150ms ease-in-out;
}

.review-step__submit {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

@media (max-width: 42rem) {
  .review-step input:not([type="checkbox"]),
  .review-step button {
    min-height: 3rem;
  }
}
</style>
