<script setup lang="ts">
import BaseSpinner from "../common/spinner/BaseSpinner.vue";
defineProps<{
  photoIds: string[];
  photos: Record<string, string>;
  label: string;
  loading?: boolean;
}>();
</script>

<template>
  <div
    v-if="photoIds.length"
    class="evidence-photos"
    :aria-label="label"
  >
    <template
      v-for="(photoId, index) in photoIds"
      :key="photoId"
    >
      <a
        v-if="photos[photoId]"
        :href="photos[photoId]"
        target="_blank"
        rel="noopener noreferrer"
        :aria-label="`${label}, photo ${index + 1}`"
      >
        <img
          :src="photos[photoId]"
          :alt="`${label}, photo ${index + 1}`"
          loading="lazy"
        />
      </a>
      <span v-else class="evidence-photos__missing">
        <BaseSpinner
          v-if="loading"
          size="small"
          :label="`Chargement de ${label.toLowerCase()}, photo ${index + 1}`"
        />
        <template v-else>Photo indisponible</template>
      </span>
    </template>
  </div>
</template>

<style scoped lang="scss">
.evidence-photos {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.6rem;

  a,
  &__missing {
    display: grid;
    place-items: center;
    width: 5.5rem;
    height: 5.5rem;
    border: 1px solid var(--color-border);
    border-radius: 0.55rem;
    overflow: hidden;
    color: var(--color-text-secondary);
    background: var(--color-surface-secondary);
    font-size: var(--font-size-sm);
    text-align: center;
  }
  a:focus-visible {
    outline: 3px solid var(--color-focus);
    outline-offset: 2px;
  }
  img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }
}
</style>
