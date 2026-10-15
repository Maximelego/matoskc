<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { inspectionTemplatesApi } from "../../api/inspection-templates";
const props = defineProps<{ photoId: string | null }>();
const url = ref("");
let sequence = 0;
function release() { if (url.value) URL.revokeObjectURL(url.value); url.value = ""; }
watch(() => props.photoId, async id => {
  const token = ++sequence; release();
  if (!id) return;
  try {
    const blob = await inspectionTemplatesApi.getPhoto(id);
    if (token === sequence) url.value = URL.createObjectURL(blob);
  } catch { /* L'état des lieux reste utilisable sans illustration. */ }
}, { immediate: true });
onUnmounted(() => { ++sequence; release(); });
</script>
<template><img v-if="url" :src="url" alt="Illustration du point de contrôle" class="illustration" /></template>
<style scoped>.illustration { display: block; max-width: 100%; max-height: 18rem; object-fit: contain; border-radius: .5rem; }</style>
