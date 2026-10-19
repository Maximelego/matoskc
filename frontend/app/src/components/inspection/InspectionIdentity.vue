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
import { normalizeCommandNumber } from "../common/input/commandNumber";

const props = defineProps<{
  operatorFirstName: string;
  client: ClientInputDto;
  departure: boolean;
  locked: boolean;
  error?: string;
}>();

const emit = defineEmits<{
  "update:operatorFirstName": [value: string];
  "update:client": [value: ClientInputDto];
}>();

function update<K extends keyof ClientInputDto>(key: K, value: ClientInputDto[K]): void {
  emit("update:client", { ...props.client, [key]: value });
}

type ValidatedField = "email" | "phone" | "command";
const touched = reactive<Record<ValidatedField, boolean>>({
  email: false,
  phone: false,
  command: false,
});
const emailError = computed(() => (touched.email ? validateEmail(props.client.email) : null));
const phoneError = computed(() => (touched.phone ? validatePhoneNumber(props.client.phone) : null));
const commandError = computed(() =>
  touched.command
    ? (validateAgencyCode(props.client.agencyCode) ??
      validateCommandNumber(
        normalizeCommandNumber(props.client.commandValue) ?? props.client.commandValue,
      ))
    : null,
);
</script>

<template>
  <section class="identity">
    <h2>Identification</h2>
    <label
      >Prénom de l’opérateur *
      <input
        :value="operatorFirstName"
        autocomplete="given-name"
        required
        @input="emit('update:operatorFirstName', ($event.target as HTMLInputElement).value)"
      />
    </label>
    <template v-if="departure">
      <label
        >Nom du client *
        <input
          :value="client.displayName"
          :disabled="locked"
          required
          autocomplete="name"
          @input="update('displayName', ($event.target as HTMLInputElement).value)"
        />
      </label>
      <label
        >Adresse électronique *
        <input
          type="email"
          :value="client.email"
          :disabled="locked"
          required
          autocomplete="email"
          :aria-invalid="Boolean(emailError)"
          :aria-describedby="emailError ? 'client-email-error' : undefined"
          @input="update('email', ($event.target as HTMLInputElement).value)"
          @blur="touched.email = true"
        />
      </label>
      <small
        v-if="emailError"
        id="client-email-error"
        class="identity__field-error"
        role="alert"
        >{{ emailError }}</small
      >
      <label
        >Téléphone *
        <input
          type="tel"
          :value="client.phone"
          :disabled="locked"
          required
          autocomplete="tel"
          :aria-invalid="Boolean(phoneError)"
          :aria-describedby="phoneError ? 'client-phone-error' : undefined"
          @input="update('phone', ($event.target as HTMLInputElement).value)"
          @blur="touched.phone = true"
        />
      </label>
      <small
        v-if="phoneError"
        id="client-phone-error"
        class="identity__field-error"
        role="alert"
        >{{ phoneError }}</small
      >
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
.identity label {
  display: grid;
  gap: 0.35rem;
}
.identity input {
  box-sizing: border-box;
  width: 100%;
  min-height: 2.75rem;
  padding: 0.6rem 0.75rem;
  color: var(--color-text);
  background: var(--color-input-background);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  font: inherit;
}
.identity input:focus-visible {
  outline: 3px solid var(--color-focus);
}
.identity__error {
  color: var(--color-danger);
}
.identity__field-error {
  margin-top: -0.6rem;
  color: var(--color-danger);
}
.identity input[aria-invalid="true"] {
  border-color: var(--color-danger);
}
</style>
