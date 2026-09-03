<template>
  <footer class="app-footer">
    <div class="app-footer__content">
      <slot>
        <span>MatosKC</span>
      </slot>
    </div>
  </footer>
</template>

<style scoped lang="scss">
.app-footer {
  color: var(--color-text-secondary);
  background-color: var(--color-surface);
  border-top: 1px solid var(--color-border);

  &__content {
    width: 100%;
    max-width: 90rem;
    box-sizing: border-box;
    display: flex;
    justify-content: center;
    gap: 1rem;
    margin-inline: auto;
    padding: 1rem 1.5rem;
    font-size: var(--font-size-sm);
  }
}

@media (max-width: 48rem) {
  .app-footer {
    &__content {
      flex-direction: column;
      align-items: center;
      padding: 0.75rem 1rem;
      text-align: center;
    }
  }
}
</style>