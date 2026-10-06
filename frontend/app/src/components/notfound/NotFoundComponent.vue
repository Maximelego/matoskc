<script setup lang="ts">
import { RouterLink } from "vue-router";
</script>

<template>
  <main class="not-found">
    <div class="not-found__card">
      <span
        class="not-found__code"
        aria-hidden="true"
        >404</span
      >

      <div class="not-found__content">
        <p class="not-found__eyebrow">Page introuvable</p>
        <h1>Cette page semble avoir disparu.</h1>
        <p class="not-found__description">
          L’adresse est peut-être incorrecte, ou la page a été déplacée. Vous pouvez revenir à
          l’accueil pour poursuivre votre navigation.
        </p>

        <RouterLink
          class="not-found__button"
          to="/"
        >
          Retour à l’accueil
        </RouterLink>
      </div>
    </div>
  </main>
</template>

<style scoped lang="scss">
.not-found {
  display: grid;
  place-items: center;
  min-height: min(75dvh, 48rem);
  padding: 2rem 1rem;
}

.not-found__card {
  position: relative;
  box-sizing: border-box;
  width: min(100%, 42rem);
  overflow: hidden;
  padding: clamp(2rem, 6vw, 4rem);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background: var(--color-surface);
  box-shadow: 0 1rem 3rem var(--color-shadow);
}

.not-found__code {
  position: absolute;
  top: -0.35em;
  right: -0.08em;
  color: var(--color-primary-soft);
  font-size: clamp(9rem, 35vw, 18rem);
  font-weight: 800;
  line-height: 1;
  letter-spacing: -0.1em;
  user-select: none;
  pointer-events: none;
}

.not-found__content {
  position: relative;
  max-width: 29rem;
}

.not-found__eyebrow {
  margin: 0 0 0.75rem;
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
}

.not-found h1 {
  margin: 0;
  color: var(--color-text);
  font-size: clamp(2rem, 6vw, 3rem);
  line-height: 1.15;
}

.not-found__description {
  margin: 1.25rem 0 2rem;
  color: var(--color-text-secondary);
  line-height: 1.6;
}

.not-found__button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 2.75rem;
  padding: 0.625rem 1.25rem;
  border: 1px solid var(--color-primary);
  border-radius: 0.5rem;
  background: var(--color-primary);
  color: var(--color-text-on-primary);
  font-weight: var(--font-weight-semibold);
  text-align: center;
  text-decoration: none;
  transition: background-color 150ms ease;
}

.not-found__button:hover {
  background: var(--color-primary-hover);
}

.not-found__button:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
  outline-offset: 0.1875rem;
}

@media (max-width: 36rem) {
  .not-found__code {
    top: -0.15em;
    right: 0;
    font-size: 10rem;
  }

  .not-found__description {
    margin-bottom: 1.5rem;
  }

  .not-found__button {
    box-sizing: border-box;
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .not-found__button {
    transition: none;
  }
}
</style>
