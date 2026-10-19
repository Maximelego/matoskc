<script setup lang="ts">
import type { ClientInputDto } from "../../api/inspections";
import CommandNumberInput from "../common/input/CommandNumberInput.vue";

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
          @input="update('email', ($event.target as HTMLInputElement).value)"
        />
      </label>
      <label
        >Téléphone *
        <input
          type="tel"
          :value="client.phone"
          :disabled="locked"
          required
          autocomplete="tel"
          @input="update('phone', ($event.target as HTMLInputElement).value)"
        />
      </label>
      <label
        >Code de l’agence *
        <input
          type="number"
          min="1"
          :value="client.agencyCode || ''"
          :disabled="locked"
          required
          @input="update('agencyCode', Number(($event.target as HTMLInputElement).value))"
        />
      </label>
      <CommandNumberInput
        :model-value="client.commandValue"
        :disabled="locked"
        @update:model-value="update('commandValue', $event)"
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
</style>
