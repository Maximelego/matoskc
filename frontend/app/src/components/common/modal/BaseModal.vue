<script setup lang="ts">
import { nextTick, onMounted, onUnmounted, ref, useId, watch } from "vue";

const props = defineProps<{
  title: string;
  open: boolean;
  busy?: boolean;
}>();

const emit = defineEmits<{
  close: [];
}>();

const dialog = ref<HTMLDialogElement | null>(null);
const titleId = useId();
let previousFocus: HTMLElement | null = null;

function show(): void {
  if (!dialog.value || dialog.value.open) return;

  previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;

  dialog.value.showModal();

  void nextTick(() => {
    dialog.value?.querySelector<HTMLElement>("input, select, button")?.focus();
  });
}

function hide(): void {
  if (!dialog.value?.open) return;

  dialog.value.close();
  previousFocus?.focus();
}

function requestClose(): void {
  if (!props.busy) emit("close");
}

function onCancel(event: Event): void {
  event.preventDefault();
  requestClose();
}

function onBackdropClick(event: MouseEvent): void {
  if (event.target === dialog.value) requestClose();
}

watch(
  () => props.open,
  (open) => {
    if (open) show();
    else hide();
  },
);

onMounted(() => {
  if (props.open) show();
});

onUnmounted(hide);
</script>

<template>
  <Teleport to="body">
    <dialog
      ref="dialog"
      class="base-modal"
      :aria-labelledby="titleId"
      @cancel="onCancel"
      @click="onBackdropClick"
    >
      <div class="base-modal__content">
        <header class="base-modal__header">
          <h2 :id="titleId">{{ title }}</h2>

          <button
            type="button"
            class="base-modal__close"
            aria-label="Fermer la fenêtre"
            :disabled="busy"
            @click="requestClose"
          >
            ×
          </button>
        </header>

        <slot />
      </div>
    </dialog>
  </Teleport>
</template>

<style scoped lang="scss">
.base-modal {
  width: min(100% - 2rem, 36rem);
  max-height: min(90vh, 50rem);
  padding: 0;
  overflow: auto;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;

  &::backdrop {
    background: rgb(0 0 0 / 0.55);
  }

  &__content {
    padding: 1.5rem;
  }

  &__header {
    display: flex;
    align-items: start;
    justify-content: space-between;
    gap: 1rem;
  }

  &__header h2 {
    margin: 0 0 1.25rem;
  }

  &__close {
    border: 0;
    color: inherit;
    background: transparent;
    font-size: 1.75rem;
    cursor: pointer;
  }
}
</style>
