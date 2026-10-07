<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { agenciesApi } from "../../api/agencies";
import type { AgencyDto, SaveAgencyDto } from "../../api/agencies/dto";
import BaseButton from "../common/button/BaseButton.vue";
import BaseModal from "../common/modal/BaseModal.vue";
import BaseSpinner from "../common/spinner/BaseSpinner.vue";

const agencies = ref<AgencyDto[]>([]);
const nameFilter = ref("");
const codeFilter = ref("");
type SortKey = "code" | "name";
const sortKey = ref<SortKey>("code");
const sortDirection = ref<"asc" | "desc">("asc");
const collator = new Intl.Collator("fr", { sensitivity: "base", numeric: true });
const loading = ref(false);
const busy = ref(false);
const error = ref("");
const formError = ref("");
const notice = ref("");
const modalOpen = ref(false);
const modalContentVisible = ref(false);
const editingId = ref<string | null>(null);
const name = ref("");
const code = ref<number | null>(null);
const form = ref<HTMLFormElement | null>(null);

const filtered = computed(() =>
  agencies.value
    .filter((agency) => {
      const name = nameFilter.value.trim().toLocaleLowerCase("fr");
      const code = codeFilter.value.trim();
      return (!name || agency.name.toLocaleLowerCase("fr").includes(name))
        && (!code || String(agency.code).includes(code));
    })
    .sort((a, b) => {
      const left = String(a[sortKey.value]);
      const right = String(b[sortKey.value]);
      return (sortDirection.value === "asc" ? 1 : -1) * collator.compare(left, right)
        || collator.compare(a.id, b.id);
    }),
);
function toggleSort(key: SortKey): void {
  if (sortKey.value === key) sortDirection.value = sortDirection.value === "asc" ? "desc" : "asc";
  else { sortKey.value = key; sortDirection.value = "asc"; }
}
function ariaSort(key: SortKey): "none" | "ascending" | "descending" {
  return sortKey.value === key ? (sortDirection.value === "asc" ? "ascending" : "descending") : "none";
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = "";
  try {
    agencies.value = (await agenciesApi.list()).agencies;
  } catch {
    error.value = "Impossible de charger les agences. Veuillez réessayer.";
  } finally {
    loading.value = false;
  }
}
function openForm(agency?: AgencyDto): void {
  editingId.value = agency?.id ?? null;
  name.value = agency?.name ?? "";
  code.value = agency?.code ?? null;
  formError.value = "";
  modalContentVisible.value = true;
  modalOpen.value = true;
}
function closeForm(): void { if (!busy.value) modalOpen.value = false; }
function clearForm(): void { if (!modalOpen.value) modalContentVisible.value = false; }
async function save(): Promise<void> {
  if (busy.value || code.value === null || !name.value.trim() || !Number.isInteger(code.value) || code.value <= 0) return;
  busy.value = true;
  formError.value = "";
  try {
    const dto: SaveAgencyDto = { name: name.value.trim(), code: code.value };
    if (editingId.value) await agenciesApi.update(editingId.value, dto);
    else await agenciesApi.create(dto);
    notice.value = editingId.value ? "L’agence a été modifiée." : "L’agence a été créée.";
    modalOpen.value = false;
    await load();
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : "Impossible d’enregistrer l’agence.";
  } finally { busy.value = false; }
}
onMounted(() => { void load(); });
</script>

<template>
  <section class="management" aria-labelledby="agencies-title">
    <header class="management__header">
      <div>
        <h1 id="agencies-title">Gestion des agences</h1>
        <p class="management__subtitle">Consultez et modifiez les agences enregistrées.</p>
      </div>
      <div class="management__actions">
        <BaseButton variant="outline" size="small" :loading="loading" @click="load">Actualiser</BaseButton>
        <BaseButton size="small" @click="openForm()">Créer une agence</BaseButton>
      </div>
    </header>

    <p v-if="notice" class="management__notice" role="status">{{ notice }}</p>
    <div v-if="error" class="management__error" role="alert">
      <p>{{ error }}</p>
      <BaseButton variant="outline" size="small" @click="load">Réessayer</BaseButton>
    </div>

    <div class="management__panel">
      <div class="management__panel-head">
        <h2>Agences</h2>
        <span class="management__count">{{ filtered.length }} résultat{{ filtered.length > 1 ? "s" : "" }}</span>
      </div>
      <div class="management__toolbar">
        <label for="agency-name-filter">Nom</label>
        <input id="agency-name-filter" v-model="nameFilter" class="management__input management__search" type="search" placeholder="Nom de l’agence" />
        <label for="agency-code-filter">Code</label>
        <input id="agency-code-filter" v-model="codeFilter" class="management__input" type="search" inputmode="numeric" placeholder="Code" />
      </div>
      <div v-if="loading" class="management__state" role="status"><BaseSpinner size="medium" /> Chargement des agences…</div>
      <p v-else-if="!error && filtered.length === 0" class="management__state">Aucune agence ne correspond à la recherche.</p>
      <div v-else-if="!error" class="app-table-scroll">
        <div class="app-table__mobile-sort">
          <label for="agency-sort">Trier par</label>
          <select id="agency-sort" v-model="sortKey" class="management__select">
            <option value="code">Code</option><option value="name">Nom</option>
          </select>
          <button type="button" class="app-table__sort-direction" :aria-label="sortDirection === 'asc' ? 'Tri croissant, inverser' : 'Tri décroissant, inverser'" @click="sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'">
            {{ sortDirection === "asc" ? "↑" : "↓" }}
          </button>
        </div>
        <table class="app-table">
          <thead><tr>
            <th v-for="column in ([
              { key: 'code', label: 'Code' },
              { key: 'name', label: 'Nom' },
            ] as const)" :key="column.key" scope="col" :aria-sort="ariaSort(column.key)">
              <button type="button" class="app-table__sort-button" @click="toggleSort(column.key)">
                {{ column.label }} <span aria-hidden="true">{{ sortKey === column.key ? (sortDirection === "asc" ? "▲" : "▼") : "↕" }}</span>
              </button>
            </th>
            <th scope="col">Actions</th>
          </tr></thead>
          <tbody>
            <tr v-for="agency in filtered" :key="agency.id">
              <th scope="row">{{ agency.code }}</th>
              <td data-label="Nom">{{ agency.name }}</td>
              <td class="management__actions-cell">
                <div class="management__row-actions">
                  <BaseButton variant="outline" size="small" :disabled="busy" @click="openForm(agency)">Modifier</BaseButton>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <BaseModal :open="modalOpen" :busy="busy" :title="editingId ? 'Modifier une agence' : 'Créer une agence'" @close="closeForm" @closed="clearForm">
      <form v-if="modalContentVisible" ref="form" class="management__form" @submit.prevent="save">
        <div class="management__field"><label for="agency-name">Nom</label>
          <input id="agency-name" v-model="name" class="management__input" required maxlength="120" :disabled="busy" />
        </div>
        <div class="management__field"><label for="agency-code">Code</label>
          <input id="agency-code" v-model.number="code" class="management__input" type="number" min="1" step="1" required :disabled="busy" />
        </div>
        <p v-if="formError" class="management__error" role="alert">{{ formError }}</p>
        <div class="management__form-actions">
          <BaseButton variant="outline" :disabled="busy" @click="closeForm">Annuler</BaseButton>
          <BaseButton :loading="busy" @click="form?.requestSubmit()">Enregistrer</BaseButton>
        </div>
      </form>
    </BaseModal>
  </section>
</template>

<style scoped lang="scss">
.management {
  display: grid; gap: 1.25rem; min-width: 0; color: var(--color-text);
  &__header, &__actions, &__toolbar, &__panel-head, &__row-actions { display: flex; align-items: center; gap: .75rem; }
  &__header, &__panel-head { justify-content: space-between; flex-wrap: wrap; }
  &__header h1, &__panel-head h2 { margin: 0; }
  &__subtitle, &__count { color: var(--color-text-secondary); }
  &__subtitle { margin: .35rem 0 0; }
  &__panel { border: 1px solid var(--color-border); border-radius: .75rem; background: var(--color-surface); overflow: hidden; }
  &__panel-head { padding: 1rem 1.25rem; border-bottom: 1px solid var(--color-border); }
  &__panel-head h2 { font-size: 1rem; }
  &__toolbar { flex-wrap: wrap; padding: 1rem 1.25rem; border-bottom: 1px solid var(--color-border); }
  &__input, &__select {
    box-sizing: border-box; min-height: 2.75rem; padding: .625rem .75rem;
    border: 1px solid var(--color-border); border-radius: .5rem;
    background: var(--color-surface); color: var(--color-text); font: inherit;
    &:focus-visible { outline: .1875rem solid var(--color-focus); outline-offset: .125rem; }
  }
  &__search { flex: 1 1 15rem; min-width: 0; }
  &__state { display: flex; justify-content: center; align-items: center; gap: .75rem; padding: 2.5rem 1rem; color: var(--color-text-secondary); }
  &__error { padding: 1rem; border: 1px solid var(--color-danger); border-radius: .5rem; color: var(--color-danger); }
  &__notice { margin: 0; padding: .75rem 1rem; border-radius: .5rem; background: var(--color-primary-soft); }
  &__row-actions { justify-content: flex-end; }
  &__badge { display: inline-block; padding: .25rem .6rem; border-radius: 999px; background: var(--color-surface-secondary); white-space: nowrap; }
  &__badge--active { color: var(--color-success); background: var(--color-success-soft); }
  &__form { display: grid; gap: 1rem; }
  &__field { display: grid; gap: .4rem; }
  &__field label { font-weight: var(--font-weight-semibold); }
  &__field input, &__field select { width: 100%; }
  &__hint { margin: 0; color: var(--color-text-secondary); font-size: var(--font-size-sm); }
  &__form-actions { display: flex; justify-content: flex-end; flex-wrap: wrap; gap: .75rem; margin-top: .5rem; }
}
@media (max-width: 42rem) {
  .management {
    &__header { align-items: stretch; }
    &__actions { width: 100%; flex-wrap: wrap; }
    &__actions > * { flex: 1 1 auto; }
    &__toolbar { align-items: stretch; display: grid; grid-template-columns: minmax(0, 1fr); gap: .45rem; }
    &__toolbar label:not(:first-child) { margin-top: .4rem; }
    &__row-actions { justify-content: flex-start; padding-top: .3rem; flex-wrap: wrap; }
  }
}
</style>
