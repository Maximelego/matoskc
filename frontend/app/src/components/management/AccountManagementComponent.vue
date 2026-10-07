<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { accountsApi } from "../../api/accounts";
import type { AccountDto, AccountRole, CreateAccountDto } from "../../api/accounts/dto";
import { agenciesApi } from "../../api/agencies";
import type { AgencyDto } from "../../api/agencies/dto";
import BaseButton from "../common/button/BaseButton.vue";
import BaseModal from "../common/modal/BaseModal.vue";
import BaseSpinner from "../common/spinner/BaseSpinner.vue";

const roleLabels: Record<AccountRole, string> = {
  SuperAdmin: "Super administrateur",
  Admin: "Administrateur",
  Agency: "Agence",
};
const accounts = ref<AccountDto[]>([]);
const agencies = ref<AgencyDto[]>([]);
const loading = ref(false);
const busy = ref(false);
const error = ref("");
const formError = ref("");
const notice = ref("");
const nameFilter = ref("");
const emailFilter = ref("");
const roleFilter = ref<"all" | AccountRole>("all");
const agencyFilter = ref("all");
const statusFilter = ref<"all" | "active" | "inactive">("all");
type SortKey = "displayName" | "email" | "role" | "agencyId" | "isActive";
const sortKey = ref<SortKey>("displayName");
const sortDirection = ref<"asc" | "desc">("asc");
const modalOpen = ref(false);
const modalContentVisible = ref(false);
const editingId = ref<string | null>(null);
const displayName = ref("");
const email = ref("");
const password = ref("");
const role = ref<AccountRole>("Admin");
const agencyId = ref("");
const form = ref<HTMLFormElement | null>(null);
const collator = new Intl.Collator("fr", { sensitivity: "base", numeric: true });
const agencyNames = computed(
  () => new Map(agencies.value.map((agency) => [agency.id, agency.name])),
);
function sortValue(account: AccountDto): string {
  switch (sortKey.value) {
    case "role":
      return roleLabels[account.role];
    case "agencyId":
      return account.agencyId ? (agencyNames.value.get(account.agencyId) ?? account.agencyId) : "";
    case "isActive":
      return account.isActive ? "Actif" : "Inactif";
    default:
      return account[sortKey.value] ?? "";
  }
}
function toggleSort(key: SortKey): void {
  if (sortKey.value === key) sortDirection.value = sortDirection.value === "asc" ? "desc" : "asc";
  else {
    sortKey.value = key;
    sortDirection.value = "asc";
  }
}
function ariaSort(key: SortKey): "none" | "ascending" | "descending" {
  return sortKey.value === key
    ? sortDirection.value === "asc"
      ? "ascending"
      : "descending"
    : "none";
}
const filtered = computed(() =>
  accounts.value
    .filter((account) => {
      const name = nameFilter.value.trim().toLocaleLowerCase("fr");
      const email = emailFilter.value.trim().toLocaleLowerCase("fr");
      return (
        (roleFilter.value === "all" || account.role === roleFilter.value) &&
        (agencyFilter.value === "all" || account.agencyId === agencyFilter.value) &&
        (statusFilter.value === "all" || account.isActive === (statusFilter.value === "active")) &&
        (!name || account.displayName.toLocaleLowerCase("fr").includes(name)) &&
        (!email || (account.email ?? "").toLocaleLowerCase("fr").includes(email))
      );
    })
    .sort(
      (a, b) =>
        (sortDirection.value === "asc" ? 1 : -1) * collator.compare(sortValue(a), sortValue(b)) ||
        collator.compare(a.id, b.id),
    ),
);

async function load(): Promise<void> {
  loading.value = true;
  error.value = "";
  try {
    const [accountResult, agencyResult] = await Promise.all([
      accountsApi.list(),
      agenciesApi.list(),
    ]);
    accounts.value = accountResult.accounts;
    agencies.value = agencyResult.agencies;
  } catch {
    error.value = "Impossible de charger les comptes. Veuillez réessayer.";
  } finally {
    loading.value = false;
  }
}
function openForm(account?: AccountDto): void {
  editingId.value = account?.id ?? null;
  displayName.value = account?.displayName ?? "";
  email.value = account?.email ?? "";
  role.value = account?.role ?? "Admin";
  agencyId.value = account?.agencyId ?? "";
  password.value = "";
  formError.value = "";
  modalContentVisible.value = true;
  modalOpen.value = true;
}
function closeForm(): void {
  if (!busy.value) modalOpen.value = false;
}
function clearForm(): void {
  if (!modalOpen.value) modalContentVisible.value = false;
}
async function save(): Promise<void> {
  if (busy.value || !displayName.value.trim()) return;
  busy.value = true;
  formError.value = "";
  try {
    if (editingId.value) {
      await accountsApi.update(editingId.value, {
        displayName: displayName.value.trim(),
        email: role.value === "Agency" ? null : email.value.trim(),
      });
    } else {
      const dto: CreateAccountDto = {
        displayName: displayName.value.trim(),
        email: role.value === "Agency" ? null : email.value.trim(),
        password: password.value,
        role: role.value,
        agencyId: role.value === "SuperAdmin" ? null : agencyId.value,
      };
      await accountsApi.create(dto);
    }
    password.value = "";
    notice.value = editingId.value ? "Le compte a été modifié." : "Le compte a été créé.";
    modalOpen.value = false;
    await load();
  } catch (cause) {
    formError.value =
      cause instanceof Error ? cause.message : "Impossible d’enregistrer le compte.";
  } finally {
    busy.value = false;
  }
}
async function toggleActive(account: AccountDto): Promise<void> {
  busy.value = true;
  error.value = "";
  try {
    await accountsApi.setActive(account.id, !account.isActive);
    notice.value = account.isActive ? "Le compte a été désactivé." : "Le compte a été activé.";
    await load();
  } catch {
    error.value = "Impossible de modifier l’état du compte.";
  } finally {
    busy.value = false;
  }
}
onMounted(() => {
  void load();
});
</script>

<template>
  <section
    class="management"
    aria-labelledby="accounts-title"
  >
    <header class="management__header">
      <div>
        <h1 id="accounts-title">Gestion des utilisateurs</h1>
        <p class="management__subtitle">Consultez les comptes et gérez leurs accès.</p>
      </div>
      <div class="management__actions">
        <BaseButton
          variant="outline"
          size="small"
          :loading="loading"
          @click="load"
          >Actualiser</BaseButton
        >
        <BaseButton
          size="small"
          @click="openForm()"
          >Créer un compte</BaseButton
        >
      </div>
    </header>

    <p
      v-if="notice"
      class="management__notice"
      role="status"
    >
      {{ notice }}
    </p>
    <div
      v-if="error"
      class="management__error"
      role="alert"
    >
      <p>{{ error }}</p>
      <BaseButton
        variant="outline"
        size="small"
        @click="load"
        >Réessayer</BaseButton
      >
    </div>

    <div class="management__panel">
      <div class="management__panel-head">
        <h2>Comptes</h2>
        <span class="management__count"
          >{{ filtered.length }} résultat{{ filtered.length > 1 ? "s" : "" }}</span
        >
      </div>
      <div class="management__toolbar">
        <label for="account-name-filter">Nom</label>
        <input
          id="account-name-filter"
          v-model="nameFilter"
          class="management__input management__search"
          type="search"
          placeholder="Nom affiché"
        />
        <label for="account-email-filter">Courriel</label>
        <input
          id="account-email-filter"
          v-model="emailFilter"
          class="management__input management__search"
          type="search"
          placeholder="Adresse électronique"
        />
        <label for="account-role">Rôle</label>
        <select
          id="account-role"
          v-model="roleFilter"
          class="management__select"
        >
          <option value="all">Tous</option>
          <option value="SuperAdmin">Super administrateur</option>
          <option value="Admin">Administrateur</option>
          <option value="Agency">Agence</option>
        </select>
        <label for="account-agency-filter">Agence</label>
        <select
          id="account-agency-filter"
          v-model="agencyFilter"
          class="management__select"
        >
          <option value="all">Toutes</option>
          <option
            v-for="agency in agencies"
            :key="agency.id"
            :value="agency.id"
          >
            {{ agency.name }} ({{ agency.code }})
          </option>
        </select>
        <label for="account-status-filter">État</label>
        <select
          id="account-status-filter"
          v-model="statusFilter"
          class="management__select"
        >
          <option value="all">Tous</option>
          <option value="active">Actifs</option>
          <option value="inactive">Inactifs</option>
        </select>
      </div>
      <div
        v-if="loading"
        class="management__state"
        role="status"
      >
        <BaseSpinner size="medium" /> Chargement des comptes…
      </div>
      <p
        v-else-if="!error && filtered.length === 0"
        class="management__state"
      >
        Aucun compte ne correspond à la recherche.
      </p>
      <div
        v-else-if="!error"
        class="app-table-scroll"
      >
        <div class="app-table__mobile-sort">
          <label for="account-sort">Trier par</label>
          <select
            id="account-sort"
            v-model="sortKey"
            class="management__select"
          >
            <option value="displayName">Nom affiché</option>
            <option value="email">Courriel</option>
            <option value="role">Rôle</option>
            <option value="agencyId">Agence</option>
            <option value="isActive">État</option>
          </select>
          <button
            type="button"
            class="app-table__sort-direction"
            :aria-label="
              sortDirection === 'asc' ? 'Tri croissant, inverser' : 'Tri décroissant, inverser'
            "
            @click="sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'"
          >
            {{ sortDirection === "asc" ? "↑" : "↓" }}
          </button>
        </div>
        <table class="app-table">
          <thead>
            <tr>
              <th
                v-for="column in [
                  { key: 'displayName', label: 'Nom affiché' },
                  { key: 'email', label: 'Courriel' },
                  { key: 'role', label: 'Rôle' },
                  { key: 'agencyId', label: 'Agence' },
                  { key: 'isActive', label: 'État' },
                ] as const"
                :key="column.key"
                scope="col"
                :aria-sort="ariaSort(column.key)"
              >
                <button
                  type="button"
                  class="app-table__sort-button"
                  @click="toggleSort(column.key)"
                >
                  {{ column.label }}
                  <span aria-hidden="true">{{
                    sortKey === column.key ? (sortDirection === "asc" ? "▲" : "▼") : "↕"
                  }}</span>
                </button>
              </th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="account in filtered"
              :key="account.id"
            >
              <th scope="row">{{ account.displayName }}</th>
              <td data-label="Courriel">{{ account.email ?? "—" }}</td>
              <td data-label="Rôle">{{ roleLabels[account.role] }}</td>
              <td data-label="Agence">
                {{
                  account.agencyId ? (agencyNames.get(account.agencyId) ?? account.agencyId) : "—"
                }}
              </td>
              <td data-label="État">
                <span
                  class="management__badge"
                  :class="{ 'management__badge--active': account.isActive }"
                  >{{ account.isActive ? "Actif" : "Inactif" }}</span
                >
              </td>
              <td class="management__actions-cell">
                <div class="management__row-actions">
                  <BaseButton
                    variant="outline"
                    size="small"
                    :disabled="busy"
                    @click="openForm(account)"
                    >Modifier</BaseButton
                  >
                  <BaseButton
                    :variant="account.isActive ? 'danger' : 'secondary'"
                    size="small"
                    :disabled="busy"
                    @click="toggleActive(account)"
                    >{{ account.isActive ? "Désactiver" : "Activer" }}</BaseButton
                  >
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <BaseModal
      :open="modalOpen"
      :busy="busy"
      :title="editingId ? 'Modifier un compte' : 'Créer un compte'"
      @close="closeForm"
      @closed="clearForm"
    >
      <form
        v-if="modalContentVisible"
        ref="form"
        class="management__form"
        @submit.prevent="save"
      >
        <div class="management__field">
          <label for="account-name">Nom affiché</label>
          <input
            id="account-name"
            v-model="displayName"
            class="management__input"
            required
            maxlength="120"
            :disabled="busy"
          />
        </div>
        <div class="management__field">
          <label for="account-form-role">Rôle</label>
          <select
            id="account-form-role"
            v-model="role"
            class="management__select"
            :disabled="busy || !!editingId"
          >
            <option value="Agency">Agence</option>
            <option value="Admin">Administrateur</option>
            <option value="SuperAdmin">Super administrateur</option>
          </select>
        </div>
        <div
          v-if="role !== 'SuperAdmin'"
          class="management__field"
        >
          <label for="account-agency">Agence</label>
          <select
            id="account-agency"
            v-model="agencyId"
            class="management__select"
            required
            :disabled="busy || !!editingId"
          >
            <option
              value=""
              disabled
            >
              Choisir une agence
            </option>
            <option
              v-for="agency in agencies"
              :key="agency.id"
              :value="agency.id"
              :disabled="!agency.isActive"
            >
              {{ agency.name }} ({{ agency.code }})
            </option>
          </select>
        </div>
        <div
          v-if="role !== 'Agency'"
          class="management__field"
        >
          <label for="account-email">Adresse électronique</label>
          <input
            id="account-email"
            v-model="email"
            class="management__input"
            type="email"
            required
            autocomplete="off"
            :disabled="busy"
          />
        </div>
        <div
          v-if="!editingId"
          class="management__field"
        >
          <label for="account-password">Mot de passe initial</label>
          <input
            id="account-password"
            v-model="password"
            class="management__input"
            type="password"
            required
            autocomplete="new-password"
            :disabled="busy"
          />
          <p class="management__hint">Le mot de passe n’est jamais affiché dans la liste.</p>
        </div>
        <p
          v-if="formError"
          class="management__error"
          role="alert"
        >
          {{ formError }}
        </p>
        <div class="management__form-actions">
          <BaseButton
            variant="outline"
            :disabled="busy"
            @click="closeForm"
            >Annuler</BaseButton
          >
          <BaseButton
            :loading="busy"
            @click="form?.requestSubmit()"
            >Enregistrer</BaseButton
          >
        </div>
      </form>
    </BaseModal>
  </section>
</template>

<style scoped lang="scss">
.management {
  display: grid;
  gap: 1.25rem;
  min-width: 0;
  color: var(--color-text);
  &__header,
  &__actions,
  &__toolbar,
  &__panel-head,
  &__row-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }
  &__header,
  &__panel-head {
    justify-content: space-between;
    flex-wrap: wrap;
  }
  &__header h1,
  &__panel-head h2 {
    margin: 0;
  }
  &__subtitle,
  &__count {
    color: var(--color-text-secondary);
  }
  &__subtitle {
    margin: 0.35rem 0 0;
  }
  &__panel {
    border: 1px solid var(--color-border);
    border-radius: 0.75rem;
    background: var(--color-surface);
    overflow: hidden;
  }
  &__panel-head {
    padding: 1rem 1.25rem;
    border-bottom: 1px solid var(--color-border);
  }
  &__panel-head h2 {
    font-size: 1rem;
  }
  &__toolbar {
    flex-wrap: wrap;
    padding: 1rem 1.25rem;
    border-bottom: 1px solid var(--color-border);
  }
  &__input,
  &__select {
    box-sizing: border-box;
    min-height: 2.75rem;
    padding: 0.625rem 0.75rem;
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    background: var(--color-surface);
    color: var(--color-text);
    font: inherit;
    &:focus-visible {
      outline: 0.1875rem solid var(--color-focus);
      outline-offset: 0.125rem;
    }
  }
  &__search {
    flex: 1 1 15rem;
    min-width: 0;
  }
  &__state {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 0.75rem;
    padding: 2.5rem 1rem;
    color: var(--color-text-secondary);
  }
  &__error {
    padding: 1rem;
    border: 1px solid var(--color-danger);
    border-radius: 0.5rem;
    color: var(--color-danger);
  }
  &__notice {
    margin: 0;
    padding: 0.75rem 1rem;
    border-radius: 0.5rem;
    background: var(--color-primary-soft);
  }
  &__row-actions {
    justify-content: flex-end;
  }
  &__badge {
    display: inline-block;
    padding: 0.25rem 0.6rem;
    border-radius: 999px;
    background: var(--color-surface-secondary);
    white-space: nowrap;
  }
  &__badge--active {
    color: var(--color-success);
    background: var(--color-success-soft);
  }
  &__form {
    display: grid;
    gap: 1rem;
  }
  &__field {
    display: grid;
    gap: 0.4rem;
  }
  &__field label {
    font-weight: var(--font-weight-semibold);
  }
  &__field input,
  &__field select {
    width: 100%;
  }
  &__hint {
    margin: 0;
    color: var(--color-text-secondary);
    font-size: var(--font-size-sm);
  }
  &__form-actions {
    display: flex;
    justify-content: flex-end;
    flex-wrap: wrap;
    gap: 0.75rem;
    margin-top: 0.5rem;
  }
}
@media (max-width: 42rem) {
  .management {
    &__header {
      align-items: stretch;
    }
    &__actions {
      width: 100%;
      flex-wrap: wrap;
    }
    &__actions > * {
      flex: 1 1 auto;
    }
    &__toolbar {
      align-items: stretch;
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      gap: 0.45rem;
    }
    &__toolbar label:not(:first-child) {
      margin-top: 0.4rem;
    }
    &__row-actions {
      justify-content: flex-start;
      padding-top: 0.3rem;
      flex-wrap: wrap;
    }
  }
}
</style>
