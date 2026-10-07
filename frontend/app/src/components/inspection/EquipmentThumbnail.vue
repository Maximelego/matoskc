<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { equipmentPhotosApi } from "../../api/equipments/photos";
import BaseIcon from "../common/icon/BaseIcon.vue";
const props = defineProps<{ equipmentId: string }>();
const url = ref("");
let active = 0;
watch(() => props.equipmentId, async id => {
  const version = ++active;
  if (url.value) URL.revokeObjectURL(url.value);
  url.value = "";
  try {
    const photo = await equipmentPhotosApi.get(id);
    if (version === active) url.value = URL.createObjectURL(photo);
  } catch { /* An absent or unavailable photo uses the placeholder. */ }
}, { immediate: true });
onUnmounted(() => { active++; if (url.value) URL.revokeObjectURL(url.value); });
</script>
<template>
  <span class="thumbnail">
    <img v-if="url" :src="url" alt="" />
    <BaseIcon v-else name="equipment" :size="42" />
  </span>
</template>
<style scoped lang="scss">
.thumbnail { display: grid; place-items: center; width: 100%; aspect-ratio: 4 / 3; overflow: hidden; border-radius: .5rem; color: var(--color-text-secondary); background: var(--color-surface-secondary); }
.thumbnail img { width: 100%; height: 100%; object-fit: cover; }
</style>
