export type FormValues = Record<string, string>;

export type FormErrors = Partial<Record<string, string[]>>;

export type FormInputType =
  | "text"
  | "email"
  | "tel"
  | "password"
  | "search"
  | "url";

export type FormInputMode =
  | "none"
  | "text"
  | "decimal"
  | "numeric"
  | "tel"
  | "search"
  | "email"
  | "url";

export type FieldValidator = (
  value: string,
  values: Readonly<FormValues>,
) => string | undefined;

export type FieldValidationRule =
  | {
      type: "required";
      message?: string;
    }
  | {
      type: "minLength";
      value: number;
      message?: string;
    }
  | {
      type: "maxLength";
      value: number;
      message?: string;
    }
  | {
      type: "email";
      message?: string;
    }
  | {
      type: "pattern";
      value: RegExp;
      message: string;
    }
  | {
      type: "sameAs";
      field: string;
      message?: string;
    }
  | {
      type: "custom";
      validate: FieldValidator;
    };

export interface FormFieldDefinition {
  name: string;
  label: string;
  type?: FormInputType;
  defaultValue?: string;
  placeholder?: string;
  autocomplete?: string;
  inputmode?: FormInputMode;
  helpText?: string;
  disabled?: boolean;
  readonly?: boolean;
  rules?: readonly FieldValidationRule[];
}

export type FormValidator = (
  values: Readonly<FormValues>,
) => FormErrors;
