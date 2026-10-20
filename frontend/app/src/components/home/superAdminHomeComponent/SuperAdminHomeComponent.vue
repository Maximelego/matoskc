<script setup lang="ts">
import HomeTilingGrid from "../HomeTilingGrid.vue";
import { AdminMenus, AgencyMenus, SuperAdminMenus } from "../menus.ts";
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
      <HomeTilingGrid :tiles="AgencyMenus" />
    </section>

    <section
      class="super-admin-home__section"
      aria-labelledby="admin-menus-title"
    >
      <div class="super-admin-home__section-heading">
        <h2 id="admin-menus-title">Menus Admin</h2>
        <p>Suivi des équipements et des états des lieux.</p>
      </div>
      <HomeTilingGrid :tiles="AdminMenus" />
    </section>

    <section
      class="super-admin-home__section"
      aria-labelledby="super-admin-menus-title"
    >
      <div class="super-admin-home__section-heading">
        <h2 id="super-admin-menus-title">Menus SuperAdmin</h2>
        <p>Gestion des agences et des accès à l’application.</p>
      </div>
      <HomeTilingGrid :tiles="SuperAdminMenus" />
    </section>
  </main>
</template>

<style scoped lang="scss">
.super-admin-home {
  display: grid;
  gap: 2.5rem;
  min-width: 0;
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
  color: var(--color-secondary);
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
@media (max-width: 42rem) {
  .super-admin-home {
    gap: 2rem;
  }
}
</style>
