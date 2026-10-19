<script setup lang="ts">
import type { DefectDto } from "../../../api/defects/dto";
import type { ClientInputDto } from "../../../api/inspections";
import InspectionIdentity from "../InspectionIdentity.vue";
import KnownDefectsList from "./KnownDefectsList.vue";

defineProps<{
  operatorFirstName: string;
  client: ClientInputDto;
  departure: boolean;
  locked: boolean;
  defects: DefectDto[];
  equipmentId: string;
}>();
const emit = defineEmits<{
  "update:operatorFirstName": [value: string];
  "update:client": [value: ClientInputDto];
}>();
</script>

<template>
  <div class="identity-step">
    <KnownDefectsList
      :defects="defects"
      :equipment-id="equipmentId"
    />
    <InspectionIdentity
      :operator-first-name="operatorFirstName"
      :client="client"
      :departure="departure"
      :locked="locked"
      @update:operator-first-name="emit('update:operatorFirstName', $event)"
      @update:client="emit('update:client', $event)"
    />
  </div>
</template>

<style scoped lang="scss">
.identity-step {
  display: grid;
  gap: 1.25rem;
  min-width: 0;
}
</style>
