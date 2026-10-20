<script setup lang="ts">
import type { ClientInputDto, InspectionTemplateDto } from "../../../api/inspections";
import CopyButton from "../../common/copy/CopyButton.vue";
import BaseIcon from "../../common/icon/BaseIcon.vue";
import type { PointValue } from "../InspectionPoint.vue";
import InspectionSignaturePad from "../InspectionSignaturePad.vue";

defineProps<{
  client: ClientInputDto;
  points: InspectionTemplateDto["points"];
  values: Record<string, PointValue>;
  signerName: string;
  accepted: boolean;
  busy: boolean;
}>();
const emit = defineEmits<{
  "update:signerName": [value: string];
  "update:accepted": [value: boolean];
  signature: [file: File | null];
  edit: [pointId: string];
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
        label="Copier le numéro de commande"
      />
    </div>

    <ul class="review-step__list">
      <li
        v-for="point in points"
        :key="point.id"
        :class="[
          values[point.id]?.result === 'Compliant'
            ? 'review-step__list-item--compliant'
            : 'review-step__list-item--non-compliant',
        ]"
      >
        {{ point.description }} :
        {{ values[point.id]?.result === "Compliant" ? "Conforme" : "Non conforme" }}
        <span v-if="values[point.id]?.observation"> - {{ values[point.id].observation }}</span>
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

    <label
      >Nom du client signataire *
      <input
        :value="signerName"
        :disabled="busy"
        required
        @input="emit('update:signerName', ($event.target as HTMLInputElement).value)"
      />
    </label>
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
    <button
      type="button"
      :disabled="busy"
      @click="emit('validate')"
    >
      Signer et valider l’état des lieux
    </button>
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
@media (max-width: 42rem) {
  .review-step input:not([type="checkbox"]),
  .review-step button {
    min-height: 3rem;
  }
}
</style>
