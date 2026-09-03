<script setup lang="ts">
import { computed } from "vue";

defineOptions({
  inheritAttrs: false,
});

type InputType =
  | "text"
  | "email"
  | "tel"
  | "password"
  | "search"
  | "url";

type InputMode =
  | "none"
  | "text"
  | "decimal"
  | "numeric"
  | "tel"
  | "search"
  | "email"
  | "url";

const props = withDefaults(
  defineProps<{
    modelValue: string;
    id: string;
    label: string;
    type?: InputType;
    name?: string;
    placeholder?: string;
    autocomplete?: string;
    inputmode?: InputMode;
    helpText?: string;
    error?: string;
    required?: boolean;
    disabled?: boolean;
    readonly?: boolean;
    minlength?: number;
    maxlength?: number;
  }>(),
  {
    type: "text",
    name: undefined,
    placeholder: undefined,
    autocomplete: undefined,
    inputmode: undefined,
    helpText: undefined,
    error: undefined,
    required: false,
    disabled: false,
    readonly: false,
    minlength: undefined,
    maxlength: undefined,
  },
);

const emit = defineEmits<{
  "update:modelValue": [value: string];
  focus: [event: FocusEvent];
  blur: [event: FocusEvent];
}>();

const helpTextId = computed<string>(() => `${props.id}-help`);
const errorId = computed<string>(() => `${props.id}-error`);

const describedBy = computed<string | undefined>(() => {
  if (props.error) {
    return errorId.value;
  }

  if (props.helpText) {
    return helpTextId.value;
  }

  return undefined;
});

function handleInput(event: Event): void {
  const input = event.currentTarget as HTMLInputElement;

  emit("update:modelValue", input.value);
}

function handleFocus(event: FocusEvent): void {
  emit("focus", event);
}

function handleBlur(event: FocusEvent): void {
  emit("blur", event);
}
</script>

<template>
  <div
    class="base-input"
    :class="{
      'base-input--error': error,
      'base-input--disabled': disabled,
      'base-input--readonly': readonly,
    }"
  >
    <label class="base-input__label" :for="id">
      {{ label }}

      <span
        v-if="required"
        class="base-input__required"
        aria-hidden="true"
      >
        *
      </span>
    </label>

    <div class="base-input__control">
      <span
        v-if="$slots.leading"
        class="base-input__leading"
      >
        <slot name="leading" />
      </span>

      <input
        v-bind="$attrs"
        :id="id"
        class="base-input__field"
        :value="modelValue"
        :type="type"
        :name="name"
        :placeholder="placeholder"
        :autocomplete="autocomplete"
        :inputmode="inputmode"
        :required="required"
        :disabled="disabled"
        :readonly="readonly"
        :minlength="minlength"
        :maxlength="maxlength"
        :aria-invalid="Boolean(error)"
        :aria-describedby="describedBy"
        @input="handleInput"
        @focus="handleFocus"
        @blur="handleBlur"
      />

      <span
        v-if="$slots.trailing"
        class="base-input__trailing"
      >
        <slot name="trailing" />
      </span>
    </div>

    <p
      v-if="error"
      :id="errorId"
      class="base-input__message base-input__message--error"
      role="alert"
    >
      {{ error }}
    </p>

    <p
      v-else-if="helpText"
      :id="helpTextId"
      class="base-input__message"
    >
      {{ helpText }}
    </p>
  </div>
</template>

<style scoped lang="scss">
.base-input {
  width: 100%;

  &__label {
    display: inline-flex;
    gap: 0.25rem;
    margin-bottom: 0.5rem;
    color: var(--color-text);
    font-size: var(--font-size-sm);
    font-weight: var(--font-weight-semibold);
  }

  &__required {
    color: var(--color-danger);
  }

  &__control {
    width: 100%;
    min-height: 2.75rem;
    box-sizing: border-box;
    display: flex;
    align-items: center;
    overflow: hidden;
    color: var(--color-text);
    background-color: var(--color-input-background);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    transition:
      border-color 150ms ease,
      box-shadow 150ms ease,
      background-color 150ms ease;

    &:focus-within {
      border-color: var(--color-focus);
      box-shadow: 0 0 0 0.1875rem var(--color-primary-soft);
    }
  }

  &__field {
    width: 100%;
    min-width: 0;
    min-height: inherit;
    box-sizing: border-box;
    padding: 0.625rem 0.75rem;
    color: var(--color-text);
    background-color: var(--color-input-background);
    border: none;
    outline: none;

    &::placeholder {
      color: var(--color-input-placeholder);
    }

    &:disabled {
      cursor: not-allowed;
    }
  }

  &__leading,
  &__trailing {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    color: var(--color-text-secondary);
  }

  &__leading {
    padding-left: 0.75rem;
  }

  &__trailing {
    padding-right: 0.75rem;
  }

  &__message {
    margin: 0.375rem 0 0;
    color: var(--color-text-secondary);
    font-size: var(--font-size-sm);

    &--error {
      color: var(--color-danger);
    }
  }

  &--error {
    .base-input__control {
      border-color: var(--color-danger);

      &:focus-within {
        border-color: var(--color-danger);
        box-shadow: 0 0 0 0.1875rem var(--color-danger-soft);
      }
    }
  }

  &--disabled {
    opacity: 0.6;
  }

  &--readonly {
    .base-input__control,
    .base-input__field {
      background-color: var(--color-surface-secondary);
    }
  }
}

@media (max-width: 48rem) {
  .base-input {
    &__control {
      min-height: 3rem;
    }

    &__field {
      padding: 0.75rem;
    }
  }
}

@media (prefers-reduced-motion: reduce) {
  .base-input__control {
    transition: none;
  }
}
</style>