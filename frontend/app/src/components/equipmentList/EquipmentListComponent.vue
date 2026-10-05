<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import { ApiError } from "../../api/client";
import { equipmentCategoriesApi } from "../../api/equipment-categories";
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto, EquipmentFilters, EquipmentStatus } from "../../api/equipments/dto";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseModal from "../common/modal/BaseModal.vue";
import BaseSpinner from "../common/spinner/BaseSpinner.vue";
import EquipmentCreateForm from "../equipmentCreate/EquipmentCreateForm.vue";
import EquipmentStatusBadge from "../equipmentStatusBadge/EquipmentStatusBadge.vue";
import EquipmentDetailsModal from "./EquipmentDetailsModal.vue";
import EquipmentFiltersControl from "./EquipmentFilters.vue";

const selectedEquipment = ref<EquipmentDto | null>(null);
const detailsOpen = ref(false);
const createContentVisible = ref(false);
function openCreate(): void {
  createContentVisible.value = true;
  createOpen.value = true;
}
function closeCreate(): void {
  createOpen.value = false;
}
function clearCreate(): void {
  if (!createOpen.value) createContentVisible.value = false;
}
function closeDetails(): void {
  detailsOpen.value = false;
}
function clearDetails(): void {
  if (!detailsOpen.value) selectedEquipment.value = null;
}
type SortKey = "name" | "serialNumber" | "equipmentCategoryId" | "status";
type SortDirection = "asc" | "desc";
const statusLabels: Record<string, string> = {
  Available: "Disponible",
  Unavailable: "Indisponible",
  ToBeDecided: "À décider",
  Decommissioned: "Réformé",
  Maintenance: "En maintenance",
  Borrowed: "Emprunté",
};
const equipments = ref<EquipmentDto[]>([]);
const categories = ref<EquipmentCategoryDto[]>([]);
const filters = ref<EquipmentFilters>({});
const loading = ref(false);
const categoriesLoading = ref(false);
const categoriesError = ref(false);
const error = ref<string | null>(null);
const notice = ref("");
const createOpen = ref(false);
const createBusy = ref(false);
const sortKey = ref<SortKey>("name");
const sortDirection = ref<SortDirection>("asc");
const collator = new Intl.Collator("fr", {
  numeric: true,
  sensitivity: "base",
});
let searchTimer: ReturnType<typeof setTimeout> | undefined;
let requestVersion = 0;
const categoryNames = computed(
  () => new Map(categories.value.map((category) => [category.id, category.name])),
);
function categoryName(equipment: EquipmentDto): string {
  return categoryNames.value.get(equipment.equipmentCategoryId) ?? equipment.equipmentCategoryId;
}
function statusLabel(equipment: EquipmentDto): string {
  return statusLabels[equipment.status] ?? equipment.status;
}
const sortedEquipments = computed(() =>
  [...equipments.value].sort((first, second) => {
    function sortValue(equipment: EquipmentDto): string {
      if (sortKey.value === "equipmentCategoryId") {
        return categoryName(equipment);
      }
      if (sortKey.value === "status") {
        return statusLabel(equipment);
      }
      return equipment[sortKey.value];
    }
    const comparison = collator.compare(sortValue(first), sortValue(second));
    return (
      (sortDirection.value === "asc" ? comparison : -comparison) ||
      collator.compare(first.id, second.id)
    );
  }),
);
function toggleSort(key: SortKey): void {
  if (sortKey.value === key) {
    sortDirection.value = sortDirection.value === "asc" ? "desc" : "asc";
    return;
  }
  sortKey.value = key;
  sortDirection.value = "asc";
}
function ariaSort(key: SortKey): "ascending" | "descending" | "none" {
  if (sortKey.value !== key) return "none";
  return sortDirection.value === "asc" ? "ascending" : "descending";
}
function openDetails(equipment: EquipmentDto): void {
  selectedEquipment.value = equipment;
  detailsOpen.value = true;
}
function equipmentErrorMessage(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.status === 401) {
      return "Veuillez vous connecter pour consulter les équipements.";
    }
    if (cause.status === 403) {
      return "Vous ne disposez pas des droits nécessaires pour consulter les équipements.";
    }
    if (cause.status >= 500) {
      return "Le serveur rencontre un problème. Veuillez réessayer dans quelques instants.";
    }
    return cause.message;
  }
  if (cause instanceof TypeError) {
    return "Impossible de joindre l’API. Veuillez vérifier votre connexion et la disponibilité du serveur.";
  }
  return "Impossible de charger les équipements.";
}
async function loadEquipments(): Promise<void> {
  const version = ++requestVersion;
  loading.value = true;
  error.value = null;
  try {
    const result = await equipmentsApi.list({
      categoryId: filters.value.categoryId,
      status: filters.value.status,
      search: filters.value.search?.trim() || undefined,
    });
    if (version === requestVersion) {
      equipments.value = result.equipments;
    }
  } catch (cause: unknown) {
    if (version !== requestVersion) return;
    equipments.value = [];
    error.value = equipmentErrorMessage(cause);
  } finally {
    if (version === requestVersion) {
      loading.value = false;
    }
  }
}
async function loadCategories(): Promise<void> {
  categoriesLoading.value = true;
  categoriesError.value = false;
  try {
    const result = await equipmentCategoriesApi.list();
    categories.value = result.equipmentCategories;
  } catch {
    categoriesError.value = true;
  } finally {
    categoriesLoading.value = false;
  }
}
function updateFilters(next: EquipmentFilters): void {
  const searchChanged = next.search !== filters.value.search;
  filters.value = next;
  if (searchTimer) clearTimeout(searchTimer);
  requestVersion++;
  equipments.value = [];
  loading.value = true;
  error.value = null;
  if (searchChanged) {
    searchTimer = setTimeout(() => {
      void loadEquipments();
    }, 300);
  } else {
    void loadEquipments();
  }
}
async function handleCreated(): Promise<void> {
  closeCreate();
  notice.value = "L’équipement a été créé.";
  await loadEquipments();
}
async function handleStatusChange(equipment: EquipmentDto, status: EquipmentStatus): Promise<void> {
  try {
    await equipmentsApi.updateStatus(equipment.id, status);
    notice.value = "L’état de l’équipement a été mis à jour.";
    closeDetails();
    await loadEquipments();
  } catch (cause: unknown) {
    error.value = equipmentErrorMessage(cause);
  }
}
onMounted(() => {
  void loadCategories();
  void loadEquipments();
});
onUnmounted(() => {
  if (searchTimer) clearTimeout(searchTimer);
  requestVersion++;
});
</script>
<template>
  <section
    class="equipment-list"
    aria-labelledby="equipment-list-title"
  >
    <div class="equipment-list__header">
      <div>
        <h1 id="equipment-list-title">Équipements</h1>
        <p class="equipment-list__subtitle">Consultez et gérez les équipements enregistrés.</p>
      </div>
      <div class="equipment-list__header-actions">
        <BaseButton
          variant="outline"
          size="small"
          :loading="loading"
          @click="loadEquipments"
        >
          <template #leading>
            <BaseIcon name="refresh" />
          </template>
          Actualiser
        </BaseButton>
        <BaseButton
          variant="primary"
          size="small"
          @click="openCreate"
        >
          <template #leading>
            <BaseIcon name="add" />
          </template>
          Créer un équipement
        </BaseButton>
      </div>
    </div>
    <p
      v-if="notice"
      class="equipment-list__notice"
      role="status"
    >
      {{ notice }}
    </p>
    <div
      v-if="error"
      class="equipment-list__error"
      role="alert"
    >
      <p>{{ error }}</p>
      <BaseButton
        variant="outline"
        size="small"
        @click="loadEquipments"
      >
        Réessayer
      </BaseButton>
    </div>
    <div class="equipment-list__panel">
      <div class="equipment-list__panel-header">
        <h2>Liste des équipements</h2>
        <span
          v-if="!loading && !error"
          class="equipment-list__count"
        >
          {{ equipments.length }}
          équipement{{ equipments.length > 1 ? "s" : "" }}
        </span>
      </div>
      <EquipmentFiltersControl
        :model-value="filters"
        :categories="categories"
        :categories-loading="categoriesLoading"
        :categories-error="categoriesError"
        @update:model-value="updateFilters"
        @retry-categories="loadCategories"
      />
      <div
        v-if="loading"
        class="equipment-list__state"
        role="status"
      >
        <BaseSpinner size="medium" />
        <span>Chargement des équipements…</span>
      </div>
      <p
        v-else-if="!error && equipments.length === 0"
        class="equipment-list__state"
      >
        Aucun équipement ne correspond aux filtres sélectionnés.
      </p>
      <div
        v-if="!loading && !error && equipments.length > 0"
        class="equipment-list__table-container"
      >
        <div class="equipment-list__mobile-sort">
          <label for="equipment-sort">Trier par</label>
          <select
            id="equipment-sort"
            :value="sortKey"
            @change="sortKey = ($event.target as HTMLSelectElement).value as SortKey"
          >
            <option value="name">Équipement</option>
            <option value="serialNumber">Numéro de série</option>
            <option value="equipmentCategoryId">Catégorie</option>
            <option value="status">État</option>
          </select>
          <button
            type="button"
            :aria-label="
              sortDirection === 'asc' ? 'Tri croissant, inverser' : 'Tri décroissant, inverser'
            "
            @click="sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'"
          >
            {{ sortDirection === "asc" ? "↑" : "↓" }}
          </button>
        </div>
        <table class="equipment-list__table">
          <thead>
            <tr>
              <th
                scope="col"
                :aria-sort="ariaSort('name')"
              >
                <button
                  type="button"
                  @click="toggleSort('name')"
                >
                  Équipement
                  <span aria-hidden="true">
                    {{ sortKey === "name" ? (sortDirection === "asc" ? "▲" : "▼") : "↕" }}
                  </span>
                </button>
              </th>
              <th
                scope="col"
                :aria-sort="ariaSort('serialNumber')"
              >
                <button
                  type="button"
                  @click="toggleSort('serialNumber')"
                >
                  Numéro de série
                  <span aria-hidden="true">
                    {{ sortKey === "serialNumber" ? (sortDirection === "asc" ? "▲" : "▼") : "↕" }}
                  </span>
                </button>
              </th>
              <th
                scope="col"
                :aria-sort="ariaSort('equipmentCategoryId')"
              >
                <button
                  type="button"
                  @click="toggleSort('equipmentCategoryId')"
                >
                  Catégorie
                  <span aria-hidden="true">
                    {{
                      sortKey === "equipmentCategoryId"
                        ? sortDirection === "asc"
                          ? "▲"
                          : "▼"
                        : "↕"
                    }}
                  </span>
                </button>
              </th>
              <th
                scope="col"
                :aria-sort="ariaSort('status')"
              >
                <button
                  type="button"
                  @click="toggleSort('status')"
                >
                  État
                  <span aria-hidden="true">
                    {{ sortKey === "status" ? (sortDirection === "asc" ? "▲" : "▼") : "↕" }}
                  </span>
                </button>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="equipment in sortedEquipments"
              :key="equipment.id"
              class="equipment-list__clickable-row"
              tabindex="0"
              :aria-label="`Consulter les détails de ${equipment.name}`"
              @click="openDetails(equipment)"
              @keydown.enter.prevent="openDetails(equipment)"
              @keydown.space.prevent="openDetails(equipment)"
            >
              <th
                scope="row"
                class="equipment-list__name"
              >
                {{ equipment.name }}
              </th>
              <td
                class="equipment-list__serial"
                data-label="Numéro de série"
              >
                {{ equipment.serialNumber || "—" }}
              </td>
              <td
                class="equipment-list__category"
                data-label="Catégorie"
              >
                {{ categoryName(equipment) }}
              </td>
              <td data-label="État">
                <EquipmentStatusBadge :status="equipment.status" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
    <BaseModal
      :open="createOpen"
      :busy="createBusy"
      title="Créer un équipement"
      @close="closeCreate"
      @closed="clearCreate"
    >
      <EquipmentCreateForm
        v-if="createContentVisible"
        @cancel="closeCreate"
        @busy="createBusy = $event"
        @created="handleCreated"
      />
    </BaseModal>
    <EquipmentDetailsModal
      :equipment="selectedEquipment"
      :equipment-categories="categories"
      :open="detailsOpen"
      @close="closeDetails"
      @closed="clearDetails"
      @change-status="handleStatusChange"
    />
  </section>
</template>
<style scoped lang="scss">
.equipment-list {
  display: grid;
  gap: 1.25rem;
  &__header,
  &__header-actions,
  &__panel-header,
  &__row-actions {
    display: flex;
    align-items: center;
  }
  &__header {
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 1rem;
  }
  &__header h1 {
    margin: 0;
  }
  &__subtitle {
    margin: 0.35rem 0 0;
    color: var(--color-text-secondary);
  }
  &__header-actions {
    flex-wrap: wrap;
    gap: 0.75rem;
  }
  &__notice {
    margin: 0;
    padding: 0.75rem 1rem;
    border-radius: 0.5rem;
    background: var(--color-primary-soft);
  }
  &__error {
    padding: 1rem;
    border: 1px solid var(--color-danger);
    border-radius: 0.5rem;
    color: var(--color-danger);
  }
  &__error p {
    margin-top: 0;
  }
  &__panel {
    overflow: hidden;
    border: 1px solid var(--color-border);
    border-radius: 0.75rem;
    background: var(--color-surface);
  }
  &__panel-header {
    justify-content: space-between;
    gap: 1rem;
    padding: 1rem 1.25rem;
    border-bottom: 1px solid var(--color-border);
  }
  &__panel-header h2 {
    margin: 0;
    font-size: 1rem;
  }
  &__count {
    color: var(--color-text-secondary);
    white-space: nowrap;
  }
  &__state {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    min-height: 8rem;
    margin: 0;
    padding: 1rem;
    color: var(--color-text-secondary);
  }
  &__table-container {
    overflow-x: auto;
  }
  &__mobile-sort {
    display: none;
  }
  &__table {
    width: 100%;
    border-collapse: collapse;
    text-align: left;
  }
  &__table thead {
    background: var(--color-surface-secondary);
  }
  &__table th,
  &__table td {
    padding: 0.875rem 1.25rem;
    border-bottom: 1px solid var(--color-border);
    vertical-align: middle;
  }
  &__table tr {
    cursor: pointer;
  }
  &__table thead th {
    color: var(--color-text-secondary);
    font-size: var(--font-size-sm);
    white-space: nowrap;
  }
  &__table thead button {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0;
    border: 0;
    color: inherit;
    background: none;
    font: inherit;
    font-weight: var(--font-weight-semibold);
    cursor: pointer;
  }
  &__table thead button:hover {
    color: var(--color-primary);
  }
  &__table thead button:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
    outline-offset: 0.25rem;
  }
  &__table thead button span {
    font-size: 0.7rem;
  }
  &__table tbody tr:last-child th,
  &__table tbody tr:last-child td {
    border-bottom: 0;
  }
  &__table tbody tr:nth-child(even) {
    background: var(--color-surface-secondary);
  }
  &__table tbody tr:hover,
  &__table tbody tr:focus-within {
    background: var(--color-primary-soft);
  }
  &__name {
    font-weight: var(--font-weight-semibold);
  }
  &__serial,
  &__category {
    color: var(--color-text-secondary);
  }
  &__actions-heading,
  &__actions-cell {
    width: 1%;
    text-align: right;
  }
  &__row-actions {
    justify-content: flex-end;
    gap: 0.25rem;
    opacity: 0;
    transition: opacity 150ms ease;
  }
  tr:hover &__row-actions,
  tr:focus-within &__row-actions {
    opacity: 1;
  }
  &__icon-button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 2.25rem;
    height: 2.25rem;
    padding: 0;
    border: 1px solid transparent;
    border-radius: 0.5rem;
    color: var(--color-text-secondary);
    background: transparent;
    cursor: pointer;
  }
  &__icon-button:hover {
    color: var(--color-primary);
    background: var(--color-primary-soft);
  }
  &__icon-button:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
    outline-offset: 0.125rem;
  }
}
@media (hover: none) {
  .equipment-list__row-actions {
    opacity: 1;
  }
}
@media (prefers-reduced-motion: reduce) {
  .equipment-list__row-actions {
    transition: none;
  }
}
</style>
<style scoped lang="scss">
@media (max-width: 42rem) {
  .equipment-list {
    min-width: 0;
    gap: 1rem;
    &__header,
    &__header-actions {
      display: grid;
      width: 100%;
    }
    &__header-actions {
      grid-template-columns: 1fr 1fr;
    }
    &__header-actions :deep(button) {
      width: 100%;
      min-height: 2.75rem;
    }
    &__panel-header {
      padding: 0.875rem 1rem;
      flex-wrap: wrap;
    }
    &__table-container {
      overflow: visible;
      padding: 0 0.75rem 0.75rem;
    }
    &__mobile-sort {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 0.5rem;
      padding: 0.75rem 0.25rem;
      align-items: center;
    }
    &__mobile-sort label {
      grid-column: 1 / -1;
    }
    &__mobile-sort select,
    &__mobile-sort button {
      min-width: 0;
      min-height: 2.75rem;
      padding: 0.5rem;
      border: 1px solid var(--color-border);
      border-radius: 0.5rem;
      color: var(--color-text);
      background: var(--color-input-background);
    }
    &__mobile-sort button {
      min-width: 2.75rem;
      cursor: pointer;
    }
    &__table,
    &__table tbody,
    &__table tr {
      display: block;
      width: 100%;
      box-sizing: border-box;
    }
    &__table thead {
      display: none;
    }
    &__table tbody {
      display: grid;
      gap: 0.75rem;
    }
    &__table tbody tr {
      padding: 0.875rem;
      border: 1px solid var(--color-border);
      border-radius: 0.65rem;
      background: var(--color-surface);
    }
    &__table tbody tr:nth-child(even) {
      background: var(--color-surface-secondary);
    }
    &__table tbody tr:hover,
    &__table tbody tr:focus-visible {
      background: var(--color-primary-soft);
    }
    &__table tbody tr:focus-visible {
      outline: 0.1875rem solid var(--color-focus);
      outline-offset: 0.125rem;
    }
    &__table th,
    &__table td {
      display: block;
      padding: 0;
      border: 0;
      overflow-wrap: anywhere;
    }
    &__table th {
      margin-bottom: 0.75rem;
      font-size: 1.05rem;
    }
    &__table td {
      margin-top: 0.5rem;
    }
    &__table td::before {
      content: attr(data-label);
      display: block;
      margin-bottom: 0.125rem;
      color: var(--color-text-secondary);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-semibold);
    }
  }
}
@media (max-width: 23rem) {
  .equipment-list__header-actions {
    grid-template-columns: 1fr;
  }
}
</style>
