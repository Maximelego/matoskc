<script setup lang="ts">
import PageHero from "../common/page/PageHero.vue";
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { ApiError } from "../../api/client";
import { defectsApi } from "../../api/defects";
import type { DefectDto, DefectStatus } from "../../api/defects/dto";
import { equipmentsApi } from "../../api/equipments";
import type { EquipmentDto } from "../../api/equipments/dto";
import { maintenanceApi } from "../../api/maintenance";
import type { MaintenanceDto, RecordMaintenanceDto } from "../../api/maintenance/dto";
import EquipmentPicker from "../common/equipmentPicker/EquipmentPicker.vue";
import SlideAndFadeLateral from "../common/transition/SlideAndFadeLateral.vue";
import MaintenanceDefects from "./MaintenanceDefects.vue";
import MaintenanceForm from "./MaintenanceForm.vue";
import MaintenanceHistory from "./MaintenanceHistory.vue";

const route = useRoute();
const router = useRouter();
const equipments = ref<EquipmentDto[]>([]);
const equipmentLoading = ref(true);
const equipmentId = ref(typeof route.query.equipmentId === "string" ? route.query.equipmentId : "");
const defects = ref<DefectDto[]>([]);
const maintenances = ref<MaintenanceDto[]>([]);
const defectStatus = ref<DefectStatus>("Open");
const defectOffset = ref(0);
const defectTotal = ref(0);
const maintenanceOffset = ref(0);
const maintenanceTotal = ref(0);
const selected = ref<string[]>([]);
const loading = ref(false);
const saving = ref(false);
const maintenanceForm = ref<InstanceType<typeof MaintenanceForm> | null>(null);

const error = ref("");
const notice = ref("");
const limit = 50;
let requestVersion = 0;
let maintenanceRequestVersion = 0;
let pending: { fingerprint: string; payload: RecordMaintenanceDto } | null = null;
const currentEquipment = computed(() =>
  equipments.value.find((item) => item.id === equipmentId.value),
);
function selectEquipment(equipment: EquipmentDto): void {
  equipmentId.value = equipment.id;
  void router.replace({ query: { ...route.query, equipmentId: equipment.id } });
}
function chooseAnotherEquipment(): void {
  defectStatus.value = "Open";
  pending = null;
  equipmentId.value = "";
  void router.replace({ query: { ...route.query, equipmentId: undefined } });
}
function message(cause: unknown) {
  if (cause instanceof ApiError) {
    if (cause.status === 404) return "Ressource introuvable ou inaccessible.";
    if (cause.status === 409)
      return "Le dossier a changé. Actualisez les défauts avant de réessayer.";
    return cause.message;
  }
  return "Impossible de charger ou d’enregistrer les données. Veuillez réessayer.";
}
async function load() {
  const token = ++requestVersion;
  defects.value = [];
  maintenances.value = [];
  selected.value = [];
  defectOffset.value = 0;
  maintenanceOffset.value = 0;
  error.value = "";
  notice.value = "";
  if (!equipmentId.value) {
    loading.value = false;
    return;
  }
  loading.value = true;
  try {
    const [defectPage, maintenancePage] = await Promise.all([
      defectsApi.list(equipmentId.value, defectStatus.value, 0, limit),
      maintenanceApi.list(equipmentId.value, 0, limit),
    ]);
    if (token !== requestVersion) return;
    defects.value = defectPage.items;
    defectTotal.value = defectPage.total;
    maintenances.value = maintenancePage.items;
    maintenanceTotal.value = maintenancePage.total;
  } catch (cause) {
    if (token === requestVersion) error.value = message(cause);
  } finally {
    if (token === requestVersion) loading.value = false;
  }
}
async function pageDefects(offset: number) {
  const token = ++requestVersion;
  loading.value = true;
  error.value = "";
  selected.value = [];
  try {
    const page = await defectsApi.list(equipmentId.value, defectStatus.value, offset, limit);
    if (token === requestVersion) {
      defects.value = page.items;
      defectTotal.value = page.total;
      defectOffset.value = offset;
    }
  } catch (cause) {
    if (token === requestVersion) error.value = message(cause);
  } finally {
    if (token === requestVersion) loading.value = false;
  }
}
async function pageMaintenances(offset: number) {
  const token = ++maintenanceRequestVersion;
  const requestedEquipmentId = equipmentId.value;
  loading.value = true;
  error.value = "";
  try {
    const page = await maintenanceApi.list(requestedEquipmentId, offset, limit);
    if (token !== maintenanceRequestVersion || requestedEquipmentId !== equipmentId.value) return;
    maintenances.value = page.items;
    maintenanceTotal.value = page.total;
    maintenanceOffset.value = offset;
  } catch (cause) {
    if (token === maintenanceRequestVersion && requestedEquipmentId === equipmentId.value)
      error.value = message(cause);
  } finally {
    if (token === maintenanceRequestVersion && requestedEquipmentId === equipmentId.value)
      loading.value = false;
  }
}
watch([equipmentId, defectStatus], () => {
  ++maintenanceRequestVersion;
  pending = null;
  void load();
});
watch(
  () => route.query.equipmentId,
  (id) => {
    equipmentId.value = typeof id === "string" ? id : "";
  },
);
onMounted(async () => {
  try {
    equipments.value = (await equipmentsApi.list()).equipments;
  } catch (cause) {
    error.value = message(cause);
  } finally {
    equipmentLoading.value = false;
  }
  if (equipmentId.value) void load();
});
function updateSelected(ids: string[]): void {
  selected.value = ids;
}
async function submit(fields: Omit<RecordMaintenanceDto, "id">): Promise<void> {
  if (!equipmentId.value || saving.value) return;
  error.value = "";
  notice.value = "";
  const fingerprint = JSON.stringify({ equipmentId: equipmentId.value, ...fields });
  if (pending?.fingerprint !== fingerprint)
    pending = { fingerprint, payload: { id: crypto.randomUUID(), ...fields } };
  saving.value = true;
  try {
    await maintenanceApi.create(equipmentId.value, pending.payload);
    pending = null;
    maintenanceForm.value?.reset();
    await load();
    notice.value = fields.resolvedDefectIds.length
      ? "La maintenance a été enregistrée et les défauts sélectionnés ont été résolus."
      : "La maintenance a été enregistrée.";
  } catch (cause) {
    error.value = message(cause);
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <main class="care">
    <SlideAndFadeLateral>
      <section
        v-if="!equipmentId"
        key="choose"
        class="care__stage"
        aria-labelledby="care-title"
      >
        <PageHero
          title="Sélectionner un équipement"
          description="Choisissez le matériel dont vous souhaitez consulter la maintenance."
          eyebrow="Maintenance"
          title-id="care-title"
        />
        <p
          v-if="equipmentLoading"
          role="status"
        >
          Chargement des équipements…
        </p>
        <p
          v-if="error"
          role="alert"
          class="care__error"
        >
          {{ error }}
        </p>
        <EquipmentPicker
          v-if="!equipmentLoading && equipments.length"
          :equipment="equipments"
          @select="selectEquipment"
        />
        <p v-else-if="!equipmentLoading && !error">Aucun équipement disponible.</p>
      </section>
      <div
        v-else
        :key="equipmentId"
        class="care__stage"
      >
        <PageHero
          title="Maintenance de cet équipement"
          :description="currentEquipment?.name ?? 'Équipement sélectionné'"
          eyebrow="Maintenance"
        >
          <template #actions>
            <button
              type="button"
              :disabled="saving"
              @click="chooseAnotherEquipment"
            >
              Choisir un autre équipement
            </button>
          </template>
        </PageHero>
        <p
          v-if="error"
          role="alert"
          class="care__error"
        >
          {{ error }}
          <button
            type="button"
            @click="load"
          >
            Actualiser
          </button>
        </p>
        <p
          v-if="notice"
          role="status"
          class="care__success"
        >
          {{ notice }}
        </p>
        <p
          v-if="loading"
          role="status"
        >
          Chargement…
        </p>
        <div class="care__content">
          <MaintenanceDefects
            v-model:status="defectStatus"
            :selected="selected"
            :defects="defects"
            :equipment-id="equipmentId"
            :total="defectTotal"
            :offset="defectOffset"
            :limit="limit"
            :loading="loading"
            :disabled="saving"
            @update:selected="updateSelected"
            @page="pageDefects"
          />
          <MaintenanceForm
            ref="maintenanceForm"
            :key="equipmentId"
            :selected="selected"
            :defects="defects"
            :disabled="saving || loading"
            @submit="submit"
          />
          <MaintenanceHistory
            :entries="maintenances"
            :total="maintenanceTotal"
            :offset="maintenanceOffset"
            :limit="limit"
            :loading="loading"
            @page="pageMaintenances"
          />
        </div>
      </div>
    </SlideAndFadeLateral>
  </main>
</template>

<style scoped lang="scss">
.care {
  display: grid;
  max-width: 70rem;
  min-width: 0;
  margin: auto;
}
.care__stage {
  display: grid;
  gap: 1.5rem;
  min-width: 0;
}
.care button {
  min-height: 2.75rem;
  padding: 0.6rem 0.9rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface-secondary);
  color: var(--color-text);
  font: inherit;
  cursor: pointer;
}
.care button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.care__content {
  display: grid;
  gap: 2rem;
  min-width: 0;
}
.care__error {
  color: var(--color-danger);
}
.care__success {
  margin: 0;
  padding: 0.9rem 1rem;
  border-radius: 0.6rem;
  color: var(--color-success);
  background: var(--color-success-soft);
}
</style>
