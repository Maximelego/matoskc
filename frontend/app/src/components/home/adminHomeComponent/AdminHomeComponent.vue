<script setup lang="ts">
import PageHero from "../../common/page/PageHero.vue";
import HomeTilingGrid from "../HomeTilingGrid.vue";
import { AdminMenus } from "../menus.ts";
</script>

<template>
  <section
    class="home"
    aria-labelledby="admin-home-title"
  >
    <PageHero
      title="Tableau de bord"
      description="Accédez au suivi du matériel et aux fonctions de gestion."
      eyebrow="Administration"
      title-id="admin-home-title"
    />

    <HomeTilingGrid :tiles="AdminMenus" />
  </section>
</template>

<style scoped lang="scss">
.home {
  display: grid;
  gap: 1.5rem;
  min-width: 0;

  &__header {
    h1 {
      margin-bottom: 0.35rem;
    }

    p {
      margin: 0;
      color: var(--color-text-secondary);
    }
  }
}

@media (max-width: 42rem) {
  .home {
    gap: 1rem;
  }
}
</style>
