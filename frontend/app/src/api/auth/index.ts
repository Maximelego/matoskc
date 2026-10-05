export type { LoginRequestDto } from "./dto";

export const AUTH_MODES = [
  { value: "agency", label: "Agence" },
  { value: "admin", label: "Administration" },
] as const;

export const AGENCIES = [{ id: "83", label: "Agence 83" }] as const;
