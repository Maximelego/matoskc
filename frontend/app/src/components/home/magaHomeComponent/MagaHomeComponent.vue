<script setup lang="ts">
import PageHero from "../../common/page/PageHero.vue";
import HomeTilingGrid from "../HomeTilingGrid.vue";
import { AgencyMenus } from "../menus.ts";
</script>

<template>
  <section
    class="home"
    aria-labelledby="maga-home-title"
  >
    <PageHero
      title="Accueil agence"
      description="Choisissez l’opération à réaliser sur le matériel."
      eyebrow="Espace agence"
      title-id="maga-home-title"
    />

    <HomeTilingGrid :tiles="AgencyMenus" />
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
