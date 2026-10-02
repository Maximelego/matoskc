<script setup lang="ts">
import HomeTile from "../HomeTile.vue";

const menus = [
  {
    title: "État des lieux de départ",
    description: "Préparer la remise du matériel et recueillir la signature.",
    icon: "↗",
    featured: true,
  },
  {
    title: "État des lieux de retour",
    description: "Comparer le matériel à son état de départ.",
    icon: "↙",
  },
  {
    title: "Reprendre un brouillon",
    description: "Poursuivre un état des lieux interrompu.",
    icon: "◷",
  },
  {
    title: "Contrôle interne",
    description: "Vérifier un matériel sans dossier de location.",
    icon: "✓",
  },
  {
    title: "Rechercher un équipement",
    description: "Consulter une fiche ou retrouver un matériel.",
    icon: "⌕",
    to: "/equipments",
  },
  {
    title: "Synchronisation",
    description: "Consulter les dossiers en attente d’envoi.",
    icon: "↻",
  },
] as const;
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
        :title="menu.title"
        :description="menu.description"
        :icon="menu.icon"
        :to="'to' in menu ? menu.to : undefined"
        :featured="'featured' in menu ? menu.featured : false"
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
