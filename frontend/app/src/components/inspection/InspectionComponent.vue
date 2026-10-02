<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import InspectionWizard from "./InspectionWizard.vue";

const route = useRoute();
const equipmentId = computed(() =>
  typeof route.params.equipmentId === "string" ? route.params.equipmentId : undefined,
);
</script>
<template>
  <InspectionWizard
    :key="equipmentId ?? 'selection'"
    :equipment-id="equipmentId"
  />
</template>
