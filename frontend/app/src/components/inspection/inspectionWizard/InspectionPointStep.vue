<script setup lang="ts">
import type { DefectDto } from "../../../api/defects/dto";
import type { InspectionTemplateDto } from "../../../api/inspections";
import type { PointValue } from "../InspectionPoint.vue";
import InspectionPoint from "../InspectionPoint.vue";
import TemplateIllustration from "../TemplateIllustration.vue";

defineProps<{
  point: InspectionTemplateDto["points"][number];
  position: number;
  total: number;
  value: PointValue;
  openDefects: DefectDto[];
  inspectionId: string | null;
  disabled: boolean;
}>();
const emit = defineEmits<{ update: [value: PointValue]; removePhoto: [photoId: string] }>();
</script>

<template>
  <section class="point-step">
    <h2>Point {{ position }} sur {{ total }}</h2>
    <p class="point-step__detail">{{ point.descriptionDetail }}</p>
    <TemplateIllustration :photo-id="point.photoId" />
    <InspectionPoint
      :id="point.id"
      :label="point.description"
      :value="value"
      :open-defects="openDefects"
      :inspection-id="inspectionId"
      :disabled="disabled"
      @update="emit('update', $event)"
      @remove-photo="emit('removePhoto', $event)"
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
</style>
