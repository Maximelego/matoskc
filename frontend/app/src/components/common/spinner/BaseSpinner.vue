<script setup lang="ts">
type SpinnerSize = "small" | "medium" | "large";
type SpinnerVariant = "primary" | "secondary" | "inherit";

withDefaults(
  defineProps<{
    size?: SpinnerSize;
    variant?: SpinnerVariant;
    label?: string;
  }>(),
  {
    size: "medium",
    variant: "primary",
    label: "Chargement en cours",
  },
);
</script>

<template>
  <span
    class="base-spinner"
    :class="[
      `base-spinner--${size}`,
      `base-spinner--${variant}`,
    ]"
    role="status"
    :aria-label="label"
  />
</template>

<style scoped lang="scss">
.base-spinner {
  --spinner-size: 2rem;
  --spinner-thickness: 0.3125rem;
  --spinner-color: var(--color-primary);

  display: inline-block;
  width: var(--spinner-size);
  aspect-ratio: 1;
  flex-shrink: 0;
  border-radius: 50%;
  background:
    radial-gradient(
      farthest-side,
      var(--spinner-color) 94%,
      transparent
    )
    top / var(--spinner-thickness) var(--spinner-thickness)
    no-repeat,
    conic-gradient(
      transparent 30%,
      var(--spinner-color)
    );

  -webkit-mask: radial-gradient(
    farthest-side,
    transparent calc(100% - var(--spinner-thickness)),
    var(--color-text) 0
  );

  mask: radial-gradient(
    farthest-side,
    transparent calc(100% - var(--spinner-thickness)),
    var(--color-text) 0
  );

  animation: base-spinner-rotate 1s infinite linear;

  &--small {
    --spinner-size: 1rem;
    --spinner-thickness: 0.1875rem;
  }

  &--medium {
    --spinner-size: 2rem;
    --spinner-thickness: 0.3125rem;
  }

  &--large {
    --spinner-size: 3.125rem;
    --spinner-thickness: 0.5rem;
  }

  &--primary {
    --spinner-color: var(--color-primary);
  }

  &--secondary {
    --spinner-color: var(--color-secondary);
  }

  &--inherit {
    --spinner-color: currentColor;
  }
}

@keyframes base-spinner-rotate {
  to {
    transform: rotate(1turn);
  }
}

@media (prefers-reduced-motion: reduce) {
  .base-spinner {
    animation-duration: 2s;
  }
}
</style>