<script setup lang="ts">
import HomeTile, { type HomeTileProps } from "../HomeTile.vue";

const agencyMenus: HomeTileProps[] = [
  {
    title: "États des lieux",
    description: "Réaliser un état des lieux pour un équipement.",
    icon: "clipboard",
    to: "/inspections",
  },
];

const adminMenus: HomeTileProps[] = [
  {
    title: "Équipements",
    description: "Consulter et gérer le matériel enregistré.",
    icon: "equipment",
    to: "/equipments",
  },
  {
    title: "Suivi des états des lieux",
    description: "Consulter les états des lieux réalisés.",
    icon: "document",
    to: "/admin/inspections",
  },
];

const superAdminMenus: HomeTileProps[] = [
  {
    title: "Gestion des agences",
    description: "Consulter et administrer les agences.",
    icon: "home",
  },
  {
    title: "Gestion des utilisateurs",
    description: "Administrer les comptes et leurs accès.",
    icon: "settings",
  },
];
</script>

<template>
  <main class="super-admin-home">
    <header class="super-admin-home__header">
      <p class="super-admin-home__eyebrow">Super administration</p>
      <h1>Tableau de bord</h1>
      <p>Accédez aux opérations de l’agence et aux outils d’administration.</p>
    </header>

    <section
      class="super-admin-home__section"
      aria-labelledby="agency-menus-title"
    >
      <div class="super-admin-home__section-heading">
        <h2 id="agency-menus-title">Menus Agence</h2>
        <p>Opérations quotidiennes sur le matériel.</p>
      </div>

      <div class="super-admin-home__grid">
        <HomeTile
          v-for="menu in agencyMenus"
          :key="menu.title"
          v-bind="menu"
        />
      </div>
    </section>

    <section
      class="super-admin-home__section"
      aria-labelledby="admin-menus-title"
    >
      <div class="super-admin-home__section-heading">
        <h2 id="admin-menus-title">Menus Admin</h2>
        <p>Suivi des équipements et des états des lieux.</p>
      </div>

      <div class="super-admin-home__grid">
        <HomeTile
          v-for="menu in adminMenus"
          :key="menu.title"
          v-bind="menu"
        />
      </div>
    </section>

    <section
      class="super-admin-home__section"
      aria-labelledby="super-admin-menus-title"
    >
      <div class="super-admin-home__section-heading">
        <h2 id="super-admin-menus-title">Menus SuperAdmin</h2>
        <p>Gestion des agences et des accès à l’application.</p>
      </div>

      <div class="super-admin-home__grid">
        <HomeTile
          v-for="menu in superAdminMenus"
          :key="menu.title"
          v-bind="menu"
        />
      </div>
    </section>
  </main>
</template>

<style scoped lang="scss">
.super-admin-home {
  display: grid;
  gap: 2.5rem;
}

.super-admin-home__header {
  h1 {
    margin: 0;
  }

  > p:last-child {
    margin: 0.5rem 0 0;
    color: var(--color-text-secondary);
  }
}

.super-admin-home__eyebrow {
  margin: 0 0 0.4rem;
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.super-admin-home__section {
  display: grid;
  gap: 1rem;
  min-width: 0;
}

.super-admin-home__section + .super-admin-home__section {
  padding-top: 1.75rem;
  border-top: 1px solid var(--color-border);
}

.super-admin-home__section-heading {
  h2 {
    margin: 0;
    font-size: 1.25rem;
  }

  p {
    margin: 0.35rem 0 0;
    color: var(--color-text-secondary);
  }
}

.super-admin-home__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 17rem), 1fr));
  gap: 1rem;
}

@media (max-width: 42rem) {
  .super-admin-home {
    gap: 2rem;
  }

  .super-admin-home__grid {
    grid-template-columns: minmax(0, 1fr);
    gap: 0.75rem;
  }
}
</style>
