<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { agenciesApi } from "../../api/agencies";
import type { AgencyDto } from "../../api/agencies/dto";
import type { InspectionDto, InspectionListItemDto } from "../../api/inspections";
import { inspectionHttpApi } from "../../api/inspections";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseModal from "../common/modal/BaseModal.vue";
import InspectionDetails from "./InspectionDetails.vue";

const route = useRoute();
const router = useRouter();
const inspections = ref<InspectionListItemDto[]>([]);
const loading = ref(false);
const error = ref("");
const agencies = ref<AgencyDto[]>([]);
const commandNumber = ref("");
const agencyId = ref("");
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
  [...inspections.value].sort((a, b) =>
    order.value === "newest"
      ? (b.validatedAt ?? b.startedAt).localeCompare(a.validatedAt ?? a.startedAt)
      : (a.validatedAt ?? a.startedAt).localeCompare(b.validatedAt ?? b.startedAt),
  ),
);

const invalidDates = computed(() => Boolean(from.value && to.value && from.value > to.value));
function resetFilters(): void {
  commandNumber.value = "";
  agencyId.value = "";
  rentalId.value = "";
  from.value = "";
  to.value = "";
  type.value = "all";
  status.value = "Validated";
  order.value = "newest";
  void load();
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = "";
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
        agencyId: agencyId.value || undefined,
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
    inspections.value = all;
  } catch {
    error.value = "Impossible de charger les états des lieux. Veuillez réessayer.";
  } finally {
    loading.value = false;
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
    } catch {
      if (!cancelled) detailError.value = "Impossible de charger le détail de l’état des lieux.";
    } finally {
      if (!cancelled) detailLoading.value = false;
    }
  },
  { immediate: true },
);
watch(equipmentId, () => {
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
    <header>
      <div>
        <h1>{{ equipmentId ? "Chronologie des états des lieux" : "États des lieux" }}</h1>
        <p>Consultez les contrôles réalisés et les documents signés.</p>
      </div>
      <div class="header__tools">
        <button
          type="button"
          :disabled="loading"
          @click="load"
        >
          <BaseIcon name="refresh" />
        </button>
      </div>
      <router-link
        to="/admin/inspection-templates"
        class="inspection-admin__create-button"
      >
        <BaseButton variant="primary">
          <BaseIcon name="add" />
          <span> Créer un modèle d'état des lieux </span>
        </BaseButton>
      </router-link>
    </header>
    <p
      v-if="equipmentId"
      class="inspection-admin__context"
    >
      Équipement : {{ inspections[0]?.equipmentName ?? equipmentId }}
      <button
        type="button"
        @click="clearEquipment"
      >
        Afficher tous les états des lieux
      </button>
    </p>
    <p
      v-if="error"
      role="alert"
    >
      {{ error }}
      <button
        type="button"
        @click="load"
      >
        Réessayer
      </button>
    </p>
    <p
      v-if="loading"
      role="status"
    >
      Chargement des états des lieux…
    </p>
    <template v-else-if="!error">
      <div class="inspection-admin__filters">
        <label
          >Numéro de commande<input
            v-model="commandNumber"
            inputmode="numeric"
            maxlength="6"
            placeholder="6 chiffres"
        /></label>
        <label
          >Agence<select v-model="agencyId">
            <option value="">Toutes les agences</option>
            <option
              v-for="agency in agencies"
              :key="agency.id"
              :value="agency.id"
            >
              {{ agency.code }} — {{ agency.name }}
            </option>
          </select></label
        >
        <label
          >Identifiant de location<input
            v-model="rentalId"
            type="text"
            placeholder="UUID de la location"
        /></label>
        <label
          >Du<input
            v-model="from"
            type="date"
        /></label>
        <label
          >Au<input
            v-model="to"
            type="date"
        /></label>
        <label
          >Type<select v-model="type">
            <option value="all">Tous</option>
            <option value="Departure">Départs</option>
            <option value="Return">Retours</option>
          </select></label
        >
        <label
          >Statut<select v-model="status">
            <option value="Validated">Validés</option>
            <option value="Draft">Brouillons</option>
            <option value="Cancelled">Annulés</option>
            <option value="all">Tous</option>
          </select></label
        >
        <label
          >Ordre<select v-model="order">
            <option value="newest">Plus récents</option>
            <option value="oldest">Plus anciens</option>
          </select></label
        >
        <div class="inspection-admin__filter-actions">
          <button
            type="button"
            :disabled="loading"
            @click="load"
          >
            Rechercher</button
          ><button
            type="button"
            :disabled="loading"
            @click="resetFilters"
          >
            Réinitialiser
          </button>
        </div>
      </div>
      <p role="status">{{ filtered.length }} état(s) des lieux</p>
      <p v-if="!filtered.length">Aucun état des lieux ne correspond à ces critères.</p>
      <ul
        v-else
        class="inspection-admin__list"
      >
        <li
          v-for="item in filtered"
          :key="item.id"
        >
          <div>
            <strong>{{ item.equipmentName }}</strong>
            <span
              >{{ item.type === "Departure" ? "Départ" : "Retour" }} -
              {{
                item.status === "Validated"
                  ? "Validé"
                  : item.status === "Cancelled"
                    ? "Annulé"
                    : "Brouillon"
              }}
              - {{ date.format(new Date(item.validatedAt ?? item.startedAt)) }}</span
            >
            <span>Opérateur : {{ item.operatorFirstName }} - Client : {{ item.clientName }}</span>
          </div>
          <button
            type="button"
            @click="show(item)"
          >
            Consulter
          </button>
        </li>
      </ul>
    </template>
    <BaseModal
      :open="Boolean(selectedId)"
      title="Détail de l’état des lieux"
      @close="close"
    >
      <p
        v-if="detailLoading"
        role="status"
      >
        Chargement du détail…
      </p>
      <p
        v-else-if="detailError"
        role="alert"
      >
        {{ detailError }}
        <button
          type="button"
          @click="close"
        >
          Fermer
        </button>
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
  max-width: 78rem;
  min-width: 0;
  margin-inline: auto;
}
header {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;

  .header__tools {
    display: flex;
    gap: 0.5rem;
    margin-left: auto;

    button {
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
  }
}
h1,
header p {
  margin: 0;
}
.inspection-admin__context {
  padding: 0.8rem;
  background: var(--color-surface-secondary);
  border-radius: 0.5rem;
}
.inspection-admin__filters {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
  gap: 0.75rem;
}
.inspection-admin__filters label {
  display: grid;
  gap: 0.35rem;
}
.inspection-admin__filter-actions {
  display: flex;
  align-items: end;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.inspection-admin__filters input,
.inspection-admin__filters select {
  min-height: 2.75rem;
  padding: 0.5rem;
  color: var(--color-text);
  background: var(--color-input-background);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
}
.inspection-admin__list {
  display: grid;
  gap: 0.6rem;
  list-style: none;
  padding: 0;
}
.inspection-admin__list li {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 0.85rem;
  border: 1px solid var(--color-border);
  border-radius: 0.6rem;
  background: var(--color-surface);
}
.inspection-admin__list li div {
  display: grid;
  gap: 0.3rem;
  min-width: 0;
  overflow-wrap: anywhere;
}
.inspection-admin__list span {
  color: var(--color-text-secondary);
}
button {
  min-height: 2.75rem;
  padding: 0.45rem 0.7rem;
  color: var(--color-text);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  cursor: pointer;
}
@media (max-width: 48rem) {
  .inspection-admin__filters input,
  .inspection-admin__filters select,
  .inspection-admin__filters button {
    min-height: 3rem;
  }
}
button:focus-visible {
  outline: 3px solid var(--color-focus);
}
@media (max-width: 40rem) {
  .inspection-admin__filters {
    grid-template-columns: 1fr;
  }
  .inspection-admin__list li {
    align-items: start;
    flex-direction: column;
  }
}
</style>
