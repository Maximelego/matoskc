<script setup lang="ts">
import { nextTick, watch } from "vue";

import BaseButton from "../button/BaseButton.vue";
import BaseInput from "../input/BaseInput.vue";

import type {
  FormErrors,
  FormFieldDefinition,
  FormValidator,
  FormValues,
} from "./form.types";
import { useBaseForm } from "./useBaseForm";

const props = withDefaults(
  defineProps<{
    fields: readonly FormFieldDefinition[];
    modelValue?: FormValues;
    validators?: readonly FormValidator[];
    submitLabel?: string;
    loading?: boolean;
    disabled?: boolean;
  }>(),
  {
    modelValue: undefined,
    validators: () => [],
    submitLabel: "Valider",
    loading: false,
    disabled: false,
  },
);

const emit = defineEmits<{
  "update:modelValue": [values: FormValues];
  "submit-attempt": [values: FormValues];
  submit: [values: FormValues];
  "invalid-submit": [errors: FormErrors];
  "validity-change": [isValid: boolean];
}>();

const {
  values,
  errors,
  touched,
  isValid,
  setValue,
  setValues,
  touchField,
  markAllTouched,
  validate,
  getFieldErrors,
} = useBaseForm(
  props.fields,
  props.modelValue ?? {},
  props.validators,
);

watch(
  () => props.modelValue,
  (nextValues) => {
    if (nextValues) {
      setValues(nextValues);
    }
  },
  { deep: true },
);

watch(isValid, (valid) => {
  emit("validity-change", valid);
}, { immediate: true });

function cloneValues(): FormValues {
  return { ...values };
}

function isRequired(field: FormFieldDefinition): boolean {
  return field.rules?.some((rule) => rule.type === "required") ?? false;
}

function getLengthRule(
  field: FormFieldDefinition,
  ruleType: "minLength" | "maxLength",
): number | undefined {
  const rule = field.rules?.find((candidate) => candidate.type === ruleType);

  return rule && "value" in rule ? rule.value as number : undefined;
}

function getVisibleError(fieldName: string): string | undefined {
  if (!touched[fieldName]) {
    return undefined;
  }

  return getFieldErrors(fieldName)[0];
}

function handleValueUpdate(fieldName: string, value: string): void {
  setValue(fieldName, value);
  emit("update:modelValue", cloneValues());
}

function handleFieldBlur(fieldName: string): void {
  touchField(fieldName);
}

async function focusFirstInvalidField(): Promise<void> {
  await nextTick();

  const firstInvalidField = props.fields.find(
    (field) => (errors.value[field.name]?.length ?? 0) > 0,
  );

  if (firstInvalidField) {
    document.getElementById(firstInvalidField.name)?.focus();
  }
}

async function handleSubmit(): Promise<void> {
  if (props.disabled || props.loading) {
    return;
  }

  const submittedValues = cloneValues();
  emit("submit-attempt", submittedValues);
  markAllTouched();

  if (validate()) {
    emit("submit", submittedValues);
    return;
  }

  emit("invalid-submit", errors.value);
  await focusFirstInvalidField();
}

function handleFieldKeydown(event: KeyboardEvent): void {
  if (event.key !== "Enter" || event.isComposing) {
    return;
  }

  event.preventDefault();
  void handleSubmit();
}
</script>

<template>
  <form class="base-form" novalidate @submit.prevent="handleSubmit">
    <div class="base-form__fields">
      <BaseInput
        v-for="field in fields"
        :id="field.name"
        :key="field.name"
        :model-value="values[field.name]"
        :label="field.label"
        :name="field.name"
        :type="field.type"
        :placeholder="field.placeholder"
        :autocomplete="field.autocomplete"
        :inputmode="field.inputmode"
        :help-text="field.helpText"
        :required="isRequired(field)"
        :disabled="disabled || loading || field.disabled"
        :readonly="field.readonly"
        :minlength="getLengthRule(field, 'minLength')"
        :maxlength="getLengthRule(field, 'maxLength')"
        :error="getVisibleError(field.name)"
        @update:model-value="handleValueUpdate(field.name, $event)"
        @blur="handleFieldBlur(field.name)"
        @keydown="handleFieldKeydown"
      />
    </div>

    <div class="base-form__actions">
      <BaseButton
        variant="primary"
        :loading="loading"
        :disabled="disabled"
        full-width
        @click="handleSubmit"
      >
        {{ submitLabel }}
      </BaseButton>
    </div>
  </form>
</template>

<style scoped lang="scss">
.base-form {
  width: 100%;
  max-width: 42rem;
  margin-inline: auto;

  &__fields {
    display: grid;
    gap: 1.25rem;
  }

  &__actions {
    margin-top: 1.5rem;
  }
}

@media (max-width: 48rem) {
  .base-form {
    &__fields {
      gap: 1rem;
    }

    &__actions {
      margin-top: 1.25rem;
    }
  }
}
</style>
