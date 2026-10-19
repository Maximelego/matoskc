<script setup lang="ts">
import { ref, watch } from "vue";
import { normalizeCommandNumber } from "./commandNumber";

const props = defineProps<{
  modelValue: string;
  agencyCode: number;
  disabled?: boolean;
  error?: string;
}>();
const emit = defineEmits<{
  "update:modelValue": [value: string];
  "update:agencyCode": [value: number];
}>();

const agencyDigits = ref("");
watch(
  () => props.agencyCode,
  (code) => {
    if (Number(agencyDigits.value) !== code || (!agencyDigits.value && code))
      agencyDigits.value = code ? String(code).padStart(2, "0") : "";
  },
  { immediate: true },
);

function updateAgency(event: Event): void {
  const input = event.target as HTMLInputElement;
  const digits = input.value.replace(/\D/g, "").slice(0, 2);
  agencyDigits.value = digits;
  input.value = digits;
  emit("update:agencyCode", digits ? Number(digits) : 0);
}

function updateCommand(event: Event): void {
  const input = event.target as HTMLInputElement;
  const digits = input.value.replace(/\D/g, "").slice(0, 6);
  input.value = digits;
  emit("update:modelValue", digits);
}

function blurCommand(): void {
  if (props.modelValue)
    emit("update:modelValue", normalizeCommandNumber(props.modelValue) ?? props.modelValue);
}
</script>

<template>
  <div class="command-number">
    <span
      id="command-number-label"
      class="command-number__label"
      >Numéro de commande *</span
    >
    <div
      class="command-number__control"
      role="group"
      aria-labelledby="command-number-label"
    >
      <label
        class="command-number__visually-hidden"
        for="command-agency-code"
        >Code agence</label
      >
      <input
        id="command-agency-code"
        class="command-number__agency"
        name="agencyCode"
        type="text"
        inputmode="numeric"
        autocomplete="off"
        placeholder="AA"
        :value="agencyDigits"
        maxlength="2"
        pattern="[0-9]{2}"
        required
        :disabled="disabled"
        :aria-invalid="Boolean(error)"
        aria-describedby="command-number-help"
        @input="updateAgency"
      />
      <span
        class="command-number__separator"
        aria-hidden="true"
        >/</span
      >
      <label
        class="command-number__visually-hidden"
        for="command-value"
        >Numéro de commande</label
      >
      <input
        id="command-value"
        class="command-number__value"
        name="commandNumber"
        type="text"
        inputmode="numeric"
        autocomplete="off"
        placeholder="XXXXXX"
        :value="modelValue"
        maxlength="6"
        pattern="[0-9]{6}"
        required
        :disabled="disabled"
        :aria-invalid="Boolean(error)"
        aria-describedby="command-number-help"
        @input="updateCommand"
        @blur="blurCommand"
      />
    </div>
    <p
      v-if="error"
      class="command-number__error"
      role="alert"
    >
      {{ error }}
    </p>
  </div>
</template>

<style scoped lang="scss">
.command-number {
  display: grid;
  gap: 0.35rem;
  min-width: 0;
}
.command-number__label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}
.command-number__control {
  display: flex;
  align-items: center;
  width: 100%;
  min-height: 2.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-input-background);
}
.command-number__control:focus-within {
  outline: 3px solid var(--color-focus);
}
.command-number__control input {
  width: 100%;
  min-width: 0;
  min-height: 2.75rem;
  border: 0;
  outline: 0;
  padding: 0.6rem 0.75rem;
  background: transparent;
  color: var(--color-text);
  font: inherit;
}
.command-number__control input:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}
.command-number__agency {
  flex: 0 0 5rem;
  text-align: center;
}
.command-number__value {
  flex: 1 1 auto;
  font-variant-numeric: tabular-nums;
}
.command-number__separator {
  flex: none;
  color: var(--color-text-secondary);
}
.command-number__error {
  margin: 0;
  color: var(--color-danger);
}
.command-number__visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
@media (max-width: 42rem) {
  .command-number__control,
  .command-number__control input {
    min-height: 3rem;
  }
}
</style>
