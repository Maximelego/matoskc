<script setup lang="ts">
type SpinnerSize = "small" | "medium" | "large";

withDefaults(
  defineProps<{
    size?: SpinnerSize;
    label?: string;
  }>(),
  {
    size: "medium",
    label: "Chargement en cours",
  },
);
</script>

<template>
  <span
    class="base-spinner"
    :class="`base-spinner--${size}`"
    role="status"
    :aria-label="label"
  />
</template>

<style scoped lang="scss">
.base-spinner {
  display: inline-block;
  box-sizing: border-box;
  flex-shrink: 0;
  color: inherit;
  border-style: solid;
  border-color: var(--color-border);
  border-top-color: currentColor;
  border-radius: 50%;
  animation: base-spinner-rotation 700ms linear infinite;

  &--small {
    width: 1rem;
    height: 1rem;
    border-width: 0.125rem;
  }

  &--medium {
    width: 1.5rem;
    height: 1.5rem;
    border-width: 0.1875rem;
  }

  &--large {
    width: 2rem;
    height: 2rem;
    border-width: 0.25rem;
  }
}

@keyframes base-spinner-rotation {
  to {
    transform: rotate(360deg);
  }
}

@media (prefers-reduced-motion: reduce) {
  .base-spinner {
    animation-duration: 1400ms;
  }
}
</style>