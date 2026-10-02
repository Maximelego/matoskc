<script setup lang="ts">
import { onMounted, onUnmounted, ref, useId, watch } from "vue";

const props = defineProps<{ title: string; open: boolean; busy?: boolean }>();
const emit = defineEmits<{ close: []; closed: [] }>();
const dialog = ref<HTMLDialogElement | null>(null);
const closing = ref(false);
const titleId = useId();
let previousFocus: HTMLElement | null = null;
let closeTimer: ReturnType<typeof setTimeout> | undefined;

function show(): void {
  const element = dialog.value;
  if (!element) return;
  if (closeTimer) clearTimeout(closeTimer);
  closeTimer = undefined;
  if (element.open) {
    closing.value = false;
    return;
  }
  previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
  closing.value = false;
  element.showModal();
}

function finishClose(): void {
  if (props.open || !closing.value || !dialog.value?.open) return;
  if (closeTimer) clearTimeout(closeTimer);
  closeTimer = undefined;
  dialog.value.close();
  closing.value = false;
  previousFocus?.focus();
  previousFocus = null;
  emit("closed");
}

function hide(): void {
  if (!dialog.value?.open || closing.value) return;
  closing.value = true;
  // Permet aussi la fermeture lorsque les animations CSS sont désactivées.
  closeTimer = setTimeout(finishClose, 250);
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

function onAnimationEnd(event: AnimationEvent): void {
  // Le nom des keyframes peut être renommé par les styles scoped de Vue.
  if (event.target === dialog.value && closing.value) finishClose();
}

watch(() => props.open, (open) => { if (open) show(); else hide(); });
onMounted(() => { if (props.open) show(); });
onUnmounted(() => {
  if (closeTimer) clearTimeout(closeTimer);
  dialog.value?.close();
});
</script>

<template>
  <Teleport to="body">
    <dialog
      ref="dialog"
      class="base-modal"
      :class="{ 'base-modal--closing': closing }"
      :aria-labelledby="titleId"
      @cancel="onCancel"
      @click="onBackdropClick"
      @animationend="onAnimationEnd"
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
          >×</button>
        </header>
        <slot />
      </div>
    </dialog>
  </Teleport>
</template>

<style scoped lang="scss">
.base-modal {
  width: min(100% - 2rem, 36rem);
  max-height: min(90dvh, 50rem);
  padding: 0;
  overflow: auto;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  box-shadow: 0 1rem 3rem var(--color-shadow);

  &[open] { animation: modal-popup-in 220ms cubic-bezier(0.2, 0.8, 0.2, 1) both; }
  &[open]::backdrop {
    background: var(--color-overlay);
    animation: modal-backdrop-in 220ms ease both;
  }
  &--closing[open] { animation: modal-popup-out 170ms ease-in both; }
  &--closing[open]::backdrop { animation: modal-backdrop-out 170ms ease-in both; }
  &__content { padding: 1.5rem; }
  &__header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; }
  &__header h2 { min-width: 0; margin: 0 0 1.25rem; overflow-wrap: anywhere; }
  &__close {
    flex: none;
    display: grid;
    place-items: center;
    width: 2.75rem;
    height: 2.75rem;
    padding: 0;
    border: 0;
    border-radius: 0.5rem;
    color: inherit;
    background: transparent;
    font-size: 1.75rem;
    cursor: pointer;
    &:focus-visible { outline: 0.1875rem solid var(--color-focus); }
  }
}
@keyframes modal-popup-in {
  from { opacity: 0; transform: translateY(1rem) scale(0.94); }
  to { opacity: 1; transform: translateY(0) scale(1); }
}
@keyframes modal-popup-out {
  from { opacity: 1; transform: translateY(0) scale(1); }
  to { opacity: 0; transform: translateY(0.5rem) scale(0.97); }
}
@keyframes modal-backdrop-in { from { opacity: 0; } to { opacity: 1; } }
@keyframes modal-backdrop-out { from { opacity: 1; } to { opacity: 0; } }
@media (max-width: 42rem) {
  .base-modal {
    box-sizing: border-box;
    width: calc(100% - 1rem);
    max-width: none;
    max-height: calc(100dvh - 1rem);
    &__content { padding: 1rem; }
  }
}
@media (prefers-reduced-motion: reduce) {
  .base-modal[open], .base-modal[open]::backdrop,
  .base-modal--closing[open], .base-modal--closing[open]::backdrop {
    animation-duration: 1ms;
  }
}
</style>
