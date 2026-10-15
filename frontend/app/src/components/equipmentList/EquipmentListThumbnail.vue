<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { api } from "../../api/client";
import BaseIcon from "../common/icon/BaseIcon.vue";

const props = defineProps<{ name: string; thumbnailUrl: string | null }>();
const failed = ref(false);
const source = computed(() => props.thumbnailUrl && !failed.value ? api.url(props.thumbnailUrl) : null);
watch(() => props.thumbnailUrl, () => { failed.value = false; });
</script>
<template>
  <span class="thumbnail">
    <img v-if="source" :src="source" :alt="`Photographie de ${name}`" loading="lazy" @error="failed = true" />
    <BaseIcon v-else name="image" :size="23" />
  </span>
</template>
<style scoped>
.thumbnail { width: 3rem; height: 3rem; flex: none; display: inline-grid; place-items: center;
  overflow: hidden; border-radius: .4rem; background: var(--color-surface-secondary); color: var(--color-text-secondary); }
.thumbnail img { width: 100%; height: 100%; object-fit: cover; }
</style>
