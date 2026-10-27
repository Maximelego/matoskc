<script setup lang="ts">
import type { MaintenanceDto } from "../../api/maintenance/dto";

defineProps<{
  entries: MaintenanceDto[];
  total: number;
  offset: number;
  limit: number;
  loading: boolean;
}>();
const emit = defineEmits<{ page: [offset: number] }>();
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium", timeStyle: "short" });
</script>

<template>
  <section
    class="history"
    aria-labelledby="maintenance-history-title"
  >
    <header>
      <p class="history__eyebrow">Suivi</p>
      <h2 id="maintenance-history-title">Maintenances enregistrées</h2>
    </header>
    <p
      v-if="!loading && !entries.length"
      class="history__empty"
    >
      Aucune maintenance enregistrée pour cet équipement.
    </p>
    <ol class="history__list">
      <li
        v-for="entry in entries"
        :key="entry.id"
        class="history__entry"
      >
        <div class="history__heading">
          <strong>{{ date.format(new Date(entry.performedAt)) }}</strong
          ><span>{{ entry.resolvedDefectIds.length }} défaut(s) résolu(s)</span>
        </div>
        <small>Intervenant : {{ entry.performedBy }}</small>
        <p>{{ entry.workDescription }}</p>
      </li>
    </ol>
    <nav
      v-if="total > limit || offset"
      class="history__pagination"
      aria-label="Pages des maintenances"
    >
      <button
        type="button"
        :disabled="loading || !offset"
        @click="emit('page', offset - limit)"
      >
        Précédent
      </button>
      <span>{{ offset + 1 }}–{{ Math.min(offset + limit, total) }} / {{ total }}</span>
      <button
        type="button"
        :disabled="loading || offset + limit >= total"
        @click="emit('page', offset + limit)"
      >
        Suivant
      </button>
    </nav>
  </section>
</template>

<style scoped lang="scss">
.history {
  display: grid;
  gap: 1rem;
  min-width: 0;
}
.history h2,
.history p {
  margin: 0;
}
.history__eyebrow {
  margin-bottom: 0.25rem !important;
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
  font-weight: 700;
}
.history__empty {
  padding: 1.3rem;
  border: 1px dashed var(--color-border);
  border-radius: 0.75rem;
  color: var(--color-text-secondary);
}
.history__list {
  display: grid;
  gap: 0.75rem;
  margin: 0;
  padding: 0;
  list-style: none;
}
.history__entry {
  display: grid;
  gap: 0.55rem;
  min-width: 0;
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-left: 0.25rem solid var(--color-secondary);
  border-radius: 0.75rem;
  background: var(--color-surface);
  overflow-wrap: anywhere;
}
.history__heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.6rem;
}
.history__heading span {
  padding: 0.25rem 0.6rem;
  border-radius: 2rem;
  background: var(--color-surface-secondary);
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
}
.history__entry small {
  color: var(--color-text-secondary);
}
.history__entry p {
  white-space: pre-wrap;
}
.history__pagination {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.7rem;
}
.history__pagination button {
  min-height: 2.75rem;
  padding: 0.5rem 0.8rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface);
  color: var(--color-text);
  cursor: pointer;
}
.history__pagination button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
