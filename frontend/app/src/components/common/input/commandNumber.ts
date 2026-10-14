export function normalizeCommandNumber(value: string): string | null {
  return /^\d{1,6}$/.test(value) ? value.padStart(6, "0") : null;
}
