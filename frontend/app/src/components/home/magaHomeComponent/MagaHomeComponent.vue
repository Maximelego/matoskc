<script setup lang="ts">
import HomeTile, { type HomeTileProps } from "../HomeTile.vue";

const menus = [
  {
    title: "Etats des Lieux",
    description: "Liste des Etats des Lieux.",
    icon: "clipboard",
    to: "/inspections",
  },
] as HomeTileProps[];
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

    <div class="home__grid">
      <HomeTile
        v-for="menu in menus"
        :key="menu.title"
        v-bind="menu"
      />
    </div>
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

  &__grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(min(100%, 17rem), 1fr));
    gap: 1rem;
  }
}

@media (max-width: 42rem) {
  .home {
    gap: 1rem;

    &__grid {
      grid-template-columns: minmax(0, 1fr);
      gap: 0.75rem;
    }
  }
}
</style>
