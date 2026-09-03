import { computed, reactive, ref } from "vue";

import type {
  FormErrors,
  FormFieldDefinition,
  FormValidator,
  FormValues,
} from "./form.types";
import { validateForm as collectFormErrors } from "./form.validators";

function createValues(
  fields: readonly FormFieldDefinition[],
  initialValues: Partial<FormValues>,
): FormValues {
  return Object.fromEntries(
    fields.map((field) => [
      field.name,
      initialValues[field.name] ?? field.defaultValue ?? "",
    ]),
  );
}

export function useBaseForm(
  fields: readonly FormFieldDefinition[],
  initialValues: Partial<FormValues> = {},
  formValidators: readonly FormValidator[] = [],
) {
  const values = reactive<FormValues>(createValues(fields, initialValues));
  const errors = ref<FormErrors>({});
  const touched = reactive<Record<string, boolean>>({});
  const dirty = reactive<Record<string, boolean>>({});

  for (const field of fields) {
    touched[field.name] = false;
    dirty[field.name] = false;
  }

  function collectErrors(): FormErrors {
    return collectFormErrors(fields, values, formValidators);
  }

  const isValid = computed<boolean>(
    () => Object.keys(collectErrors()).length === 0,
  );

  function setValue(fieldName: string, value: string): void {
    if (values[fieldName] !== value) {
      values[fieldName] = value;
      dirty[fieldName] = true;
    }

    if (touched[fieldName]) {
      errors.value = collectErrors();
    }
  }

  function setValues(
    nextValues: Partial<FormValues>,
    markAsDirty = false,
  ): void {
    for (const field of fields) {
      const nextValue = nextValues[field.name] ?? field.defaultValue ?? "";

      if (values[field.name] !== nextValue) {
        values[field.name] = nextValue;

        if (markAsDirty) {
          dirty[field.name] = true;
        }
      }
    }
  }

  function touchField(fieldName: string): void {
    touched[fieldName] = true;
    errors.value = collectErrors();
  }

  function markAllTouched(): void {
    for (const field of fields) {
      touched[field.name] = true;
    }
  }

  function validate(): boolean {
    errors.value = collectErrors();

    return Object.keys(errors.value).length === 0;
  }

  function getFieldErrors(fieldName: string): readonly string[] {
    return errors.value[fieldName] ?? [];
  }

  function reset(nextValues: Partial<FormValues> = initialValues): void {
    setValues(nextValues);
    errors.value = {};

    for (const field of fields) {
      touched[field.name] = false;
      dirty[field.name] = false;
    }
  }

  return {
    values,
    errors,
    touched,
    dirty,
    isValid,
    setValue,
    setValues,
    touchField,
    markAllTouched,
    validate,
    getFieldErrors,
    reset,
  };
}
