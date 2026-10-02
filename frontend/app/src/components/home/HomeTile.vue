<script setup lang="ts">
import { RouterLink } from "vue-router";
import type { IconName } from "../common/icon/BaseIcon.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";

export type HomeTileProps = {
  title: string;
  description: string;
  icon: IconName;
  to?: string;
  featured?: boolean;
};

defineProps<HomeTileProps>();
</script>

<template>
  <RouterLink
    v-if="to"
    :to="to"
    class="home-tile"
    :class="{ 'home-tile--featured': featured }"
  >
    <BaseIcon
      class="home-tile__background-icon"
      :name="icon"
      :size="112"
    />

    <span class="home-tile__content">
      <strong class="home-tile__title">{{ title }}</strong>
      <span class="home-tile__description">{{ description }}</span>
    </span>

    <BaseIcon
      class="home-tile__arrow"
      name="arrow-right"
      :size="20"
    />
  </RouterLink>

  <div
    v-else
    class="home-tile home-tile--disabled"
    :class="{ 'home-tile--featured': featured }"
  >
    <BaseIcon
      class="home-tile__background-icon"
      :name="icon"
      :size="112"
    />

    <span class="home-tile__content">
      <strong class="home-tile__title">{{ title }}</strong>
      <span class="home-tile__description">{{ description }}</span>
      <span class="home-tile__soon">À venir</span>
    </span>
  </div>
</template>

<style scoped lang="scss">
.home-tile {
  position: relative;
  display: flex;
  align-items: flex-end;
  min-width: 0;
  min-height: 11rem;

  box-sizing: border-box;
  overflow: hidden;
  padding: 1.25rem;
  border: 1px solid var(--color-border);
  border-radius: 0.875rem;
  color: var(--color-text);
  background: var(--color-surface);
  text-decoration: none;
  isolation: isolate;
  transition:
    border-color 150ms ease,
    background-color 150ms ease,
    transform 150ms ease;

  &[href]:hover {
    border-color: var(--color-primary);
    background: var(--color-primary-soft);
    transform: translateY(-2px);
  }

  &[href]:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
    outline-offset: 0.1875rem;
  }

  &--featured {
    border-color: var(--color-primary);
    background: var(--color-primary-soft);
  }

  &--disabled {
    color: var(--color-text-secondary);
  }

  &__background-icon {
    position: absolute;
    z-index: -1;
    top: -0.75rem;
    right: -0.75rem;
    color: var(--color-primary);
    opacity: 0.12;
    pointer-events: none;
  }

  &--featured &__background-icon {
    opacity: 0.18;
  }

  &--disabled &__background-icon {
    color: var(--color-text-secondary);
    opacity: 0.1;
  }

  &__content {
    display: grid;
    gap: 0.45rem;
    min-width: 0;
    max-width: 100%;
  }

  &__title {
    color: var(--color-text);
    font-size: 1.1rem;
    line-height: 1.3;
  }

  &__description {
    line-height: 1.45;
  }

  &__soon {
    width: fit-content;
    margin-top: 0.25rem;
    padding: 0.2rem 0.55rem;
    border-radius: 999px;
    color: var(--color-text-secondary);
    background: var(--color-surface-secondary);
    font-size: var(--font-size-sm);
  }

  &__arrow {
    flex: none;
    align-self: flex-end;
    margin-left: auto;
    color: var(--color-primary);
  }
}

@media (max-width: 42rem) {
  .home-tile {
    min-height: 9.5rem;
    padding: 1rem;
  }

  .home-tile__background-icon {
    top: -1.25rem;
    right: -1.25rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .home-tile {
    transition: none;
  }
}
</style>
