<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { defectsApi } from "../../api/defects";
const props = defineProps<{ equipmentId: string; defectId: string; photoId: string }>();
const url = ref("");
let sequence = 0;
function clear() { if (url.value) URL.revokeObjectURL(url.value); url.value = ""; }
watch(() => [props.equipmentId, props.defectId, props.photoId] as const, async ([equipmentId, defectId, photoId]) => {
  const token = ++sequence; clear();
  try {
    const blob = await defectsApi.getPhoto(equipmentId, defectId, photoId);
    if (token === sequence) url.value = URL.createObjectURL(blob);
  } catch { /* La description reste accessible si la photographie manque. */ }
}, { immediate: true });
onUnmounted(() => { ++sequence; clear(); });
</script>
<template><a v-if="url" :href="url" target="_blank" rel="noopener noreferrer">
  <img :src="url" alt="Photographie du défaut" class="defect-photo" /></a></template>
<style scoped>.defect-photo { width: 5rem; height: 5rem; object-fit: cover; border-radius: .4rem; }</style>
