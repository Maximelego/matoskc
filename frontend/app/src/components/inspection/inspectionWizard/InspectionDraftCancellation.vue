<script setup lang="ts">
defineProps<{ reason: string; busy: boolean }>();
const emit = defineEmits<{ "update:reason": [value: string]; cancel: [] }>();
</script>

<template>
  <section class="draft-cancellation">
    <label for="cancel-reason">Annuler ce brouillon</label>
    <textarea
      id="cancel-reason"
      :value="reason"
      rows="2"
      maxlength="1000"
      placeholder="Motif de l’annulation"
      :disabled="busy"
      @input="emit('update:reason', ($event.target as HTMLTextAreaElement).value)"
    />
    <button
      type="button"
      :disabled="busy || !reason.trim()"
      @click="emit('cancel')"
    >
      Annuler l’état des lieux
    </button>
  </section>
</template>

<style scoped lang="scss">
.draft-cancellation {
  display: grid;
  gap: 0.5rem;
  border-top: 1px solid var(--color-border);
  padding-top: 1rem;
}
.draft-cancellation textarea {
  width: 100%;
  min-width: 0;
}
.draft-cancellation button {
  justify-self: start;
  min-height: 2.75rem;
  padding: 0.55rem 0.85rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-surface);
  cursor: pointer;
}
.draft-cancellation button:focus-visible {
  outline: 3px solid var(--color-focus);
}
.draft-cancellation button:disabled {
  opacity: 0.55;
  cursor: wait;
}
</style>
