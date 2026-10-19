<script setup lang="ts">
import { RouterLink } from "vue-router";

defineProps<{
  title: string;
}>();

</script>

<template>
  <header class="app-header">
    <div class="app-header__content">
      <RouterLink
        to="/"
        class="app-header__title"
      >
        {{ title }}
      </RouterLink>

      <div
        v-if="$slots.actions"
        class="app-header__actions"
      >
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
    text-decoration: none;
    color: var(--color-primary);
    font-size: var(--font-size-xl);
    font-weight: var(--font-weight-bold);

    &:hover {
      cursor: pointer;
      scale: 1.1;
      transition: scale 0.2s ease-in-out;
    }
  }

  &__actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-left: auto;
  }

}

@media (max-width: 48rem) {
  .app-header {
    &__content {
      flex-wrap: wrap;
      gap: 0.75rem;
      padding-inline: 1rem;
    }

    &__actions {
      margin-left: auto;
    }
  }
}
</style>
