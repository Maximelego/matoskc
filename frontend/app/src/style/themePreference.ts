import { ref } from "vue";

export type ThemePreference = "light" | "dark";

const storageKey = "matoskc-theme";
export const themePreference = ref<ThemePreference>("light");

export function initializeTheme(): void {
  let saved: string | null = null;
  try {
    saved = window.localStorage.getItem(storageKey);
  } catch {
    // La navigation privée peut interdire l'accès au stockage.
  }
  const systemDark = window.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;
  themePreference.value = saved === "light" || saved === "dark" ? saved : systemDark ? "dark" : "light";
  document.documentElement.dataset.theme = themePreference.value;
}

export function setTheme(value: ThemePreference): void {
  themePreference.value = value;
  document.documentElement.dataset.theme = value;
  try {
    window.localStorage.setItem(storageKey, value);
  } catch {
    // Le choix reste actif pour la session courante.
  }
}
