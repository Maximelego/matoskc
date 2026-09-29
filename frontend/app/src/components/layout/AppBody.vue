<template>
  <main class="app-body">
    <div class="app-body__content">
      <slot />
    </div>
  </main>
</template>

<style scoped lang="scss">
.app-body {
  flex: 1;
  width: 100%;
  box-sizing: border-box;

  &__content {
    width: 100%;
    max-width: 90rem;
    box-sizing: border-box;
    margin-inline: auto;
    padding: 2rem 1.5rem;
  }
}

@media (max-width: 48rem) {
  .app-body {
    &__content {
      padding: 1.25rem 1rem;
    }
  }
}
</style>