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
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;

  
}
</style>
