<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { authSession } from "../../api/auth/session";
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto.ts";
import type { EquipmentDto, EquipmentStatus } from "../../api/equipments/dto";
import BaseModal from "../common/modal/BaseModal.vue";
import EquipmentStatusBadge from "../equipmentStatusBadge/EquipmentStatusBadge.vue";
import EquipmentHistory from "./EquipmentHistory.vue";
import EquipmentPhoto from "./EquipmentPhoto.vue";
import EquipmentQRCode from "./EquipmentQRCode.vue";

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
  changeStatus: [equipment: EquipmentDto, status: EquipmentStatus, reason: string];
  updateEquipment: [equipment: EquipmentDto, name: string, categoryId: string];
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
const reason = ref("");
const editName = ref("");
const editCategoryId = ref("");
const showHistory = ref(false);

watch(
  () => props.equipment,
  () => {
    selectedStatus.value = "";
    reason.value = "";
    editName.value = props.equipment?.name ?? "";
    editCategoryId.value = props.equipment?.equipmentCategoryId ?? "";
    showHistory.value = false;
  },
);

const isAdministrator = computed(() =>
  ["Admin", "SuperAdmin"].includes(authSession.account.value?.role ?? ""),
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

  if (!reason.value.trim() || reason.value.trim().length > 1000) return;
  emit("changeStatus", equipment, status, reason.value.trim());
}
function saveEquipment(): void {
  if (!props.equipment || !editName.value.trim() || !editCategoryId.value || props.busy) return;
  emit("updateEquipment", props.equipment, editName.value.trim(), editCategoryId.value);
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
      <EquipmentPhoto
        :equipment-id="equipment.id"
        :open="open"
      />

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
        v-if="isAdministrator"
        class="equipment-details__status"
        @submit.prevent="saveEquipment"
      >
        <h3>Modifier le matériel</h3>
        <label for="equipment-edit-name">Nom</label>
        <input
          id="equipment-edit-name"
          v-model="editName"
          required
          :disabled="busy"
        />
        <label for="equipment-edit-category">Catégorie</label>
        <select
          id="equipment-edit-category"
          v-model="editCategoryId"
          required
          :disabled="busy"
        >
          <option
            v-for="category in equipmentCategories"
            :key="category.id"
            :value="category.id"
          >
            {{ category.name }}
          </option>
        </select>
        <button
          type="submit"
          :disabled="busy || !editName.trim() || !editCategoryId"
        >
          Enregistrer les modifications
        </button>
      </form>
      <RouterLink
        v-if="isAdministrator"
        :to="{ name: 'ManageMaintenances', query: { equipmentId: equipment.id } }"
      >
        Voir les défauts et les maintenances
      </RouterLink>
      <div v-if="isAdministrator">
        <BaseButton
          type="button"
          class="equipment-details__history"
          @click="showHistory = !showHistory"
        >
          {{ showHistory ? "Masquer l’historique" : "Afficher l’historique du matériel" }}
        </BaseButton>
        <EquipmentHistory
          v-if="showHistory"
          :equipment-id="equipment.id"
        />
      </div>

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

          <label for="equipment-status-reason">Motif du changement *</label>
          <textarea
            id="equipment-status-reason"
            v-model="reason"
            required
            maxlength="1000"
            rows="3"
            :disabled="busy"
            placeholder="Précisez la raison de cette décision"
          />

          <p
            v-if="error"
            class="equipment-details__error"
            role="alert"
          >
            {{ error }}
          </p>

          <button
            type="submit"
            :disabled="!selectedStatus || !reason.trim() || busy"
          >
            {{ busy ? "Enregistrement…" : "Enregistrer le nouvel état" }}
          </button>
        </template>

        <EquipmentQRCode
          v-if="equipment"
          :equipment-id="equipment.id"
          :equipment-name="equipment.name"
        />
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

  a {
    text-decoration: none;
  }

  &__status {
    display: grid;
    gap: 0.75rem;
    padding-top: 1.25rem;
    border-top: 1px solid var(--color-border);

    h3,
    x p {
      margin: 0;
    }

    input,
    select,
    textarea {
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

  &__history {
    justify-self: start;
    padding: 0.5rem;
    margin: 0.2rem 0.2rem 0.2rem 0;
    border: 0;
    border-radius: 0.5rem;
    font: inherit;
    cursor: pointer;
    color: var(--color-primary);
    text-decoration: none;
  }
}

@media (max-width: 42rem) {
  .equipment-details {
    gap: 1rem;
  }
  .equipment-details__photo {
    min-height: 9rem;
  }
  .equipment-details__fields {
    grid-template-columns: minmax(0, 1fr);
    gap: 0.875rem;
  }
  .equipment-details__status button {
    justify-self: stretch;
    min-height: 2.75rem;
  }
}
</style>
