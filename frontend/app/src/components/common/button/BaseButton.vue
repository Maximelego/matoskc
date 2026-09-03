<script setup lang="ts">

import BaseSpinner from "../spinner/BaseSpinner.vue";

type ButtonVariant =
  | "primary"
  | "secondary"
  | "outline"
  | "ghost"
  | "danger";

type ButtonSize = "small" | "medium" | "large";

const props = withDefaults(
  defineProps<{
    variant?: ButtonVariant;
    size?: ButtonSize;
    disabled?: boolean;
    loading?: boolean;
    fullWidth?: boolean;
  }>(),
  {
    variant: "primary",
    size: "medium",
    disabled: false,
    loading: false,
    fullWidth: false,
  },
);

const emit = defineEmits<{
  click: [event: MouseEvent];
}>();


function handleClick(event: MouseEvent): void {
  if (props.disabled || props.loading) {
    return;
  }

  emit("click", event);
}
</script>

<template>
  <button
    type="button"
    class="base-button"
    :class="[
      `base-button--${variant}`,
      `base-button--${size}`,
      {
        'base-button--full-width': fullWidth,
        'base-button--loading': loading,
      },
    ]"
    :disabled="disabled || loading"
    :aria-busy="loading"
    @click="handleClick"
  >
    <BaseSpinner
        v-if="loading"
        size="small"
        label="Action en cours"
    />

    <span
      v-else-if="$slots.leading"
      class="base-button__icon"
      aria-hidden="true"
    >
      <slot name="leading" />
    </span>

    <span class="base-button__label">
      <slot />
    </span>

    <span
      v-if="!loading && $slots.trailing"
      class="base-button__icon"
      aria-hidden="true"
    >
      <slot name="trailing" />
    </span>
  </button>
</template>

<style scoped lang="scss">
@use "app/src/style/theme.scss";

.base-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.625rem;
  box-sizing: border-box;
  font-weight: var(--font-weight-semibold);
  line-height: var(--line-height-compact);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  cursor: pointer;
  transition:
    color 150ms ease,
    background-color 150ms ease,
    border-color 150ms ease,
    opacity 150ms ease;

  &:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
    outline-offset: 0.125rem;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }

  &--primary {
    color: var(--color-text-on-primary);
    background-color: var(--color-primary);
    border-color: var(--color-primary);

    &:not(:disabled):hover {
      background-color: var(--color-primary-hover);
      border-color: var(--color-primary-hover);
    }

    &:not(:disabled):active {
      background-color: var(--color-primary-active);
      border-color: var(--color-primary-active);
    }
  }

  &--secondary {
    color: var(--color-text-on-secondary);
    background-color: var(--color-secondary);
    border-color: var(--color-secondary);

    &:not(:disabled):hover {
      background-color: var(--color-secondary-hover);
      border-color: var(--color-secondary-hover);
    }

    &:not(:disabled):active {
      background-color: var(--color-secondary-active);
      border-color: var(--color-secondary-active);
    }
  }

  &--outline {
    color: var(--color-primary);
    background-color: var(--color-surface);
    border-color: var(--color-primary);

    &:not(:disabled):hover {
      background-color: var(--color-primary-soft);
    }

    &:not(:disabled):active {
      background-color: var(--color-primary-soft-hover);
    }
  }

  &--ghost {
    color: var(--color-text);
    background-color: var(--color-surface);
    border-color: var(--color-surface);

    &:not(:disabled):hover {
      background-color: var(--color-surface-hover);
      border-color: var(--color-surface-hover);
    }

    &:not(:disabled):active {
      background-color: var(--color-surface-secondary);
      border-color: var(--color-surface-secondary);
    }
  }

  &--danger {
    color: var(--color-danger);
    background-color: var(--color-danger-soft);
    border-color: var(--color-danger);

    &:not(:disabled):hover,
    &:not(:disabled):active {
      color: var(--color-text-on-primary);
      background-color: var(--color-danger);
    }
  }

  &--small {
    min-height: 2.25rem;
    padding: 0.5rem 0.75rem;
    font-size: var(--font-size-sm);
  }

  &--medium {
    min-height: 2.75rem;
    padding: 0.625rem 1rem;
    font-size: var(--font-size-md);
  }

  &--large {
    min-height: 3.25rem;
    padding: 0.75rem 1.25rem;
    font-size: var(--font-size-lg);
  }

  &--full-width {
    width: 100%;
  }

  &__label {
    min-width: 0;
  }

  &__icon {
    width: 1.25rem;
    height: 1.25rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }
}

@media (max-width: 48rem) {
  .base-button {
    &--medium {
      min-height: 3rem;
    }

    &--large {
      min-height: 3.5rem;
    }
  }
}

@media (prefers-reduced-motion: reduce) {
  .base-button {
    transition: none;
  }
}
</style>
