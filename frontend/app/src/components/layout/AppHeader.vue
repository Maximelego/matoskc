<script setup lang="ts">
import { ref } from "vue";

defineProps<{
  title: string;
}>();

const isMenuOpen = ref<boolean>(false);

function toggleMenu(): void {
  isMenuOpen.value = !isMenuOpen.value;
}

function closeMenu(): void {
  isMenuOpen.value = false;
}
</script>

<template>
  <header class="app-header">
    <div class="app-header__content">
      <span class="app-header__title">
        {{ title }}
      </span>

      <button
        class="app-header__menu-button"
        type="button"
        aria-controls="main-navigation"
        :aria-expanded="isMenuOpen"
        :aria-label="isMenuOpen ? 'Fermer le menu' : 'Ouvrir le menu'"
        @click="toggleMenu"
      >
        <span />
        <span />
        <span />
      </button>

      <nav
        id="main-navigation"
        class="app-header__navigation"
        :class="{ 'app-header__navigation--open': isMenuOpen }"
        aria-label="Navigation principale"
        @click="closeMenu"
      >
        <slot name="navigation" />
      </nav>

      <div v-if="$slots.actions" class="app-header__actions">
        <slot name="actions" />
      </div>
    </div>
  </header>
</template>

<style scoped lang="scss">
.app-header {
  color: var(--color-text);
  background-color: var(--color-surface);
  border-bottom: 1px solid var(--color-border);

  &__content {
    width: 100%;
    max-width: 90rem;
    min-height: 4rem;
    box-sizing: border-box;
    display: flex;
    align-items: center;
    gap: 1.5rem;
    margin-inline: auto;
    padding: 0.75rem 1.5rem;
  }

  &__title {
    flex-shrink: 0;
    color: var(--color-primary);
    font-size: var(--font-size-xl);
    font-weight: var(--font-weight-bold);
  }

  &__navigation {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  &__actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-left: auto;
  }

  &__menu-button {
    display: none;
    width: 2.75rem;
    height: 2.75rem;
    box-sizing: border-box;
    padding: 0.625rem;
    color: var(--color-text);
    background-color: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    cursor: pointer;

    span {
      display: block;
      width: 100%;
      height: 0.125rem;
      margin-block: 0.25rem;
      background-color: currentColor;
    }

    &:hover {
      background-color: var(--color-surface-hover);
    }

    &:focus-visible {
      outline: 0.1875rem solid var(--color-focus);
      outline-offset: 0.125rem;
    }
  }
}

@media (max-width: 48rem) {
  .app-header {
    &__content {
      flex-wrap: wrap;
      gap: 0.75rem;
      padding-inline: 1rem;
    }

    &__menu-button {
      display: block;
      margin-left: auto;
    }

    &__navigation {
      display: none;
      width: 100%;
      flex-direction: column;
      align-items: stretch;
      gap: 0.5rem;
      padding-top: 0.75rem;
      border-top: 1px solid var(--color-border);

      &--open {
        display: flex;
      }
    }

    &__actions {
      margin-left: 0;
    }
  }
}
</style>