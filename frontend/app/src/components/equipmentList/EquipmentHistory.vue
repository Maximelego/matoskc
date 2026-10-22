<script setup lang="ts">
import { ref, watch } from "vue";
import { equipmentHistoryApi } from "../../api/equipment-history";
import type {
  EquipmentAuditChangeDto,
  EquipmentAuditEntryDto,
} from "../../api/equipment-history/dto";
const props = defineProps<{ equipmentId: string }>();
const entries = ref<EquipmentAuditEntryDto[]>([]);
const loading = ref(false);
const error = ref("");
const offset = ref(0);
const limit = 50;
let sequence = 0;
const labels: Record<string, string> = {
  Name: "Nom",
  CategoryId: "Catégorie",
  Status: "État",
  SerialNumber: "Numéro de série",
};
function changes(entry: EquipmentAuditEntryDto): EquipmentAuditChangeDto[] {
  try {
    const value: unknown = JSON.parse(entry.changesJson);
    return Array.isArray(value)
      ? value.filter(
          (item): item is EquipmentAuditChangeDto =>
            typeof item === "object" && item !== null && typeof item.field === "string",
        )
      : [];
  } catch {
    return [];
  }
}
async function load(page: number) {
  const token = ++sequence;
  loading.value = true;
  error.value = "";
  try {
    const result = await equipmentHistoryApi.list(props.equipmentId, page * limit, limit);
    if (token === sequence) {
      entries.value = result;
      offset.value = page;
    }
  } catch {
    if (token === sequence) error.value = "Impossible de charger l’historique.";
  } finally {
    if (token === sequence) loading.value = false;
  }
}
watch(
  () => props.equipmentId,
  () => {
    entries.value = [];
    offset.value = 0;
    void load(0);
  },
  { immediate: true },
);
</script>
<template>
  <section class="history">
    <h3>Historique du matériel</h3>
    <p
      v-if="loading"
      role="status"
    >
      Chargement de l’historique…
    </p>
    <p
      v-if="error"
      role="alert"
    >
      {{ error }}
      <button
        type="button"
        @click="load(offset)"
      >
        Réessayer
      </button>
    </p>
    <p v-if="!loading && !error && !entries.length">
      Aucune opération enregistrée depuis l’activation du journal.
    </p>
    <ol v-if="entries.length">
      <li
        v-for="entry in entries"
        :key="entry.id"
      >
        <strong>{{
          entry.operation === "Added"
            ? "Création"
            : entry.operation === "Modified"
              ? "Modification"
              : entry.operation
        }}</strong>
        <time :datetime="entry.occurredAt">{{
          new Date(entry.occurredAt).toLocaleString("fr-FR")
        }}</time>
        <p v-if="entry.reason">Motif : {{ entry.reason }}</p>
        <ul>
          <li
            v-for="change in changes(entry)"
            :key="change.field"
          >
            {{ labels[change.field] ?? change.field }} : {{ change.before ?? "—" }} →
            {{ change.after ?? "—" }}
          </li>
        </ul>
      </li>
    </ol>
    <nav
      v-if="offset > 0 || entries.length === limit"
      aria-label="Pages de l’historique"
    >
      <button
        type="button"
        :disabled="loading || offset === 0"
        @click="load(offset - 1)"
      >
        Précédent
      </button>
      <button
        type="button"
        :disabled="loading || entries.length < limit"
        @click="load(offset + 1)"
      >
        Suivant
      </button>
    </nav>
  </section>
</template>
<style scoped>
.history {
  border-top: 1px solid var(--color-border);
  padding-top: 1rem;
}
.history ol {
  padding-left: 1.5rem;
}
.history ol > li {
  padding: 0.65rem 0;
}
.history time {
  display: block;
  color: var(--color-text-secondary);
}
.history p {
  margin: 0.3rem 0;
}
.history ul {
  padding-left: 1.25rem;
}
.history nav {
  display: flex;
  gap: 0.6rem;
}
.history button {
  padding: 0.5rem 0.8rem;
  border: 1px solid var(--color-border);
  border-radius: 0.4rem;
  color: inherit;
  background: var(--color-surface);
  cursor: pointer;
}
</style>
