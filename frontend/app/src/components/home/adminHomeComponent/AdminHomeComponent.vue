<script setup lang="ts">
import HomeTilingGrid from "../HomeTilingGrid.vue";
import { AdminMenus } from "../menus.ts";
</script>

<template>
  <section
    class="home"
    aria-labelledby="admin-home-title"
  >
    <header class="home__header">
      <h1 id="admin-home-title">Tableau de bord</h1>
      <p>Accédez au suivi du matériel et aux fonctions de gestion.</p>
    </header>

    <HomeTilingGrid :tiles="AdminMenus" />
  </section>
</template>

<style scoped lang="scss">
.home {
  gap: 1.5rem;
  display: flex;
  flex-direction: row;

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
