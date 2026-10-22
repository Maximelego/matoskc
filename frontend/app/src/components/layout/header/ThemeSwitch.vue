<script setup lang="ts">
import { setTheme, themePreference } from "../../../style/themePreference";

function toggleTheme(): void {
  setTheme(themePreference.value === "dark" ? "light" : "dark");
}
</script>

<template>
  <button
    type="button"
    class="theme-switch"
    role="switch"
    :aria-checked="themePreference === 'dark'"
    aria-label="Thème sombre"
    @click="toggleTheme"
  >
    <span
      class="theme-switch__icon"
      aria-hidden="true"
      >☀</span
    >
    <span
      class="theme-switch__track"
      aria-hidden="true"
      ><span class="theme-switch__thumb"
    /></span>
    <span
      class="theme-switch__icon"
      aria-hidden="true"
      >☾</span
    >
  </button>
</template>

<style scoped lang="scss">
.theme-switch {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  flex: none;
  min-height: 2.75rem;
  padding: 0.35rem 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface);
  color: var(--color-text);
  cursor: pointer;
  font: inherit;
  font-size: var(--font-size-sm);
}
.theme-switch:hover {
  background: var(--color-surface-hover);
}
.theme-switch:focus-visible {
  outline: 3px solid var(--color-focus);
  outline-offset: 2px;
}
.theme-switch__icon {
  font-size: 1.1rem;
  line-height: 1;
}
.theme-switch__track {
  display: flex;
  align-items: center;
  width: 2.3rem;
  height: 1.35rem;
  padding: 0.15rem;
  border-radius: 2rem;
  background: var(--color-border-strong);
  transition: background-color 180ms ease;
}
.theme-switch__thumb {
  width: 1.05rem;
  height: 1.05rem;
  border-radius: 50%;
  background: var(--color-surface);
  box-shadow: 0 1px 3px var(--color-shadow);
  transition: transform 180ms ease;
}
.theme-switch[aria-checked="true"] .theme-switch__track {
  background: var(--color-secondary);
}
.theme-switch[aria-checked="true"] .theme-switch__thumb {
  transform: translateX(0.95rem);
}
@media (max-width: 36rem) {
  .theme-switch__label {
    display: none;
  }
}
@media (prefers-reduced-motion: reduce) {
  .theme-switch__track,
  .theme-switch__thumb {
    transition: none;
  }
}
</style>
