<script setup lang="ts">
import type { ClientInputDto } from "../../../api/inspections";
import InspectionIdentity from "../InspectionIdentity.vue";

defineProps<{
  operatorFirstName: string;
  client: ClientInputDto;
  departure: boolean;
  locked: boolean;
  expectedAgencyCode: number | null;
  validationRequested: boolean;
}>();
const emit = defineEmits<{
  "update:operatorFirstName": [value: string];
  "update:client": [value: ClientInputDto];
}>();
</script>

<template>
  <div class="identity-step">
    <InspectionIdentity
      :operator-first-name="operatorFirstName"
      :client="client"
      :departure="departure"
      :locked="locked"
      :expected-agency-code="expectedAgencyCode"
      :validation-requested="validationRequested"
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
