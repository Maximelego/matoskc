<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { adminInspectionsApi, type InspectionRecord } from "../../api/inspections/admin";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseModal from "../common/modal/BaseModal.vue";
import InspectionDetails from "./InspectionDetails.vue";

const inspections = ref<InspectionRecord[]>([]);
const loading = ref(true);
const error = ref("");
const search = ref("");
const direction = ref("all");
const order = ref("newest");
const selected = ref<InspectionRecord | null>(null);
const detailOpen = ref(false);
const formatDate = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium", timeStyle: "short" });

const filtered = computed(() => {
  const query = search.value.trim().toLocaleLowerCase("fr");
  return inspections.value
    .filter((item) => direction.value === "all" || item.direction === direction.value)
    .filter((item) => `${item.equipmentName} ${item.serialNumber} ${item.operatorFirstName}`.toLocaleLowerCase("fr").includes(query))
    .sort((a, b) => order.value === "newest"
      ? b.performedAt.localeCompare(a.performedAt)
      : a.performedAt.localeCompare(b.performedAt));
});

async function load(): Promise<void> {
  loading.value = true;
  error.value = "";
  try {
    inspections.value = await adminInspectionsApi.list();
  } catch {
    error.value = "Impossible de charger les états des lieux. Veuillez réessayer.";
  } finally {
    loading.value = false;
  }
}

function showDetail(item: InspectionRecord): void {
  selected.value = item;
  detailOpen.value = true;
}

onMounted(() => { void load(); });
</script>

<template>
  <main class="inspection-admin" aria-labelledby="inspection-admin-title">
    <header class="inspection-admin__header">
      <div>
        <h1 id="inspection-admin-title">États des lieux</h1>
        <p>Consultez les contrôles et les défauts relevés sur les équipements.</p>
      </div>
      <BaseButton variant="outline" size="small" :loading="loading" @click="load">
        <template #leading><BaseIcon name="refresh" :size="18" /></template>
        Actualiser
      </BaseButton>
    </header>

    <p class="inspection-admin__demo" role="note">
      Données de démonstration : les inspections saisies dans le formulaire ne sont pas encore enregistrées.
    </p>

    <div v-if="error" class="inspection-admin__error" role="alert">
      <p>{{ error }}</p>
      <BaseButton variant="outline" size="small" @click="load">Réessayer</BaseButton>
    </div>
    <p v-if="loading" role="status">Chargement des états des lieux…</p>

    <template v-else-if="!error">
      <div class="inspection-admin__filters">
        <label>Rechercher
          <input v-model="search" type="search" placeholder="Équipement, série ou opérateur" />
        </label>
        <label>Type
          <select v-model="direction">
            <option value="all">Tous</option>
            <option value="departure">Départs</option>
            <option value="return">Retours</option>
          </select>
        </label>
        <label>Trier par date
          <select v-model="order">
            <option value="newest">Plus récents</option>
            <option value="oldest">Plus anciens</option>
          </select>
        </label>
      </div>

      <p class="inspection-admin__count" role="status">{{ filtered.length }} état{{ filtered.length > 1 ? "s" : "" }} des lieux</p>
      <p v-if="!filtered.length" class="inspection-admin__empty">Aucun état des lieux ne correspond à ces critères.</p>

      <div v-else class="inspection-admin__table-scroll">
        <table class="inspection-admin__table">
          <thead><tr><th scope="col">Équipement</th><th scope="col">Date</th><th scope="col">Type</th><th scope="col">Opérateur</th><th scope="col">Défauts</th><th scope="col">Détails</th></tr></thead>
          <tbody>
            <tr v-for="item in filtered" :key="item.id">
              <th scope="row"><strong>{{ item.equipmentName }}</strong><small>{{ item.serialNumber }}</small></th>
              <td>{{ formatDate.format(new Date(item.performedAt)) }}</td>
              <td>{{ item.direction === "return" ? "Retour" : "Départ" }}</td>
              <td>{{ item.operatorFirstName }}</td>
              <td>{{ item.sections.flatMap(section => section.answers).filter(answer => answer.value === "nonCompliant").length }}</td>
              <td><button type="button" class="inspection-admin__view" :aria-label="`Consulter l’état des lieux de ${item.equipmentName}`" @click="showDetail(item)"><BaseIcon name="view" :size="18" /><span>Consulter</span></button></td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <BaseModal :open="detailOpen" title="Détail de l’état des lieux" @close="detailOpen = false" @closed="selected = null">
      <InspectionDetails v-if="selected" :inspection="selected" />
    </BaseModal>
  </main>
</template>

<style scoped lang="scss">
.inspection-admin { display: grid; gap: 1.25rem; min-width: 0; max-width: 80rem; margin-inline: auto; }
.inspection-admin__header { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 1rem; }
.inspection-admin__header h1 { margin: 0; }
.inspection-admin__header p { margin: .4rem 0 0; color: var(--color-text-secondary); }
.inspection-admin__demo { margin: 0; padding: .8rem 1rem; border-radius: .5rem; background: var(--color-warning-soft); }
.inspection-admin__error { padding: 1rem; border: 1px solid var(--color-danger); border-radius: .65rem; }
.inspection-admin__filters { display: grid; grid-template-columns: minmax(12rem, 2fr) repeat(2, minmax(10rem, 1fr)); gap: .75rem; }
.inspection-admin__filters label { display: grid; gap: .35rem; }
.inspection-admin__filters input, .inspection-admin__filters select { box-sizing: border-box; width: 100%; min-height: 2.75rem; padding: .55rem; border: 1px solid var(--color-border); border-radius: .5rem; color: var(--color-text); background: var(--color-surface); font: inherit; }
.inspection-admin__count { margin: 0; color: var(--color-text-secondary); }
.inspection-admin__empty { padding: 2rem; text-align: center; color: var(--color-text-secondary); }
.inspection-admin__table-scroll { overflow-x: auto; border: 1px solid var(--color-border); border-radius: .75rem; background: var(--color-surface); }
.inspection-admin__table { width: 100%; border-collapse: collapse; text-align: left; }
.inspection-admin__table th, .inspection-admin__table td { padding: .85rem 1rem; border-bottom: 1px solid var(--color-border); }
.inspection-admin__table thead { background: var(--color-surface-secondary); }
.inspection-admin__table tbody tr:nth-child(even) { background: var(--color-surface-secondary); }
.inspection-admin__table tbody tr:hover, .inspection-admin__table tbody tr:focus-within { background: var(--color-primary-soft); }
.inspection-admin__table tbody th small { display: block; margin-top: .2rem; color: var(--color-text-secondary); font-weight: normal; }
.inspection-admin__view { display: inline-flex; align-items: center; gap: .4rem; min-height: 2.75rem; border: 0; color: var(--color-primary); background: transparent; font: inherit; cursor: pointer; }
.inspection-admin__view:focus-visible { outline: .1875rem solid var(--color-focus); outline-offset: .125rem; }
@media(max-width: 48rem) {
  .inspection-admin__filters { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .inspection-admin__filters label:first-child { grid-column: 1 / -1; }
  .inspection-admin__table-scroll { overflow: visible; border: 0; background: transparent; }
  .inspection-admin__table, .inspection-admin__table tbody, .inspection-admin__table tr { display: block; }
  .inspection-admin__table thead { display: none; }
  .inspection-admin__table tbody tr { display: grid; gap: .25rem; margin-bottom: .75rem; padding: .75rem; border: 1px solid var(--color-border); border-radius: .75rem; background: var(--color-surface); }
  .inspection-admin__table th, .inspection-admin__table td { padding: .2rem; border: 0; }
  .inspection-admin__table td:last-child { justify-self: start; }
}
@media(max-width: 24rem) { .inspection-admin__filters { grid-template-columns: 1fr; } .inspection-admin__filters label:first-child { grid-column: auto; } }
</style>
