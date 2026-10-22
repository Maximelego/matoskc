<script setup lang="ts">
import type { DefectDto } from "../../../api/defects/dto";
import DefectPhoto from "../../maintenance/DefectPhoto.vue";

defineProps<{ defects: DefectDto[]; equipmentId: string }>();
</script>

<template>
  <section
    v-if="defects.length"
    class="known-defects"
    aria-label="Défauts ouverts"
  >
    <h2>Défauts Connus</h2>
    <ul>
      <li
        v-for="defect in defects"
        :key="defect.id"
      >
        <strong>{{ defect.description }}</strong> : {{ defect.observation }}
        <small
          >Constaté par {{ defect.operatorFirstName }} le
          {{ new Date(defect.reportedAt).toLocaleDateString("fr-FR") }}</small
        >
        <details v-if="defect.occurrences.length">
          <summary>Constats et photographies</summary>
          <ul>
            <li
              v-for="occurrence in defect.occurrences"
              :key="occurrence.id"
            >
              {{ occurrence.observation }} ·
              {{ new Date(occurrence.observedAt).toLocaleDateString("fr-FR") }} · constaté par
              {{ occurrence.operatorFirstName }}
              <div class="known-defects__photos">
                <DefectPhoto
                  v-for="photoId in occurrence.photoIds"
                  :key="photoId"
                  :equipment-id="equipmentId"
                  :defect-id="defect.id"
                  :photo-id="photoId"
                />
              </div>
            </li>
          </ul>
        </details>
      </li>
    </ul>
  </section>
</template>

<style scoped lang="scss">
.known-defects {
  padding: 1rem;
  border-radius: 0.6rem;
  background: var(--color-warning-soft);
  overflow-wrap: anywhere;
}
.known-defects li {
  margin-block: 0.6rem;
}
.known-defects small {
  display: block;
}
.known-defects__photos {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-block: 0.5rem;
}
</style>
