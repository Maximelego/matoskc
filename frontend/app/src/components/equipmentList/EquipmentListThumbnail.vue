<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import { equipmentPhotosApi } from "../../api/equipments/photos";
import BaseIcon from "../common/icon/BaseIcon.vue";

const props = defineProps<{ equipmentId: string; name: string }>();
const element = ref<HTMLElement | null>(null);
const url = ref<string | null>(null);
let observer: IntersectionObserver | null = null;
let disposed = false;

async function load(): Promise<void> {
  try {
    const metadata = await equipmentPhotosApi.list(props.equipmentId);
    const first = metadata.sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc))[0];
    if (!first || disposed) return;
    const blob = await equipmentPhotosApi.download(props.equipmentId, first.id);
    if (disposed) return;
    if (blob.type.startsWith("image/")) url.value = URL.createObjectURL(blob);
  } catch { /* La miniature absente ou inaccessible affiche l’icône de remplacement. */ }
}
onMounted(() => {
  if (!element.value) return;
  if (!("IntersectionObserver" in window)) { void load(); return; }
  observer = new IntersectionObserver(entries => {
    if (entries.some(entry => entry.isIntersecting)) { observer?.disconnect(); void load(); }
  }, { rootMargin: "150px" });
  observer.observe(element.value);
});
onUnmounted(() => { disposed = true; observer?.disconnect(); if (url.value) URL.revokeObjectURL(url.value); });
</script>
<template>
  <span ref="element" class="thumbnail">
    <img v-if="url" :src="url" :alt="`Photographie de ${name}`" loading="lazy" />
    <BaseIcon v-else name="image" :size="23" />
  </span>
</template>
<style scoped>
.thumbnail { width: 3rem; height: 3rem; flex: none; display: inline-grid; place-items: center;
  overflow: hidden; border-radius: .4rem; background: var(--color-surface-secondary); color: var(--color-text-secondary); }
.thumbnail img { width: 100%; height: 100%; object-fit: cover; }
</style>
