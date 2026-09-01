<template>
  <main class="app-body">
    <slot />
  </main>
</template>

<style scoped lang="scss">
.app-body {
  flex: 1;
  width: 100%;
  box-sizing: border-box;
  padding: 1rem;
  color: var(--color-text);
  background-color: var(--color-background);
}
</style>