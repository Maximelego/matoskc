<script setup lang="ts">
import { onMounted, ref } from "vue";
import { ApiError } from "../../api/client";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseSpinner from "../common/spinner/BaseSpinner.vue";

const emit = defineEmits<{
  view: [equipment: EquipmentDto];
  edit: [equipment: EquipmentDto];
}>();

const equipments = ref<EquipmentDto[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

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

onMounted(loadEquipments);
</script>

<template>
  <section
    class="equipment-list"
    aria-labelledby="equipment-list-title"
  >
    <div class="equipment-list__header">
      <h1 id="equipment-list-title">Équipements</h1>

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
    </div>

    <p
      v-if="loading && equipments.length === 0"
      role="status"
    >
      <BaseSpinner size="medium" />
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
        <template #leading>
          <BaseIcon name="refresh" />
        </template>
        Réessayer
      </BaseButton>
    </div>

    <p
      v-else-if="!loading && equipments.length === 0"
      class="equipment-list__empty"
    >
      Aucun équipement à afficher.
    </p>

    <div
      v-if="equipments.length > 0"
      class="equipment-list__table-container"
    >
      <table class="equipment-list__table">
        <thead>
          <tr>
            <th scope="col">Nom</th>
            <th scope="col">Numéro de série</th>
            <th scope="col">Catégorie</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>

        <tbody>
          <tr
            v-for="equipment in equipments"
            :key="equipment.id"
          >
            <td>{{ equipment.name }}</td>
            <td>{{ equipment.serialNumber || "—" }}</td>
            <td>{{ equipment.equipmentCategoryId }}</td>
            <td>
              <div class="equipment-list__actions">
                <BaseButton
                  variant="outline"
                  size="small"
                  :disabled="loading"
                  @click="emit('view', equipment)"
                >
                  <template #leading>
                    <BaseIcon name="view" />
                  </template>
                  Consulter
                </BaseButton>

                <BaseButton
                  variant="ghost"
                  size="small"
                  :disabled="loading"
                  @click="emit('edit', equipment)"
                >
                  <template #leading>
                    <BaseIcon name="edit" />
                  </template>
                  Modifier
                </BaseButton>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<style scoped lang="scss">
.equipment-list {
  display: grid;
  gap: 1rem;

  &__header,
  &__actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  &__header {
    justify-content: space-between;
  }

  &__header h1 {
    margin: 0;
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

  &__table-container {
    overflow-x: auto;
  }

  &__table {
    width: 100%;
    border-collapse: collapse;
    text-align: left;
  }

  &__table th,
  &__table td {
    padding: 0.75rem;
    border-bottom: 1px solid var(--color-border);
  }

  &__table th {
    white-space: nowrap;
  }

  &__actions {
    white-space: nowrap;
  }
}
</style>
