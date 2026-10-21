<script setup lang="ts">
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
import DefectPhoto from "./DefectPhoto.vue";

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
const performedBy = ref("");
const performedAt = ref("");
const workDescription = ref("");
const loading = ref(false);
const saving = ref(false);
const error = ref("");
const notice = ref("");
const limit = 50;
const maxDate = new Date(Date.now() - new Date().getTimezoneOffset() * 60_000)
  .toISOString()
  .slice(0, 16);
let requestVersion = 0;
let pending: { fingerprint: string; payload: RecordMaintenanceDto } | null = null;
const openDefects = computed(() => defects.value.filter((item) => item.status === "Open"));
const currentEquipment = computed(() =>
  equipments.value.find((item) => item.id === equipmentId.value),
);
function selectEquipment(equipment: EquipmentDto): void {
  equipmentId.value = equipment.id;
  void router.replace({ query: { ...route.query, equipmentId: equipment.id } });
}
function chooseAnotherEquipment(): void {
  if (saving.value) return;
  performedAt.value = "";
  performedBy.value = "";
  workDescription.value = "";
  defectStatus.value = "Open";
  pending = null;
  equipmentId.value = "";
  void router.replace({ query: { ...route.query, equipmentId: undefined } });
}
function message(cause: unknown) {
  if (cause instanceof ApiError) {
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
  if (!equipmentId.value) return;
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
  loading.value = true;
  error.value = "";
  try {
    const page = await maintenanceApi.list(equipmentId.value, offset, limit);
    maintenances.value = page.items;
    maintenanceTotal.value = page.total;
    maintenanceOffset.value = offset;
  } catch (cause) {
    error.value = message(cause);
  } finally {
    loading.value = false;
  }
}
watch([equipmentId, defectStatus], () => {
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
async function submit() {
  if (!equipmentId.value || saving.value) return;
  error.value = "";
  notice.value = "";
  const timestamp = new Date(performedAt.value);
  if (
    !performedBy.value.trim() ||
    !workDescription.value.trim() ||
    !Number.isFinite(timestamp.getTime())
  ) {
    error.value = "Renseignez la date, l’intervenant et la description des travaux.";
    return;
  }
  if (
    timestamp.getTime() > Date.now() ||
    performedBy.value.trim().length > 200 ||
    workDescription.value.trim().length > 10_000
  ) {
    error.value = "Vérifiez la date et la longueur des champs (200 et 10 000 caractères maximum).";
    return;
  }
  if (
    selected.value.some((id) => {
      const defect = openDefects.value.find((item) => item.id === id);
      return !defect || timestamp.getTime() < new Date(defect.reportedAt).getTime();
    })
  ) {
    error.value =
      "La date d’intervention doit être postérieure au constat de chaque défaut sélectionné.";
    return;
  }
  const fields = {
    performedAt: timestamp.toISOString(),
    performedBy: performedBy.value.trim(),
    workDescription: workDescription.value.trim(),
    resolvedDefectIds: [...selected.value].sort(),
  };
  const fingerprint = JSON.stringify({ equipmentId: equipmentId.value, ...fields });
  if (pending?.fingerprint !== fingerprint)
    pending = { fingerprint, payload: { id: crypto.randomUUID(), ...fields } };
  saving.value = true;
  try {
    await maintenanceApi.create(equipmentId.value, pending.payload);
    pending = null;
    selected.value = [];
    workDescription.value = "";
    await load();
    notice.value = "La maintenance a été enregistrée et les défauts sélectionnés ont été résolus.";
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
        <header>
          <h1 id="care-title">Sélectionner un équipement</h1>
          <p>Choisissez le matériel dont vous souhaitez consulter la maintenance.</p>
        </header>
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
        <header class="care__detail-header">
          <div>
            <h1>Maintenance de cet équipement</h1>
            <p>{{ currentEquipment?.name ?? "Équipement sélectionné" }}</p>
          </div>
          <button
            type="button"
            :disabled="saving"
            @click="chooseAnotherEquipment"
          >
            Choisir un autre équipement
          </button>
        </header>
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
        <section class="care__panel">
          <h2>Défauts constatés</h2>
          <label for="defect-status">Statut</label>
          <select
            id="defect-status"
            v-model="defectStatus"
            :disabled="saving"
          >
            <option value="Open">Ouverts</option>
            <option value="Resolved">Résolus</option>
          </select>
          <p v-if="!loading && !defects.length">
            Aucun défaut {{ defectStatus === "Open" ? "ouvert" : "résolu" }}.
          </p>
          <ul class="care__defects">
            <li
              v-for="defect in defects"
              :key="defect.id"
            >
              <label
                v-if="defect.status === 'Open'"
                class="care__checkbox"
              >
                <input
                  v-model="selected"
                  type="checkbox"
                  :value="defect.id"
                  :disabled="saving"
                />
                Résoudre pendant cette maintenance
              </label>
              <strong>{{ defect.description }}</strong>
              <p>{{ defect.observation }}</p>
              <small
                >Signalé le {{ new Date(defect.reportedAt).toLocaleString("fr-FR") }} ·
                {{ defect.occurrences.length }} constat(s)</small
              >
              <details v-if="defect.occurrences.length">
                <summary>Voir les constats</summary>
                <ul>
                  <li
                    v-for="occurrence in defect.occurrences"
                    :key="occurrence.id"
                  >
                    {{ occurrence.observation }} ·
                    {{ new Date(occurrence.observedAt).toLocaleDateString("fr-FR") }}
                    <div class="care__photos">
                      <DefectPhoto
                        v-for="id in occurrence.photoIds"
                        :key="id"
                        :equipment-id="equipmentId"
                        :defect-id="defect.id"
                        :photo-id="id"
                      />
                    </div>
                  </li>
                </ul>
              </details>
              <small v-if="defect.resolvedAt"
                >Résolu le {{ new Date(defect.resolvedAt).toLocaleString("fr-FR") }}</small
              >
            </li>
          </ul>
          <nav
            v-if="defectTotal > limit || defectOffset"
            aria-label="Pages des défauts"
          >
            <button
              type="button"
              :disabled="loading || !defectOffset"
              @click="pageDefects(defectOffset - limit)"
            >
              Précédent
            </button>
            <span
              >{{ defectOffset + 1 }}–{{ Math.min(defectOffset + limit, defectTotal) }} /
              {{ defectTotal }}</span
            >
            <button
              type="button"
              :disabled="loading || defectOffset + limit >= defectTotal"
              @click="pageDefects(defectOffset + limit)"
            >
              Suivant
            </button>
          </nav>
        </section>
        <form
          class="care__panel"
          @submit.prevent="submit"
        >
          <h2>Enregistrer une maintenance</h2>
          <p>
            Une maintenance peut être enregistrée sans résoudre de défaut. Résoudre un défaut ne
            modifie pas automatiquement la disponibilité du matériel.
          </p>
          <label for="care-date">Date et heure de l’intervention</label>
          <input
            id="care-date"
            v-model="performedAt"
            type="datetime-local"
            :max="maxDate"
            required
            :disabled="saving"
          />
          <label for="care-person">Intervenant</label>
          <input
            id="care-person"
            v-model="performedBy"
            maxlength="200"
            required
            :disabled="saving"
          />
          <label for="care-work">Travaux effectués</label>
          <textarea
            id="care-work"
            v-model="workDescription"
            rows="5"
            maxlength="10000"
            required
            :disabled="saving"
          />
          <p>{{ selected.length }} défaut(s) sélectionné(s) pour résolution</p>
          <button
            type="submit"
            :disabled="saving || loading"
          >
            {{ saving ? "Enregistrement…" : "Enregistrer la maintenance" }}
          </button>
        </form>
        <section class="care__panel">
          <h2>Maintenances enregistrées</h2>
          <p v-if="!loading && !maintenances.length">Aucune maintenance enregistrée.</p>
          <ol>
            <li
              v-for="entry in maintenances"
              :key="entry.id"
            >
              <strong
                >{{ new Date(entry.performedAt).toLocaleString("fr-FR") }} ·
                {{ entry.performedBy }}</strong
              >
              <p class="care__work">{{ entry.workDescription }}</p>
              <small>{{ entry.resolvedDefectIds.length }} défaut(s) résolu(s)</small>
            </li>
          </ol>
          <nav
            v-if="maintenanceTotal > limit || maintenanceOffset"
            aria-label="Pages des maintenances"
          >
            <button
              type="button"
              :disabled="loading || !maintenanceOffset"
              @click="pageMaintenances(maintenanceOffset - limit)"
            >
              Précédent
            </button>
            <span
              >{{ maintenanceOffset + 1 }}–{{
                Math.min(maintenanceOffset + limit, maintenanceTotal)
              }}
              / {{ maintenanceTotal }}</span
            >
            <button
              type="button"
              :disabled="loading || maintenanceOffset + limit >= maintenanceTotal"
              @click="pageMaintenances(maintenanceOffset + limit)"
            >
              Suivant
            </button>
          </nav>
        </section>
      </div>
    </SlideAndFadeLateral>
  </main>
</template>

<style scoped>
.care {
  display: grid;
  gap: 1.4rem;
  max-width: 65rem;
  min-width: 0;
  margin: auto;
}
.care__stage {
  display: grid;
  gap: 1.4rem;
  min-width: 0;
}
.care__detail-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 1rem;
}
.care__detail-header h1 {
  margin-bottom: 0.35rem;
}
.care__detail-header p {
  margin: 0;
}
.care header p,
.care small {
  color: var(--color-text-secondary);
}

.care__panel {
  display: grid;
  gap: 0.7rem;
  min-width: 0;
  padding: 1.4rem;
  border: 1px solid var(--color-border);
  border-radius: 0.7rem;
  background: var(--color-surface);
  overflow-wrap: anywhere;
}
.care__panel h2 {
  margin: 0;
}
.care__defects,
.care__panel ol {
  padding-left: 1.4rem;
}
.care__defects > li,
.care__panel ol > li {
  padding: 0.8rem 0;
  border-bottom: 1px solid var(--color-border);
}
.care__defects p {
  margin: 0.35rem 0;
}
.care__photos {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin: 0.5rem 0;
}
.care__checkbox {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  margin-bottom: 0.4rem;
}
.care select,
.care input:not([type="checkbox"]),
.care textarea {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  min-height: 2.75rem;
  padding: 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.4rem;
  color: var(--color-text);
  background: var(--color-input-background);
}
.care button {
  box-sizing: border-box;
  justify-self: start;
  min-height: 2.75rem;
  padding: 0.65rem 0.9rem;
  border: 1px solid var(--color-border);
  border-radius: 0.4rem;
  color: var(--color-text);
  background: var(--color-surface);
  cursor: pointer;
}
.care form button {
  color: var(--color-text-on-primary);
  background: var(--color-primary);
  border: 0;
  font-weight: 600;
}
.care button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.care nav {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.6rem;
}
.care__work {
  white-space: pre-wrap;
}
.care__error {
  color: var(--color-danger);
}
.care__success {
  color: var(--color-success);
}
@media (max-width: 42rem) {
  .care__panel {
    padding: 1rem;
  }
  .care select,
  .care input:not([type="checkbox"]),
  .care button {
    min-height: 3rem;
  }
}
</style>
