<script setup lang="ts">
import BaseInput from "../common/input/BaseInput.vue";
import { computed, nextTick, ref, watch } from "vue";
import { authSession } from "../../api/auth/session";
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto.ts";
import type { EquipmentDto, EquipmentStatus } from "../../api/equipments/dto";
import BaseIcon from "../common/icon/BaseIcon.vue";
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
const editNameTouched = ref(false);
const editCategoryId = ref("");
const editing = ref(false);
const showHistory = ref(false);
const historyLoaded = ref(false);

watch(
  () => props.equipment,
  () => {
    selectedStatus.value = "";
    reason.value = "";
    editName.value = props.equipment?.name ?? "";
    editNameTouched.value = false;
    editCategoryId.value = props.equipment?.equipmentCategoryId ?? "";
    editing.value = false;
    showHistory.value = false;
    historyLoaded.value = false;
  },
);
watch(
  () => props.open,
  (open) => {
    if (!open) {
      editing.value = false;
      showHistory.value = false;
    }
  },
);
const isAdministrator = computed(() =>
  ["Admin", "SuperAdmin"].includes(authSession.account.value?.role ?? ""),
);
const availableStatuses = computed(() =>
  props.equipment ? transitions[props.equipment.status] : [],
);
const categoryName = computed(
  () =>
    props.equipmentCategories.find(
      (category) => category.id === props.equipment?.equipmentCategoryId,
    )?.name ?? "Non renseignée",
);
function cancelEdit(): void {
  editName.value = props.equipment?.name ?? "";
  editCategoryId.value = props.equipment?.equipmentCategoryId ?? "";
  editing.value = false;
}
function saveEquipment(): void {
  editNameTouched.value = true;
  if (!props.equipment || !editName.value.trim() || !editCategoryId.value || props.busy) return;
  if (
    editName.value.trim() === props.equipment.name &&
    editCategoryId.value === props.equipment.equipmentCategoryId
  ) {
    cancelEdit();
    return;
  }
  emit("updateEquipment", props.equipment, editName.value.trim(), editCategoryId.value);
}
function submitStatus(): void {
  if (
    !props.equipment ||
    !selectedStatus.value ||
    props.busy ||
    !availableStatuses.value.includes(selectedStatus.value) ||
    !reason.value.trim() ||
    reason.value.trim().length > 1000
  )
    return;
  emit("changeStatus", props.equipment, selectedStatus.value, reason.value.trim());
}
async function toggleHistory(): Promise<void> {
  if (showHistory.value) {
    showHistory.value = false;
    return;
  }
  historyLoaded.value = true;
  await nextTick();
  showHistory.value = true;
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

      <section class="equipment-details__section">
        <div class="equipment-details__heading">
          <h3>Informations</h3>
          <button
            v-if="isAdministrator && !editing"
            type="button"
            class="equipment-details__edit"
            :disabled="busy"
            @click="editing = true"
          >
            <BaseIcon
              name="edit"
              :size="18"
            />
            Modifier
          </button>
        </div>
        <form
          novalidate
          @submit.prevent="saveEquipment"
        >
          <dl class="equipment-details__fields">
            <div>
              <dt>Nom</dt>
              <dd v-if="editing">
                <BaseInput
                  label="Nom du matériel"
                  id="equipment-edit-name"
                  v-model="editName"
                  :error="
                    editNameTouched && !editName.trim()
                      ? 'Le nom du matériel est obligatoire.'
                      : undefined
                  "
                  required
                  :disabled="busy"
                  maxlength="200"
                />
              </dd>
              <dd v-else>{{ equipment.name }}</dd>
            </div>
            <div>
              <dt>Numéro de série</dt>
              <dd>{{ equipment.serialNumber || "Non renseigné" }}</dd>
            </div>
            <div>
              <dt>Catégorie</dt>
              <dd v-if="editing">
                <select
                  id="equipment-edit-category"
                  v-model="editCategoryId"
                  aria-label="Catégorie du matériel"
                  required
                  :disabled="busy"
                >
                  <option
                    v-for="category in equipmentCategories.filter(
                      (item) => item.agencyId === equipment?.agencyId,
                    )"
                    :key="category.id"
                    :value="category.id"
                  >
                    {{ category.name }}
                  </option>
                </select>
              </dd>
              <dd v-else>{{ categoryName }}</dd>
            </div>
            <div>
              <dt>État actuel</dt>
              <dd><EquipmentStatusBadge :status="equipment.status" /></dd>
            </div>
          </dl>
          <div
            v-if="editing"
            class="equipment-details__edit-actions"
          >
            <button
              type="button"
              class="equipment-details__secondary"
              :disabled="busy"
              @click="cancelEdit"
            >
              Annuler
            </button>
            <button
              type="submit"
              class="equipment-details__primary"
              :disabled="busy || !editName.trim() || !editCategoryId"
            >
              {{ busy ? "Enregistrement…" : "Enregistrer" }}
            </button>
          </div>
        </form>
      </section>
      <p
        v-if="error"
        class="equipment-details__error"
        role="alert"
      >
        {{ error }}
      </p>

      <div
        v-if="isAdministrator"
        class="equipment-details__navigation"
      >
        <RouterLink
          class="equipment-details__nav-card"
          :to="{ name: 'ManageMaintenances', query: { equipmentId: equipment.id } }"
        >
          <BaseIcon
            name="maintenance"
            :size="25"
          />
          <span
            ><strong>Défauts et maintenances</strong
            ><small>Consulter les constats et les interventions</small></span
          >
          <BaseIcon
            name="arrow-right"
            :size="20"
          />
        </RouterLink>
        <button
          type="button"
          class="equipment-details__nav-card"
          :aria-expanded="showHistory"
          aria-controls="equipment-history"
          :disabled="busy"
          @click="toggleHistory"
        >
          <BaseIcon
            name="clock"
            :size="25"
          />
          <span
            ><strong>Historique du matériel</strong
            ><small>{{
              showHistory ? "Masquer les opérations" : "Voir les opérations enregistrées"
            }}</small></span
          >
          <BaseIcon
            :name="showHistory ? 'chevron-up' : 'chevron-down'"
            :size="20"
          />
        </button>
        <div
          id="equipment-history"
          class="equipment-details__history-reveal"
          :class="{ 'equipment-details__history-reveal--open': showHistory }"
          :aria-hidden="!showHistory"
          :inert="!showHistory"
        >
          <div class="equipment-details__history-content">
            <EquipmentHistory
              v-if="historyLoaded"
              :equipment-id="equipment.id"
            />
          </div>
        </div>
      </div>

      <form
        class="equipment-details__section equipment-details__status"
        @submit.prevent="submitStatus"
      >
        <h3>Changer l’état</h3>
        <p v-if="!availableStatuses.length">
          Aucun changement d’état n’est autorisé pour cet équipement.
        </p>
        <template v-else>
          <fieldset
            class="equipment-details__choices"
            :disabled="busy"
          >
            <legend>Choisissez le nouvel état</legend>
            <label
              v-for="status in availableStatuses"
              :key="status"
              :class="{ 'equipment-details__choice--selected': selectedStatus === status }"
            >
              <input
                v-model="selectedStatus"
                type="radio"
                name="equipment-next-status"
                :value="status"
              />
              <EquipmentStatusBadge :status="status" />
            </label>
          </fieldset>
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
          <button
            type="submit"
            class="equipment-details__primary"
            :disabled="!selectedStatus || !reason.trim() || busy"
          >
            {{ busy ? "Enregistrement…" : "Enregistrer le nouvel état" }}
          </button>
        </template>
      </form>
      <section class="equipment-details__section equipment-details__qr">
        <h3>QR code du matériel</h3>
        <EquipmentQRCode
          :equipment-id="equipment.id"
          :equipment-name="equipment.name"
        />
      </section>
    </div>
  </BaseModal>
</template>

<style scoped lang="scss">
.equipment-details {
  display: grid;
  gap: 1.5rem;
  min-width: 0;

  &__section {
    display: grid;
    gap: 0.9rem;
    padding-top: 1rem;
    border-top: 1px solid var(--color-border);
  }
  &__heading {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
  }
  h3,
  p {
    margin: 0;
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
  input:not([type="radio"]),
  select,
  textarea {
    box-sizing: border-box;
    width: 100%;
    min-height: 2.75rem;
    padding: 0.625rem;
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    color: var(--color-text);
    background: var(--color-input-background);
    font: inherit;
  }
  button {
    font: inherit;
    cursor: pointer;
  }
  button:focus-visible,
  a:focus-visible,
  input:focus-visible,
  select:focus-visible,
  textarea:focus-visible {
    outline: 3px solid var(--color-focus);
    outline-offset: 2px;
  }
  &__edit,
  &__secondary,
  &__primary {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.4rem;
    min-height: 2.75rem;
    padding: 0.6rem 0.9rem;
    border-radius: 0.55rem;
  }
  &__edit,
  &__secondary {
    border: 1px solid var(--color-border);
    background: var(--color-surface);
    color: var(--color-text);
  }
  &__edit:hover:not(:disabled),
  &__secondary:hover:not(:disabled) {
    background: var(--color-secondary-soft-hover);
  }
  &__primary {
    justify-self: start;
    border: 1px solid var(--color-primary);
    background: var(--color-primary);
    color: var(--color-text-on-primary);
  }
  &__primary:hover:not(:disabled) {
    background: var(--color-primary-hover);
  }
  button:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
  &__edit-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.6rem;
    margin-top: 1rem;
  }
  &__navigation {
    display: grid;
    gap: 0.6rem;
    min-width: 0;
  }
  &__nav-card {
    box-sizing: border-box;
    display: flex;
    align-items: center;
    gap: 0.8rem;
    width: 100%;
    min-height: 4rem;
    padding: 0.85rem;
    border: 1px solid var(--color-border);
    border-radius: 0.75rem;
    color: var(--color-text);
    background: var(--color-surface-secondary);
    text-align: left;
    text-decoration: none;
    transition:
      background-color 180ms ease,
      border-color 180ms ease,
      transform 180ms ease;
    &:hover:not(:disabled) {
      background: var(--color-secondary-soft);
      border-color: var(--color-secondary);
      transform: translateY(-2px);
    }
    > span {
      display: grid;
      gap: 0.2rem;
      min-width: 0;
      flex: 1;
    }
    strong {
      font-weight: var(--font-weight-semibold);
    }
    small {
      color: var(--color-text-secondary);
    }
    > svg:last-child {
      flex: none;
    }
  }
  &__history-reveal {
    display: grid;
    grid-template-rows: 0fr;
    opacity: 0;
    transition:
      grid-template-rows 320ms ease,
      opacity 260ms ease;
    &--open {
      grid-template-rows: 1fr;
      opacity: 1;
    }
  }
  &__history-content {
    min-height: 0;
    overflow: hidden;
  }
  &__status {
    > label {
      font-weight: var(--font-weight-semibold);
    }
  }
  &__choices {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr));
    gap: 0.55rem;
    margin: 0;
    padding: 0;
    border: 0;
    legend {
      margin-bottom: 0.55rem;
      font-weight: var(--font-weight-semibold);
    }
    label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      min-height: 3rem;
      padding: 0.65rem;
      border: 1px solid var(--color-border);
      border-radius: 0.65rem;
      background: var(--color-surface-secondary);
      cursor: pointer;
      &:hover {
        background: var(--color-secondary-soft);
      }
      &.equipment-details__choice--selected {
        border-color: var(--color-secondary);
        background: var(--color-secondary-soft);
      }
    }
    input {
      accent-color: var(--color-secondary);
      flex: none;
    }
  }
  &__error {
    color: var(--color-danger);
  }
  &__qr {
    justify-items: center;
    h3 {
      justify-self: start;
    }
  }
}
@media (max-width: 42rem) {
  .equipment-details {
    gap: 1rem;
  }
  .equipment-details__fields {
    grid-template-columns: minmax(0, 1fr);
  }
  .equipment-details__primary {
    justify-self: stretch;
  }
  .equipment-details__edit-actions > button {
    flex: 1;
  }
}
@media (prefers-reduced-motion: reduce) {
  .equipment-details__history-reveal,
  .equipment-details__nav-card {
    transition: none;
  }
}
</style>
