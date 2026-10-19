const LOCAL_PART = /^[A-Z0-9!#$%&'*+/=?^_`{|}~.-]+$/i;
const DOMAIN_LABEL = /^[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?$/i;

export function isValidEmail(value: string): boolean {
  const email = value.trim();
  if (!email || email.length > 254 || /\s/.test(email)) return false;

  const parts = email.split("@");
  if (parts.length !== 2) return false;

  const [local, domain] = parts;
  if (!local || local.length > 64 || !LOCAL_PART.test(local)) return false;
  if (local.startsWith(".") || local.endsWith(".") || local.includes("..")) {
    return false;
  }

  const labels = domain.split(".");
  return (
    labels.length >= 2 &&
    labels.every((label) => DOMAIN_LABEL.test(label)) &&
    labels.at(-1)!.length >= 2
  );
}

export function validateEmail(value: string, required = true): string | undefined {
  if (!value.trim()) {
    return required ? "L’adresse électronique est obligatoire." : undefined;
  }

  return isValidEmail(value) ? undefined : "L’adresse électronique n’est pas valide.";
}

export function isValidPhoneNumber(value: string): boolean {
  const phone = value.trim().replace(/[\s().-]/g, "");
  return /^(?:0[1-9]\d{8}|\+33[1-9]\d{8})$/.test(phone);
}

export function validatePhoneNumber(value: string, required = true): string | undefined {
  if (!value.trim()) {
    return required ? "Le numéro de téléphone est obligatoire." : undefined;
  }

  return isValidPhoneNumber(value)
    ? undefined
    : "Saisissez un numéro de téléphone français valide.";
}

export function isValidAgencyCode(value: number): boolean {
  return Number.isInteger(value) && value >= 1 && value <= 99;
}

export function validateAgencyCode(value: number): string | undefined {
  return isValidAgencyCode(value) ? undefined : "Le code agence doit comporter deux chiffres.";
}

export function isValidCommandNumber(value: string): boolean {
  return /^\d{6}$/.test(value);
}

export function validateCommandNumber(value: string): string | undefined {
  if (!value.trim()) return "Le numéro de commande est obligatoire.";

  return isValidCommandNumber(value)
    ? undefined
    : "Le numéro de commande doit comporter six chiffres.";
}
