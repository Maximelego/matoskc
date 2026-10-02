<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import InspectionComponent from "../components/inspection/InspectionComponent.vue";
import InspectionWizard from "../components/inspection/InspectionWizard.vue";
const route = useRoute();
const equipmentId = computed(() => typeof route.params.equipmentId === "string" ? route.params.equipmentId : null);
</script>
<template>
  <InspectionWizard v-if="equipmentId" :key="equipmentId" :equipment-id="equipmentId" />
  <InspectionComponent v-else />
</template>
