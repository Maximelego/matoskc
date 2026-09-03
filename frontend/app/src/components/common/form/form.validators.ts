import type {
  FieldValidationRule,
  FormErrors,
  FormFieldDefinition,
  FormValidator,
  FormValues,
} from "./form.types";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function isEmpty(value: string): boolean {
  return value.trim().length === 0;
}

function matchesPattern(value: string, pattern: RegExp): boolean {
  pattern.lastIndex = 0;
  const matches = pattern.test(value);
  pattern.lastIndex = 0;

  return matches;
}

function validateRule(
  rule: FieldValidationRule,
  value: string,
  values: Readonly<FormValues>,
): string | undefined {
  switch (rule.type) {
    case "required":
      return undefined;

    case "minLength":
      return value.length >= rule.value
        ? undefined
        : rule.message ?? `Ce champ doit contenir au moins ${rule.value} caractères.`;

    case "maxLength":
      return value.length <= rule.value
        ? undefined
        : rule.message ?? `Ce champ ne peut pas dépasser ${rule.value} caractères.`;

    case "email":
      return EMAIL_PATTERN.test(value)
        ? undefined
        : rule.message ?? "L’adresse e-mail n’est pas valide.";

    case "pattern":
      return matchesPattern(value, rule.value) ? undefined : rule.message;

    case "sameAs":
      return value === values[rule.field]
        ? undefined
        : rule.message ?? "Les deux valeurs doivent être identiques.";

    case "custom":
      return rule.validate(value, values);
  }
}

export function validateField(
  field: FormFieldDefinition,
  values: Readonly<FormValues>,
): string[] {
  const value = values[field.name] ?? "";
  const rules = field.rules ?? [];
  const requiredRule = rules.find(
    (rule): rule is Extract<
      FieldValidationRule,
      { type: "required" }
    > => rule.type === "required",
  );

  if (isEmpty(value)) {
    if (!requiredRule) {
      return [];
    }

    return [requiredRule.message ?? "Ce champ est obligatoire."];
  }

  return rules
    .map((rule) => validateRule(rule, value, values))
    .filter((error): error is string => error !== undefined);
}

function mergeErrors(target: FormErrors, source: FormErrors): void {
  for (const [fieldName, fieldErrors] of Object.entries(source)) {
    if (!fieldErrors || fieldErrors.length === 0) {
      continue;
    }

    target[fieldName] = [
      ...(target[fieldName] ?? []),
      ...fieldErrors,
    ];
  }
}

export function validateForm(
  fields: readonly FormFieldDefinition[],
  values: Readonly<FormValues>,
  formValidators: readonly FormValidator[] = [],
): FormErrors {
  const errors: FormErrors = {};

  for (const field of fields) {
    const fieldErrors = validateField(field, values);

    if (fieldErrors.length > 0) {
      errors[field.name] = fieldErrors;
    }
  }

  for (const validator of formValidators) {
    mergeErrors(errors, validator(values));
  }

  return errors;
}
