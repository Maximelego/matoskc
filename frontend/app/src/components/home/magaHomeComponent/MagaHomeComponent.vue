<script setup lang="ts">
import HomeTilingGrid from "../HomeTilingGrid.vue";
import { AgencyMenus } from "../menus.ts";
</script>

<template>
  <section
    class="home"
    aria-labelledby="maga-home-title"
  >
    <header class="home__header">
      <h1 id="maga-home-title">Accueil agence</h1>
      <p>Choisissez l’opération à réaliser sur le matériel.</p>
    </header>

    <HomeTilingGrid :tiles="AgencyMenus" />
  </section>
</template>

<style scoped lang="scss">
.home {
  display: grid;
  gap: 1.5rem;

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
