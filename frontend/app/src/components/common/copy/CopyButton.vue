<script setup lang="ts">
import { onUnmounted, ref } from "vue";
import BaseIcon from "../icon/BaseIcon.vue";

export type VariantType = "full" | "icon-only";

const props = defineProps<{
  value: string | null | undefined;
  label: string;
  variant: VariantType;
}>();
const feedback = ref("");

let timer: ReturnType<typeof setTimeout> | undefined;

async function copy(): Promise<void> {
  clearTimeout(timer);
  try {
    if (!props.value) return;
    await navigator.clipboard.writeText(props.value);
    feedback.value = "Copié";
  } catch {
    feedback.value = "Copie impossible";
  }
  timer = setTimeout(() => {
    feedback.value = "";
  }, 2500);
}

onUnmounted(() => clearTimeout(timer));
</script>

<template>
  <span class="copy-control">
    <button
      type="button"
      :disabled="!value"
      :aria-label="`Copier ${label}`"
      @click="copy"
    >
      <BaseIcon
        name="clipboard"
        :size="16"
      />
      <p v-if="variant === 'full'">Copier</p>
    </button>
    <span
      :class="{
        success: feedback === 'Copié',
        error: feedback === 'Copie impossible',
      }"
      role="status"
      >{{ feedback }}</span
    >
  </span>
</template>

<style scoped lang="scss">
.copy-control {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}
button {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  min-height: 2.25rem;
  padding: 0.35rem 0.55rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-surface);
  font: inherit;
  cursor: pointer;
}
button:hover:not(:disabled) {
  background: var(--color-surface-hover);
}
button:focus-visible {
  outline: 3px solid var(--color-focus);
  outline-offset: 2px;
}
button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

span[role="status"] {
  &.success {
    color: var(--color-success);
  }
  &.error {
    color: var(--color-danger);
  }
  font-size: var(--font-size-sm);
}
</style>
