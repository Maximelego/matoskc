<script setup lang="ts">
import { ASSETS } from '../../assets/manifest';

defineProps<{
  title: string;
}>();


</script>

<template>
  <header class="app-header">
    <image class="app-header__logo" :src="ASSETS.meta.src" alt="logo"/>
    <h1 class="app-header__title">
      {{ title }}
    </h1>

    <slot />
  </header>
</template>

<style scoped lang="scss">
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem;
  color: var(--color-text);
  background-color: var(--color-surface);
  border-bottom: 1px solid var(--color-border);

  &__title {
    margin: 0;
    font-size: var(--font-size-xl);
    font-weight: var(--font-weight-bold);
  }

  &__logo {
    width: auto;
  }
}
</style>