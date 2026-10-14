<script setup lang="ts">
import BaseInput from "./BaseInput.vue";
import { normalizeCommandNumber } from "./commandNumber";

const props = defineProps<{ modelValue: string; disabled?: boolean; error?: string }>();
const emit = defineEmits<{ "update:modelValue": [value: string] }>();
function update(value: string): void {
  emit("update:modelValue", value.replace(/\D/g, "").slice(0, 6));
}
function blur(): void {
  if (props.modelValue) emit("update:modelValue", normalizeCommandNumber(props.modelValue) ?? props.modelValue);
}
</script>

<template>
  <BaseInput
    id="command-number" :model-value="modelValue" label="Numéro de commande" name="commandNumber"
    inputmode="numeric" autocomplete="off" placeholder="000123" :maxlength="6" required
    help-text="Six chiffres. Les zéros manquants sont ajoutés au début."
    :disabled="disabled" :error="error" @update:model-value="update" @blur="blur"
  />
</template>
