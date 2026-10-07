<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto.ts";
import type { EquipmentDto, EquipmentStatus } from "../../api/equipments/dto";
import BaseModal from "../common/modal/BaseModal.vue";
import EquipmentPhoto from "./EquipmentPhoto.vue";
import EquipmentStatusBadge from "../equipmentStatusBadge/EquipmentStatusBadge.vue";

const props = defineProps<{
  equipment: EquipmentDto | null;
  open: boolean;
  equipmentCategories: EquipmentCategoryDto[];
  busy?: boolean;
  error?: string | null;
}>();

const emit = defineEmits<{
  close: [];
  closed: [];
  changeStatus: [equipment: EquipmentDto, status: EquipmentStatus];
}>();

const labels: Record<EquipmentStatus, string> = {
  Available: "Disponible",
  Borrowed: "Emprunté",
  ToBeDecided: "À déterminer",
  Unavailable: "Indisponible",
  Maintenance: "En maintenance",
  Decommissioned: "Réformé",
};

const transitions: Record<EquipmentStatus, EquipmentStatus[]> = {
  Available: ["Decommissioned"],
  Borrowed: [],
  ToBeDecided: ["Available", "Unavailable"],
  Unavailable: ["Available", "Maintenance", "Decommissioned"],
  Maintenance: ["Available", "Unavailable", "Decommissioned"],
  Decommissioned: [],
};

const selectedStatus = ref<EquipmentStatus | "">("");

watch(
  () => props.equipment,
  () => {
    selectedStatus.value = "";
  },
);

const availableStatuses = computed(() =>
  props.equipment ? (transitions[props.equipment.status] ?? []) : [],
);

function submit(): void {
  const equipment = props.equipment;
  const status = selectedStatus.value;

  if (!equipment || !status || props.busy || !availableStatuses.value.includes(status)) {
    return;
  }

  emit("changeStatus", equipment, status);
}

</script>

<template>
  <BaseModal
    :open="open"
    :busy="busy"
    :title="equipment?.name ?? 'Détails de l’équipement'"
    @close="emit('close')"
  >
    <div
      v-if="equipment"
      class="equipment-details"
    >
      <EquipmentPhoto :equipment-id="equipment.id" :open="open" />

      <dl class="equipment-details__fields">
        <div>
          <dt>Nom</dt>
          <dd>{{ equipment.name }}</dd>
        </div>

        <div>
          <dt>Numéro de série</dt>
          <dd>{{ equipment.serialNumber || "Non renseigné" }}</dd>
        </div>

        <div>
          <dt>Catégorie</dt>
          <dd>
            {{
              equipmentCategories.find((c) => c.id === equipment?.equipmentCategoryId)?.name ||
              "Non renseignée"
            }}
          </dd>
        </div>

        <div>
          <dt>État actuel</dt>
          <dd><EquipmentStatusBadge :status="equipment.status" /></dd>
        </div>
      </dl>

      <form
        class="equipment-details__status"
        @submit.prevent="submit"
      >
        <h3>Changer l’état</h3>

        <p v-if="availableStatuses.length === 0">
          Aucun changement d’état n’est autorisé pour cet équipement.
        </p>

        <template v-else>
          <label for="equipment-next-status">Nouvel état</label>
          <select
            id="equipment-next-status"
            v-model="selectedStatus"
            :disabled="busy"
          >
            <option value="">Sélectionnez un état</option>
            <option
              v-for="status in availableStatuses"
              :key="status"
              :value="status"
            >
              {{ labels[status] }}
            </option>
          </select>

          <p
            v-if="error"
            class="equipment-details__error"
            role="alert"
          >
            {{ error }}
          </p>

          <button
            type="submit"
            :disabled="!selectedStatus || busy"
          >
            {{ busy ? "Enregistrement…" : "Enregistrer le nouvel état" }}
          </button>
        </template>
      </form>
    </div>
  </BaseModal>
</template>

<style scoped lang="scss">
.equipment-details {
  display: grid;
  gap: 1.5rem;

  &__photo {
    display: grid;
    place-items: center;
    min-height: 12rem;
    border: 1px dashed var(--color-border-strong);
    border-radius: 0.75rem;
    color: var(--color-text-secondary);
    background: var(--color-surface-secondary);
  }

  &__fields {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
    gap: 1rem;
    margin: 0;

    div {
      min-width: 0;
    }

    dt {
      margin-bottom: 0.35rem;
      color: var(--color-text-secondary);
      font-size: var(--font-size-sm);
    }

    dd {
      margin: 0;
      overflow-wrap: anywhere;
      font-weight: var(--font-weight-semibold);
    }
  }

  &__status {
    display: grid;
    gap: 0.75rem;
    padding-top: 1.25rem;
    border-top: 1px solid var(--color-border);

    h3,
    p {
      margin: 0;
    }

    select {
      width: 100%;
      padding: 0.625rem;
      border: 1px solid var(--color-border);
      border-radius: 0.5rem;
      color: var(--color-text);
      background: var(--color-input-background);
    }

    button {
      justify-self: start;
      padding: 0.625rem 1rem;
      border: 0;
      border-radius: 0.5rem;
      color: var(--color-text-on-primary);
      background: var(--color-primary);
      cursor: pointer;

      &:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
    }
  }

  &__error {
    color: var(--color-danger);
  }
}

@media (max-width: 42rem) {
  .equipment-details { gap: 1rem; }
  .equipment-details__photo { min-height: 9rem; }
  .equipment-details__fields { grid-template-columns: minmax(0, 1fr); gap: 0.875rem; }
  .equipment-details__status button { justify-self: stretch; min-height: 2.75rem; }
}
</style>
