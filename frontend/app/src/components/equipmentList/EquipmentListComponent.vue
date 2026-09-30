<script setup lang="ts">
import { onMounted, ref } from "vue";
import { ApiError } from "../../api/client";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseModal from "../common/modal/BaseModal.vue";
import BaseSpinner from "../common/spinner/BaseSpinner.vue";
import EquipmentCreateForm from "../equipmentCreate/EquipmentCreateForm.vue";

const emit = defineEmits<{
  view: [equipment: EquipmentDto];
  edit: [equipment: EquipmentDto];
}>();

const equipments = ref<EquipmentDto[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const notice = ref("");

const createOpen = ref(false);
const createBusy = ref(false);

async function loadEquipments(): Promise<void> {
  loading.value = true;
  error.value = null;

  try {
    const result = await equipmentsApi.list();
    equipments.value = result.equipments;
  } catch (cause: unknown) {
    if (cause instanceof ApiError) {
      if (cause.status === 401) {
        error.value = "Veuillez vous connecter pour consulter les équipements.";
      } else if (cause.status === 403) {
        error.value = "Vous ne disposez pas des droits nécessaires pour consulter les équipements.";
      } else if (cause.status >= 500) {
        error.value =
          "Le serveur rencontre un problème. Veuillez réessayer dans quelques instants.";
      } else {
        error.value = cause.message;
      }
    } else if (cause instanceof TypeError) {
      error.value =
        "Impossible de joindre l’API. Veuillez vérifier votre connexion et la disponibilité du serveur.";
    } else {
      error.value = "Impossible de charger les équipements.";
    }
  } finally {
    loading.value = false;
  }
}

async function handleCreated(): Promise<void> {
  createOpen.value = false;
  notice.value = "L’équipement a été créé.";
  await loadEquipments();
}

onMounted(loadEquipments);
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
          @click="createOpen = true"
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
          {{ equipments.length }} équipement{{ equipments.length > 1 ? "s" : "" }}
        </span>
      </div>

      <div
        v-if="loading && equipments.length === 0"
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
        Aucun équipement à afficher.
      </p>

      <div
        v-if="equipments.length > 0"
        class="equipment-list__table-scroll"
      >
        <table class="equipment-list__table">
          <thead>
            <tr>
              <th scope="col">Équipement</th>
              <th scope="col">Numéro de série</th>
              <th scope="col">Catégorie</th>
              <th
                scope="col"
                class="equipment-list__actions-heading"
              >
                Actions
              </th>
            </tr>
          </thead>

          <tbody>
            <tr
              v-for="equipment in equipments"
              :key="equipment.id"
            >
              <th
                scope="row"
                class="equipment-list__name"
              >
                {{ equipment.name }}
              </th>

              <td class="equipment-list__serial">
                {{ equipment.serialNumber || "—" }}
              </td>

              <td class="equipment-list__category">
                {{ equipment.equipmentCategoryId }}
              </td>

              <td class="equipment-list__actions-cell">
                <div class="equipment-list__row-actions">
                  <button
                    type="button"
                    class="equipment-list__icon-button"
                    :aria-label="`Consulter ${equipment.name}`"
                    :title="`Consulter ${equipment.name}`"
                    :disabled="loading"
                    @click="emit('view', equipment)"
                  >
                    <BaseIcon
                      name="view"
                      :size="18"
                    />
                  </button>

                  <button
                    type="button"
                    class="equipment-list__icon-button"
                    :aria-label="`Modifier ${equipment.name}`"
                    :title="`Modifier ${equipment.name}`"
                    :disabled="loading"
                    @click="emit('edit', equipment)"
                  >
                    <BaseIcon
                      name="edit"
                      :size="18"
                    />
                  </button>
                </div>
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
      @close="createOpen = false"
    >
      <EquipmentCreateForm
        v-if="createOpen"
        @cancel="createOpen = false"
        @busy="createBusy = $event"
        @created="handleCreated"
      />
    </BaseModal>
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
    color: var(--color-text);
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

  &__table-scroll {
    overflow-x: auto;
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

  &__table thead th {
    color: var(--color-text-secondary);
    font-size: var(--font-size-sm);
    font-weight: var(--font-weight-semibold);
    white-space: nowrap;
  }

  &__table tbody tr:last-child th,
  &__table tbody tr:last-child td {
    border-bottom: 0;
  }

  &__table tbody tr {
    transition: background-color 150ms ease;
  }

  &__table tbody tr:hover,
  &__table tbody tr:focus-within {
    background: var(--color-surface-hover);
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

  &__table tbody tr:nth-child(even) {
    background: var(--color-surface-secondary);
  }

  &__table tbody tr:hover,
  &__table tbody tr:focus-within {
    background: var(--color-primary-soft);
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

    &:hover {
      color: var(--color-primary);
      background: var(--color-primary-soft);
    }

    &:focus-visible {
      outline: 0.1875rem solid var(--color-focus);
      outline-offset: 0.125rem;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }
}

@media (hover: none) {
  .equipment-list__row-actions {
    opacity: 1;
  }
}

@media (prefers-reduced-motion: reduce) {
  .equipment-list__table tbody tr,
  .equipment-list__row-actions {
    transition: none;
  }
}
</style>
