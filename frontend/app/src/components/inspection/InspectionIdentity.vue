<script setup lang="ts">
import { computed, reactive } from "vue";
import type { ClientInputDto } from "../../api/inspections";
import {
  validateAgencyCode,
  validateCommandNumber,
  validateEmail,
  validatePhoneNumber,
} from "../../utils/validators.ts";
import CommandNumberInput from "../common/input/CommandNumberInput.vue";
import BaseInput from "../common/input/BaseInput.vue";
import { normalizeCommandNumber } from "../common/input/commandNumber";

const props = defineProps<{
  operatorFirstName: string;
  client: ClientInputDto;
  departure: boolean;
  locked: boolean;
  expectedAgencyCode?: number | null;
  error?: string;
  validationRequested?: boolean;
}>();

const emit = defineEmits<{
  "update:operatorFirstName": [value: string];
  "update:client": [value: ClientInputDto];
}>();

function update<K extends keyof ClientInputDto>(key: K, value: ClientInputDto[K]): void {
  emit("update:client", { ...props.client, [key]: value });
}

type ValidatedField = "operator" | "name" | "email" | "phone" | "command";
const touched = reactive<Record<ValidatedField, boolean>>({
  operator: false,
  name: false,
  email: false,
  phone: false,
  command: false,
});
const operatorError = computed(() =>
  touched.operator || props.validationRequested
    ? props.operatorFirstName.trim()
      ? undefined
      : "Veuillez saisir le prénom de l’opérateur."
    : undefined,
);
const nameError = computed(() =>
  touched.name || props.validationRequested
    ? props.client.displayName.trim()
      ? undefined
      : "Veuillez saisir le nom du client."
    : undefined,
);
const emailError = computed(() =>
  touched.email || props.validationRequested ? validateEmail(props.client.email) : undefined,
);
const phoneError = computed(() =>
  touched.phone || props.validationRequested ? validatePhoneNumber(props.client.phone) : undefined,
);
const commandError = computed(() =>
  touched.command || props.validationRequested
    ? props.expectedAgencyCode && props.client.agencyCode !== props.expectedAgencyCode
      ? "Le code agence doit correspondre à l’agence de l’équipement."
      : (validateAgencyCode(props.client.agencyCode) ??
        validateCommandNumber(
          normalizeCommandNumber(props.client.commandValue) ?? props.client.commandValue,
        ))
    : undefined,
);
</script>

<template>
  <section class="identity">
    <h2>Identification</h2>
    <BaseInput
      id="inspection-operator"
      :model-value="operatorFirstName"
      label="Prénom de l’opérateur"
      autocomplete="given-name"
      required
      :error="operatorError"
      @update:model-value="emit('update:operatorFirstName', $event)"
      @blur="touched.operator = true"
    />
    <template v-if="departure">
      <BaseInput
        id="client-name"
        :model-value="client.displayName"
        label="Nom du client"
        :disabled="locked"
        required
        autocomplete="name"
        :error="nameError"
        @update:model-value="update('displayName', $event)"
        @blur="touched.name = true"
      />
      <BaseInput
        id="client-email"
        :model-value="client.email"
        label="Adresse électronique"
        type="email"
        :disabled="locked"
        required
        autocomplete="email"
        :error="emailError"
        @update:model-value="update('email', $event)"
        @blur="touched.email = true"
      />
      <BaseInput
        id="client-phone"
        :model-value="client.phone"
        label="Téléphone"
        type="tel"
        :disabled="locked"
        required
        autocomplete="tel"
        :error="phoneError"
        @update:model-value="update('phone', $event)"
        @blur="touched.phone = true"
      />
      <CommandNumberInput
        :model-value="client.commandValue"
        :agency-code="client.agencyCode"
        :disabled="locked"
        :error="commandError ?? undefined"
        @update:model-value="update('commandValue', $event)"
        @update:agency-code="update('agencyCode', $event)"
        @focusout="touched.command = true"
      />
    </template>
    <p v-else>
      Client : <strong>{{ client.displayName }}</strong> - commande {{ client.commandValue }}
    </p>
    <p
      v-if="error"
      role="alert"
      class="identity__error"
    >
      {{ error }}
    </p>
  </section>
</template>

<style scoped lang="scss">
.identity {
  display: grid;
  gap: 0.9rem;
}
.identity h2 {
  margin: 0;
}
.identity__error {
  color: var(--color-danger);
}
</style>
