<script lang="ts" setup>
import type { HomeTileProps } from "./HomeTile.vue";
import HomeTile from "./HomeTile.vue";

defineProps<{
  tiles: HomeTileProps[];
}>();
</script>

<template>
  <div class="home-tiling-grid">
    <HomeTile
      v-for="tile in tiles"
      :key="tile.title"
      v-bind="tile"
    />
  </div>
</template>

<style scoped lang="scss">
.home-tiling-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 16rem), 1fr));
  gap: 1rem;
  min-width: 0;
}

@media (max-width: 42rem) {
  .home-tiling-grid {
    gap: 0.75rem;
  }
}
</style>
