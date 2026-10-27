<script setup lang="ts">
import BaseSpinner from "../common/spinner/BaseSpinner.vue";
import PageHero from "../common/page/PageHero.vue";
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { agenciesApi } from "../../api/agencies";
import { authSession } from "../../api/auth/session";
import { agencyScope } from "../../api/agencies/scope";
import { ApiError } from "../../api/client";
import type { AgencyDto } from "../../api/agencies/dto";
import type { InspectionDto, InspectionListItemDto } from "../../api/inspections";
import { inspectionHttpApi } from "../../api/inspections";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseInput from "../common/input/BaseInput.vue";
import BaseModal from "../common/modal/BaseModal.vue";
import InspectionDetails from "./InspectionDetails.vue";

const route = useRoute();
const router = useRouter();
const inspections = ref<InspectionListItemDto[]>([]);
const loading = ref(false);
const error = ref("");
const agencies = ref<AgencyDto[]>([]);
const isSuperAdmin = computed(() => authSession.account.value?.role === "SuperAdmin");
const ownAgencyId = computed(() => authSession.account.value?.agencyId ?? "");
const ownAgency = computed(() => agencies.value.find((item) => item.id === ownAgencyId.value));
let requestId = 0;
const commandNumber = ref("");
const rentalId = ref("");
const from = ref("");
const to = ref("");
const type = ref("all");
const status = ref("Validated");
const order = ref("newest");
const selected = ref<InspectionDto | null>(null);
const detailLoading = ref(false);
const detailError = ref("");
const selectedId = computed(() =>
  typeof route.query.inspectionId === "string" ? route.query.inspectionId : "",
);
const equipmentId = computed(() =>
  typeof route.query.equipmentId === "string" ? route.query.equipmentId : "",
);
const date = new Intl.DateTimeFormat("fr-FR", { dateStyle: "medium", timeStyle: "short" });

const filtered = computed(() =>
  [...inspections.value]
    .filter(
      (item) => isSuperAdmin.value || (ownAgencyId.value && item.agencyId === ownAgencyId.value),
    )
    .sort((a, b) =>
      order.value === "newest"
        ? (b.validatedAt ?? b.startedAt).localeCompare(a.validatedAt ?? a.startedAt)
        : (a.validatedAt ?? a.startedAt).localeCompare(b.validatedAt ?? b.startedAt),
    ),
);

const invalidDates = computed(() => Boolean(from.value && to.value && from.value > to.value));
function resetFilters(): void {
  commandNumber.value = "";
  rentalId.value = "";
  from.value = "";
  to.value = "";
  type.value = "all";
  status.value = "Validated";
  order.value = "newest";
  void load();
}

async function load(): Promise<void> {
  const currentRequest = ++requestId;
  loading.value = true;
  error.value = "";
  inspections.value = [];
  if (!isSuperAdmin.value && !ownAgencyId.value) {
    error.value = "Aucune agence n’est associée à ce compte.";
    loading.value = false;
    return;
  }
  if (invalidDates.value || (commandNumber.value && !/^\d{6}$/.test(commandNumber.value.trim()))) {
    error.value = invalidDates.value
      ? "La date de début doit précéder la date de fin."
      : "Le numéro de commande doit contenir exactement 6 chiffres.";
    loading.value = false;
    return;
  }
  try {
    const all: InspectionListItemDto[] = [];
    for (let offset = 0; ; offset += 100) {
      const page = await inspectionHttpApi.list({
        status:
          status.value === "all"
            ? undefined
            : (status.value as "Draft" | "Validated" | "Cancelled"),
        type: type.value === "all" ? undefined : (type.value as "Departure" | "Return"),
        equipmentId: equipmentId.value || undefined,
        agencyId: agencyScope.activeAgencyId.value || undefined,
        rentalId: rentalId.value.trim() || undefined,
        commandNumber: commandNumber.value.trim() || undefined,
        from: from.value ? new Date(`${from.value}T00:00:00`).toISOString() : undefined,
        to: to.value ? new Date(`${to.value}T23:59:59.999`).toISOString() : undefined,
        offset,
        limit: 100,
      });
      all.push(...page.inspections);
      if (offset + page.inspections.length >= page.total || !page.inspections.length) break;
    }
    if (currentRequest === requestId) inspections.value = all;
  } catch {
    if (currentRequest === requestId)
      error.value = "Impossible de charger les états des lieux. Veuillez réessayer.";
  } finally {
    if (currentRequest === requestId) loading.value = false;
  }
}
function show(item: InspectionListItemDto): void {
  void router.replace({ query: { ...route.query, inspectionId: item.id } });
}
function close(): void {
  selected.value = null;
  const query = { ...route.query };
  delete query.inspectionId;
  void router.replace({ query });
}
function clearEquipment(): void {
  const query = { ...route.query };
  delete query.equipmentId;
  void router.replace({ query });
}
watch(
  selectedId,
  async (id, _previous, onCleanup) => {
    selected.value = null;
    detailError.value = "";
    if (!id) {
      detailLoading.value = false;
      return;
    }
    let cancelled = false;
    onCleanup(() => {
      cancelled = true;
    });
    detailLoading.value = true;
    try {
      const detail = await inspectionHttpApi.getById(id);
      if (!cancelled) selected.value = detail;
    } catch (cause) {
      if (!cancelled)
        detailError.value =
          cause instanceof ApiError && cause.status === 404
            ? "Ressource introuvable ou inaccessible."
            : "Impossible de charger le détail de l’état des lieux.";
    } finally {
      if (!cancelled) detailLoading.value = false;
    }
  },
  { immediate: true },
);
watch([equipmentId, ownAgencyId, isSuperAdmin], () => {
  void load();
});
onMounted(() => {
  void agenciesApi
    .list()
    .then((result) => {
      agencies.value = result.agencies;
    })
    .catch(() => {});
  void load();
});
</script>

<template>
  <main class="inspection-admin">
    <PageHero
      :title="equipmentId ? 'Chronologie des états des lieux' : 'États des lieux'"
      description="Consultez les contrôles réalisés et les documents signés."
      eyebrow="Administration"
    >
      <template #actions>
        <div class="inspection-admin__header-actions">
          <BaseButton
            variant="outline"
            :disabled="loading"
            @click="load"
          >
            <template #leading><BaseIcon name="refresh" /></template>
            Actualiser
          </BaseButton>
          <router-link
            to="/admin/inspection-templates"
            class="inspection-admin__template-link"
          >
            <BaseIcon name="add" /> Modèles d’états des lieux
          </router-link>
        </div>
      </template>
    </PageHero>

    <div
      v-if="equipmentId"
      class="inspection-admin__context"
    >
      <span
        >Équipement : <strong>{{ inspections[0]?.equipmentName ?? equipmentId }}</strong></span
      >
      <BaseButton
        variant="outline"
        @click="clearEquipment"
        >Afficher tous les états des lieux</BaseButton
      >
    </div>

    <form
      class="inspection-admin__filters"
      @submit.prevent="load"
    >
      <div class="inspection-admin__filters-heading">
        <h2><BaseIcon name="filter" /> Filtres</h2>
        <span v-if="!isSuperAdmin"
          >Agence : {{ ownAgency ? `${ownAgency.code} — ${ownAgency.name}` : "mon agence" }}</span
        >
      </div>
      <div class="inspection-admin__fields">
        <BaseInput
          id="inspection-command-filter"
          v-model="commandNumber"
          label="Numéro de commande"
          inputmode="numeric"
          :maxlength="6"
          placeholder="6 chiffres"
          :error="
            commandNumber && !/^\d{6}$/.test(commandNumber.trim()) && error
              ? 'Le numéro de commande doit contenir exactement 6 chiffres.'
              : undefined
          "
        />
        <BaseInput
          id="inspection-rental-filter"
          v-model="rentalId"
          label="Identifiant de location"
          placeholder="UUID de la location"
        />
        <BaseInput
          id="inspection-date-from"
          v-model="from"
          label="Du"
          type="date"
          :error="
            invalidDates && error ? 'La date de début doit précéder la date de fin.' : undefined
          "
        />
        <BaseInput
          id="inspection-date-to"
          v-model="to"
          label="Au"
          type="date"
        />
        <label
          >Type
          <select v-model="type">
            <option value="all">Tous</option>
            <option value="Departure">Départs</option>
            <option value="Return">Retours</option>
          </select>
        </label>
        <label
          >Statut
          <select v-model="status">
            <option value="Validated">Validés</option>
            <option value="Draft">Brouillons</option>
            <option value="Cancelled">Annulés</option>
            <option value="all">Tous</option>
          </select>
        </label>
      </div>
      <div class="inspection-admin__filter-actions">
        <BaseButton
          type="submit"
          :disabled="loading"
          >Rechercher</BaseButton
        >
        <BaseButton
          variant="outline"
          :disabled="loading"
          @click="resetFilters"
          >Réinitialiser</BaseButton
        >
      </div>
    </form>

    <p
      v-if="error"
      class="inspection-admin__message inspection-admin__message--error"
      role="alert"
    >
      {{ error }}
      <BaseButton
        variant="outline"
        @click="load"
        >Réessayer</BaseButton
      >
    </p>
    <p
      v-if="loading"
      class="inspection-admin__message"
      role="status"
    >
      <BaseSpinner size="medium" label="Chargement des états des lieux" />
    </p>
    <section
      v-else-if="!error"
      class="inspection-admin__results"
      aria-label="Résultats des états des lieux"
    >
      <div class="inspection-admin__results-header">
        <h2>Résultats</h2>
        <p role="status">
          {{ filtered.length }} état{{ filtered.length > 1 ? "s" : "" }} des lieux
        </p>
      </div>
      <p
        v-if="!filtered.length"
        class="inspection-admin__empty"
      >
        Aucun état des lieux ne correspond à ces critères.
      </p>
      <div
        v-else
        class="app-table-scroll"
      >
        <div class="app-table__mobile-sort">
          <label for="inspection-sort">Trier par date</label>
          <select
            id="inspection-sort"
            v-model="order"
          >
            <option value="newest">Plus récents</option>
            <option value="oldest">Plus anciens</option>
          </select>
        </div>
        <table class="app-table inspection-admin__table">
          <thead>
            <tr>
              <th scope="col">Équipement</th>
              <th scope="col">Type</th>
              <th scope="col">Client et commande</th>
              <th
                v-if="isSuperAdmin && !agencyScope.activeAgencyId.value"
                scope="col"
              >
                Agence
              </th>
              <th scope="col">Opérateur</th>
              <th
                scope="col"
                :aria-sort="order === 'newest' ? 'descending' : 'ascending'"
              >
                <button
                  type="button"
                  class="app-table__sort-button"
                  @click="order = order === 'newest' ? 'oldest' : 'newest'"
                >
                  Date <span aria-hidden="true">{{ order === "newest" ? "▼" : "▲" }}</span>
                </button>
              </th>
              <th scope="col">Statut</th>
              <th scope="col"><span class="visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in filtered"
              :key="item.id"
            >
              <td data-label="Équipement">
                <strong>{{ item.equipmentName }}</strong>
                <small class="inspection-admin__subtext">{{ item.serialNumber }}</small>
              </td>
              <td data-label="Type">{{ item.type === "Departure" ? "Départ" : "Retour" }}</td>
              <td data-label="Client et commande">
                {{ item.clientName }}
                <small class="inspection-admin__subtext"
                  >{{ item.agencyCode }}/{{ item.commandNumber }}</small
                >
              </td>
              <td
                v-if="isSuperAdmin && !agencyScope.activeAgencyId.value"
                data-label="Agence"
              >
                {{
                  agencies.find((agency) => agency.id === item.agencyId)?.name ?? item.agencyCode
                }}
              </td>
              <td data-label="Opérateur">{{ item.operatorFirstName }}</td>
              <td data-label="Date">
                {{ date.format(new Date(item.validatedAt ?? item.startedAt)) }}
              </td>
              <td data-label="Statut">
                <span
                  class="inspection-admin__status"
                  :class="`inspection-admin__status--${item.status.toLowerCase()}`"
                >
                  {{
                    item.status === "Validated"
                      ? "Validé"
                      : item.status === "Cancelled"
                        ? "Annulé"
                        : "Brouillon"
                  }}
                </span>
              </td>
              <td class="inspection-admin__action">
                <BaseButton
                  variant="outline"
                  size="small"
                  @click="show(item)"
                >
                  <template #leading
                    ><BaseIcon
                      name="view"
                      :size="18"
                  /></template>
                  Consulter
                  <span class="visually-hidden"
                    >{{ item.equipmentName }},
                    {{ item.type === "Departure" ? "départ" : "retour" }}</span
                  >
                </BaseButton>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <BaseModal
      :open="Boolean(selectedId)"
      title="Détail de l’état des lieux"
      @close="close"
    >
      <p
        v-if="detailLoading"
        role="status"
      >
        <BaseSpinner size="medium" label="Chargement du détail" />
      </p>
      <p
        v-else-if="detailError"
        role="alert"
      >
        {{ detailError }}
        <BaseButton
          variant="outline"
          @click="close"
          >Fermer</BaseButton
        >
      </p>
      <InspectionDetails
        v-if="selected"
        :inspection="selected"
      />
    </BaseModal>
  </main>
</template>

<style scoped lang="scss">
.inspection-admin {
  display: grid;
  gap: 1.25rem;
  max-width: 82rem;
  min-width: 0;
  margin-inline: auto;
}
.inspection-admin__header,
.inspection-admin__header-actions,
.inspection-admin__context,
.inspection-admin__results-header,
.inspection-admin__filter-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  flex-wrap: wrap;
}
.inspection-admin__header h1,
.inspection-admin__header p,
.inspection-admin__results-header h2,
.inspection-admin__results-header p {
  margin: 0;
}
.inspection-admin__header p,
.inspection-admin__subtext {
  color: var(--color-text-secondary);
}
.inspection-admin__template-link {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  min-height: 2.75rem;
  box-sizing: border-box;
  padding: 0.5rem 0.75rem;
  border-radius: 0.5rem;
  background: var(--color-secondary-soft);
  color: var(--color-secondary);
  font-weight: var(--font-weight-semibold);
  text-decoration: none;
}
.inspection-admin__template-link:hover {
  background: var(--color-secondary-soft-hover);
}
.inspection-admin__context,
.inspection-admin__filters,
.inspection-admin__results {
  min-width: 0;
  padding: 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface);
}
.inspection-admin__context {
  background: var(--color-secondary-soft);
}
.inspection-admin__filters {
  display: grid;
  gap: 1rem;
}
.inspection-admin__filters-heading {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.inspection-admin__filters-heading h2 {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0;
  font-size: 1.1rem;
}
.inspection-admin__filters-heading span {
  color: var(--color-text-secondary);
}
.inspection-admin__fields {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 12rem), 1fr));
  gap: 0.75rem;
}
.inspection-admin__fields label {
  display: grid;
  gap: 0.35rem;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}
.inspection-admin__fields input,
.inspection-admin__fields select {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  min-height: 2.75rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-input-background);
  color: var(--color-text);
  font: inherit;
}
.inspection-admin__fields :is(input, select):focus-visible,
.inspection-admin__template-link:focus-visible {
  outline: 3px solid var(--color-focus);
  outline-offset: 2px;
}
.inspection-admin__filter-actions {
  justify-content: flex-end;
}
.inspection-admin__results {
  padding: 0;
  overflow: hidden;
}
.inspection-admin__results-header {
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--color-border);
}
.inspection-admin__results-header h2 {
  font-size: 1.1rem;
}
.inspection-admin__results-header p {
  color: var(--color-text-secondary);
}
.inspection-admin__empty,
.inspection-admin__message {
  margin: 0;
  padding: 1.5rem;
}
.inspection-admin__message--error {
  color: var(--color-danger);
}
.inspection-admin__table {
  min-width: 64rem;
}
.inspection-admin__table td {
  overflow-wrap: anywhere;
}
.inspection-admin__subtext {
  display: block;
  margin-top: 0.2rem;
  font-size: var(--font-size-sm);
}
.inspection-admin__status {
  display: inline-flex;
  padding: 0.2rem 0.55rem;
  border-radius: 2rem;
  background: var(--color-surface-secondary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}
.inspection-admin__status--validated {
  background: var(--color-success-soft, var(--color-surface-secondary));
  color: var(--color-success);
}
.inspection-admin__status--cancelled {
  color: var(--color-danger);
}
.inspection-admin__status--draft {
  background: var(--color-secondary-soft);
  color: var(--color-secondary);
}
.inspection-admin__action {
  white-space: nowrap;
}
.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip-path: inset(50%);
  white-space: nowrap;
}
@media (max-width: 48rem) {
  .inspection-admin__table {
    min-width: 0;
  }
  .inspection-admin__results {
    overflow: visible;
  }
  .inspection-admin__results-header {
    border-bottom: 0;
  }
  .inspection-admin__table tbody {
    padding: 0 0.75rem 0.75rem;
  }
  .inspection-admin__action {
    margin-top: 0.75rem;
  }
}
@media (max-width: 36rem) {
  .inspection-admin__header-actions,
  .inspection-admin__header-actions > *,
  .inspection-admin__filter-actions > * {
    width: 100%;
  }
  .inspection-admin__filter-actions {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
