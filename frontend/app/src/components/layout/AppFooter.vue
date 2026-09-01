<template>
  <footer class="app-footer">
    <slot>
      <p class="app-footer__text">
        Application de suivi du matériel
      </p>
    </slot>
  </footer>
</template>

<style scoped lang="scss">
.app-footer {
  padding: 1rem;
  color: var(--color-text-secondary);
  background-color: var(--color-surface);
  border-top: 1px solid var(--color-border);

  &__text {
    margin: 0;
    font-size: var(--font-size-sm);
    text-align: center;
  }
}
</style>