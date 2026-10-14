<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { equipmentsApi } from "../../api/equipments";
import { inspectionHttpApi } from "../../api/inspections";
import type { InspectionDto } from "../../api/inspections";
import BaseModal from "../common/modal/BaseModal.vue";
import InspectionDetails from "./InspectionDetails.vue";

const route = useRoute();
const router = useRouter();
const inspections = ref<InspectionDto[]>([]);
const equipmentNames = ref<Record<string, string>>({});
const loading = ref(false);
const error = ref("");
const search = ref("");
const type = ref("all");
const order = ref("newest");
const selected = ref<InspectionDto | null>(null);
const equipmentId = computed(() => typeof route.query.equipmentId === "string" ? route.query.equipmentId : "");
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium", timeStyle: "short" });
const filtered = computed(() => inspections.value
  .filter(item => !equipmentId.value || item.equipmentId === equipmentId.value)
  .filter(item => type.value === "all" || item.type === type.value)
  .filter(item => `${equipmentNames.value[item.equipmentId] ?? ""} ${item.operatorFirstName}`
    .toLocaleLowerCase("fr").includes(search.value.trim().toLocaleLowerCase("fr")))
  .sort((a, b) => order.value === "newest"
    ? (b.validatedAt ?? b.startedAt).localeCompare(a.validatedAt ?? a.startedAt)
    : (a.validatedAt ?? a.startedAt).localeCompare(b.validatedAt ?? b.startedAt)));

async function load(): Promise<void> {
  loading.value = true; error.value = "";
  try {
    const all: InspectionDto[] = [];
    // The API currently has no equipmentId filter. Read its pages before filtering locally.
    for (let offset = 0; ; offset += 100) {
      const page = await inspectionHttpApi.list({ status: "Validated", offset, limit: 100 });
      all.push(...page.inspections);
      if (page.inspections.length < 100) break;
    }
    inspections.value = all;
    const ids = [...new Set(all.map(item => item.equipmentId))];
    const names = await Promise.all(ids.map(async id => {
      try { return [id, (await equipmentsApi.getById(id)).name] as const; }
      catch { return [id, id] as const; }
    }));
    equipmentNames.value = Object.fromEntries(names);
    const selectedId = route.query.inspectionId;
    selected.value = typeof selectedId === "string" ? all.find(item => item.id === selectedId) ?? null : null;
  } catch { error.value = "Impossible de charger les états des lieux. Veuillez réessayer."; }
  finally { loading.value = false; }
}
function show(item: InspectionDto): void {
  selected.value = item;
  void router.replace({ query: { ...route.query, inspectionId: item.id } });
}
function close(): void {
  selected.value = null;
  const query = { ...route.query }; delete query.inspectionId;
  void router.replace({ query });
}
function clearEquipment(): void {
  const query = { ...route.query }; delete query.equipmentId;
  void router.replace({ query });
}
watch(() => route.query.inspectionId, id => {
  selected.value = typeof id === "string" ? inspections.value.find(item => item.id === id) ?? null : null;
});
onMounted(() => { void load(); });
</script>
<template>
  <main class="inspection-admin">
    <header><div><h1>{{ equipmentId ? 'Chronologie des états des lieux' : 'États des lieux' }}</h1>
      <p>Consultez les contrôles réalisés et les documents signés.</p></div>
      <button type="button" :disabled="loading" @click="load">Actualiser</button></header>
    <p v-if="equipmentId" class="inspection-admin__context">
      Équipement : {{ equipmentNames[equipmentId] ?? equipmentId }}
      <button type="button" @click="clearEquipment">Afficher tous les états des lieux</button>
    </p>
    <p v-if="error" role="alert">{{ error }} <button type="button" @click="load">Réessayer</button></p>
    <p v-if="loading" role="status">Chargement des états des lieux…</p>
    <template v-else-if="!error">
      <div class="inspection-admin__filters">
        <label>Rechercher<input v-model="search" type="search" placeholder="Équipement ou opérateur" /></label>
        <label>Type<select v-model="type"><option value="all">Tous</option><option value="Departure">Départs</option><option value="Return">Retours</option></select></label>
        <label>Ordre<select v-model="order"><option value="newest">Plus récents</option><option value="oldest">Plus anciens</option></select></label>
      </div>
      <p role="status">{{ filtered.length }} état(s) des lieux</p>
      <p v-if="!filtered.length">Aucun état des lieux ne correspond à ces critères.</p>
      <ul v-else class="inspection-admin__list">
        <li v-for="item in filtered" :key="item.id">
          <div><strong>{{ equipmentNames[item.equipmentId] ?? item.equipmentId }}</strong>
            <span>{{ item.type === 'Departure' ? 'Départ' : 'Retour' }} - {{ date.format(new Date(item.validatedAt ?? item.startedAt)) }}</span>
            <span>Opérateur : {{ item.operatorFirstName }} - {{ item.answers.filter(a => a.result === 'NonCompliant').length }} défaut(s)</span></div>
          <button type="button" @click="show(item)">Consulter</button>
        </li>
      </ul>
    </template>
    <BaseModal :open="Boolean(selected)" title="Détail de l’état des lieux" @close="close">
      <InspectionDetails v-if="selected" :inspection="selected" />
    </BaseModal>
  </main>
</template>
<style scoped lang="scss">
.inspection-admin { display: grid; gap: 1.25rem; max-width: 78rem; margin-inline: auto; }
header { display: flex; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
h1, header p { margin: 0; }
.inspection-admin__context { padding: .8rem; background: var(--color-surface-secondary); border-radius: .5rem; }
.inspection-admin__filters { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: .75rem; }
.inspection-admin__filters label { display: grid; gap: .35rem; }
.inspection-admin__filters input, .inspection-admin__filters select { min-height: 2.75rem; padding: .5rem;
  color: var(--color-text); background: var(--color-input-background); border: 1px solid var(--color-border); border-radius: .5rem; }
.inspection-admin__list { display: grid; gap: .6rem; list-style: none; padding: 0; }
.inspection-admin__list li { display: flex; justify-content: space-between; align-items: center; gap: 1rem;
  padding: .85rem; border: 1px solid var(--color-border); border-radius: .6rem; background: var(--color-surface); }
.inspection-admin__list li div { display: grid; gap: .3rem; }
.inspection-admin__list span { color: var(--color-text-secondary); }
button { min-height: 2.5rem; padding: .45rem .7rem; color: var(--color-text); background: var(--color-surface);
  border: 1px solid var(--color-border); border-radius: .5rem; cursor: pointer; }
button:focus-visible { outline: 3px solid var(--color-focus); }
@media (max-width: 40rem) { .inspection-admin__filters { grid-template-columns: 1fr; }
  .inspection-admin__list li { align-items: start; flex-direction: column; } }
</style>
