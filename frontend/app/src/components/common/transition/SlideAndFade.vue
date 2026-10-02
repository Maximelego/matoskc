<template>
  <Transition
    name="slide-and-fade"
    mode="out-in"
  >
    <slot />
  </Transition>
</template>

<style lang="scss">
.slide-and-fade-enter-active,
.slide-and-fade-leave-active {
  transition:
    opacity 180ms ease,
    transform 180ms ease;
}

.slide-and-fade-enter-from {
  opacity: 0;
  transform: translateY(0.75rem);
}

.slide-and-fade-leave-to {
  opacity: 0;
  transform: translateY(-0.5rem);
}

@media (prefers-reduced-motion: reduce) {
  .slide-and-fade-enter-active,
  .slide-and-fade-leave-active {
    transition: none;
  }
}
</style>
