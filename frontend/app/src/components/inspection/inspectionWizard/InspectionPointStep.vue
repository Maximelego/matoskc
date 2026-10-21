<script setup lang="ts">
import type { InspectionTemplateDto } from "../../../api/inspections";
import type { PointValue } from "../InspectionPoint.vue";
import InspectionPoint from "../InspectionPoint.vue";
import TemplateIllustration from "../TemplateIllustration.vue";

defineProps<{
  point: InspectionTemplateDto["points"][number];
  position: number;
  total: number;
  value: PointValue;
  inspectionId: string | null;
  disabled: boolean;
}>();
const emit = defineEmits<{ update: [value: PointValue]; removePhoto: [findingId: string, photoId: string] }>();
</script>

<template>
  <section class="point-step">
    <div class="point-step__description">
      <div>
        <h2>Point {{ position }} sur {{ total }}</h2>
        <p class="point-step__detail">{{ point.descriptionDetail }}</p>
      </div>
      <TemplateIllustration :photo-id="point.photoId" />
    </div>
    <InspectionPoint
      :id="point.id"
      :label="point.description"
      :value="value"
      :inspection-id="inspectionId"
      :disabled="disabled"
      @update="emit('update', $event)"
      @remove-photo="(findingId, photoId) => emit('removePhoto', findingId, photoId)"
    />
  </section>
</template>

<style scoped lang="scss">
.point-step {
  display: grid;
  gap: 1rem;
  min-width: 0;
}
.point-step__detail {
  white-space: pre-wrap;
  tab-size: 4;
  overflow-wrap: anywhere;
}
.point-step__description {
  display: flex;
  flex-direction: row;
  justify-content: space-between;

  gap: 1rem;
}
</style>
