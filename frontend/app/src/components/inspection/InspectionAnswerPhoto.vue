<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { inspectionHttpApi } from "../../api/inspections";
const props = defineProps<{ inspectionId: string; photoId: string }>();
const url = ref("");
let sequence = 0;
function release() { if (url.value) URL.revokeObjectURL(url.value); url.value = ""; }
watch(() => [props.inspectionId, props.photoId] as const, async ([inspectionId, photoId]) => {
  const token = ++sequence; release();
  try {
    const blob = await inspectionHttpApi.getPhoto(inspectionId, photoId);
    if (token === sequence) url.value = URL.createObjectURL(blob);
  } catch { /* Le lien texte reste utilisable si la miniature échoue. */ }
}, { immediate: true });
onUnmounted(() => { ++sequence; release(); });
</script>
<template><img v-if="url" :src="url" alt="Photographie enregistrée pour cette réponse" class="answer-photo" /></template>
<style scoped>.answer-photo { width: 5rem; height: 5rem; object-fit: cover; border-radius: .4rem; }</style>
