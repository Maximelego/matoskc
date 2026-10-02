<script setup lang="ts">
import { onMounted, ref } from "vue";
import { ApiError } from "../../api/client";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";
import BaseSpinner from "../common/spinner/BaseSpinner.vue";
import InspectionEquipmentPicker from "./InspectionEquipmentPicker.vue";

const equipment = ref<EquipmentDto[]>([]);
const loading = ref(true);
const error = ref("");

function errorMessage(cause: unknown): string {
  if (cause instanceof ApiError) {
    if (cause.status === 401) {
      return "Veuillez vous connecter pour consulter les équipements.";
    }

    if (cause.status === 403) {
      return "Vous ne disposez pas des droits nécessaires pour consulter les équipements.";
    }

    if (cause.status >= 500) {
      return "Le serveur rencontre actuellement un problème. Veuillez réessayer dans quelques instants.";
    }

    return "Impossible de récupérer la liste des équipements. Veuillez réessayer.";
  }

  if (cause instanceof TypeError) {
    return "Impossible de joindre le serveur. Veuillez vérifier votre connexion, puis réessayer.";
  }

  return "Une erreur inattendue est survenue lors du chargement des équipements.";
}

async function load(): Promise<void> {
  loading.value = true;
  error.value = "";

  try {
    const [available, borrowed] = await Promise.all([
      equipmentsApi.list({ status: "Available" }),
      equipmentsApi.list({ status: "Borrowed" }),
    ]);

    equipment.value = [...available.equipments, ...borrowed.equipments].sort((a, b) =>
      a.name.localeCompare(b.name, "fr"),
    );
  } catch (cause: unknown) {
    error.value = errorMessage(cause);
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  void load();
});
</script>

<template>
  <main
    class="inspection-list"
    aria-labelledby="inspection-list-title"
  >
    <header class="inspection-list__header">
      <h1 id="inspection-list-title">Choisir un équipement</h1>
      <p>
        Sélectionnez un équipement pour commencer son état des lieux. Un code QR peut également
        ouvrir directement sa fiche.
      </p>
    </header>

    <div
      v-if="loading"
      class="inspection-list__state"
      role="status"
      aria-live="polite"
    >
      <BaseSpinner size="medium" />
      <p>Chargement des équipements…</p>
    </div>

    <section
      v-else-if="error"
      class="inspection-list__error"
      role="alert"
      aria-labelledby="inspection-error-title"
    >
      <span
        class="inspection-list__error-icon"
        aria-hidden="true"
      >
        <BaseIcon
          name="warning"
          :size="28"
        />
      </span>

      <div class="inspection-list__error-content">
        <h2 id="inspection-error-title">Les équipements sont momentanément indisponibles</h2>
        <p>{{ error }}</p>

        <BaseButton
          variant="outline"
          size="small"
          @click="load"
        >
          <template #leading>
            <BaseIcon
              name="refresh"
              :size="18"
            />
          </template>
          Réessayer
        </BaseButton>
      </div>
    </section>

    <p
      v-else-if="!equipment.length"
      class="inspection-list__state"
    >
      Aucun équipement disponible pour un état des lieux.
    </p>

    <InspectionEquipmentPicker
      v-else
      :equipment="equipment"
    />
  </main>
</template>

<style scoped lang="scss">
.inspection-list {
  display: grid;
  gap: 1.5rem;
  width: min(100%, 72rem);
  min-width: 0;
  margin-inline: auto;

  &__header {
    display: grid;
    gap: 0.5rem;

    h1,
    p {
      margin: 0;
    }

    p {
      color: var(--color-text-secondary);
    }
  }

  &__state {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    min-height: 10rem;
    margin: 0;
    padding: 1.5rem;
    color: var(--color-text-secondary);
    text-align: center;

    p {
      margin: 0;
    }
  }

  &__error {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1.25rem;
    border: 1px solid var(--color-border);
    border-radius: 0.75rem;
    background: var(--color-surface);
  }

  &__error-icon {
    display: grid;
    flex: none;
    place-items: center;
    width: 3rem;
    height: 3rem;
    border-radius: 0.75rem;
    color: var(--color-danger);
    background: var(--color-surface-secondary);
  }

  &__error-content {
    display: grid;
    justify-items: start;
    gap: 0.75rem;
    min-width: 0;

    h2 {
      margin: 0;
      font-size: 1.125rem;
    }

    p {
      margin: 0;
      color: var(--color-text-secondary);
    }
  }
}

@media (max-width: 42rem) {
  .inspection-list__error {
    gap: 0.75rem;
    padding: 1rem;
  }
}
</style>
