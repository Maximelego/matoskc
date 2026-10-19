<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import type { LoginRequestDto } from "../../api/auth";
import { authSession } from "../../api/auth/session";
import { ApiError } from "../../api/client";
import SlideAndFadeLateral from "../common/transition/SlideAndFadeLateral.vue";
import AdminLoginComponent from "./AdminLoginComponent.vue";
import AgencyLoginComponent from "./AgencyLoginComponent.vue";

type LoginMode = LoginRequestDto["mode"];
const mode = ref<LoginMode>("agency");
const route = useRoute();
const router = useRouter();
const busy = ref(false);
const error = ref<string | null>(null);
const displayError = computed(
  () =>
    error.value ??
    (route.query.unavailable === "1"
      ? "Le serveur est momentanément inaccessible. Vous pouvez réessayer de vous connecter."
      : null),
);

function selectMode(value: LoginMode): void {
  if (busy.value || mode.value === value) return;
  mode.value = value;
  error.value = null;
}

async function submit(credentials: LoginRequestDto): Promise<void> {
  if (busy.value) return;
  busy.value = true;
  error.value = null;
  try {
    await authSession.login(credentials);
    const redirect = route.query.redirect;
    const destination =
      typeof redirect === "string" && redirect.startsWith("/") && !redirect.startsWith("//")
        ? redirect
        : "/";
    await router.replace(destination);
  } catch (cause) {
    if (cause instanceof ApiError) {
      error.value =
        cause.status === 401
          ? "Identifiants incorrects. Veuillez réessayer."
          : cause.status === 429
            ? "Trop de tentatives de connexion. Veuillez patienter avant de réessayer."
            : cause.status >= 500
              ? "Le serveur est momentanément indisponible. Veuillez réessayer."
              : cause.status === 400
                ? "La demande de connexion est invalide. Veuillez vérifier les informations saisies."
                : "La connexion a échoué. Veuillez réessayer.";
    } else error.value = "Impossible de joindre le serveur. Veuillez vérifier votre connexion.";
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <main class="login">
    <section
      class="login__card"
      aria-labelledby="login-title"
    >
      <header class="login__header">
        <div class="login__brand">M</div>

        <div>
          <p class="login__eyebrow">MatosKC</p>
          <h1 id="login-title">Bienvenue</h1>
          <p class="login__intro">Choisissez votre espace pour vous connecter.</p>
        </div>
      </header>

      <div
        class="login__modes"
        role="group"
        aria-label="Mode de connexion"
      >
        <button
          type="button"
          class="login__mode"
          :class="{ 'login__mode--active': mode === 'agency' }"
          :aria-pressed="mode === 'agency'"
          :disabled="busy"
          @click="selectMode('agency')"
        >
          <span class="login__mode-label">Connexion</span>
          <strong>Agence</strong>
        </button>

        <button
          type="button"
          class="login__mode"
          :class="{ 'login__mode--active': mode === 'admin' }"
          :aria-pressed="mode === 'admin'"
          :disabled="busy"
          @click="selectMode('admin')"
        >
          <span class="login__mode-label">Connexion</span>
          <strong>Admin</strong>
        </button>
      </div>
      <SlideAndFadeLateral>
        <AdminLoginComponent
          v-if="mode === 'admin'"
          :busy="busy"
          :error="displayError"
          @submit="submit"
        />
        <AgencyLoginComponent
          v-else
          :busy="busy"
          :error="displayError"
          @submit="submit"
        />
      </SlideAndFadeLateral>
    </section>
  </main>
</template>

<style scoped lang="scss">
.login {
  display: grid;
  place-items: center;
  min-height: min(80dvh, 52rem);
  padding: 2rem 1rem;
}

.login__card {
  box-sizing: border-box;
  width: min(100%, 32rem);
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background: var(--color-surface);
  color: var(--color-text);
  box-shadow: 0 1.25rem 3.5rem var(--color-shadow);
}

.login__header {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.75rem 1.75rem 1.5rem;
}

.login__brand {
  display: grid;
  place-items: center;
  flex: none;
  width: 3rem;
  height: 3rem;
  border-radius: 0.85rem;
  background: var(--color-primary);
  color: var(--color-text-on-primary);
  font-size: 1.5rem;
  font-weight: var(--font-weight-semibold);
}

.login__eyebrow {
  margin: 0 0 0.2rem;
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.login__header h1 {
  margin: 0;
  font-size: 1.5rem;
  line-height: 1.2;
}

.login__intro {
  margin: 0.4rem 0 0;
  color: var(--color-text-secondary);
  line-height: 1.4;
}

.login__modes {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin: 0 1.75rem;
  gap: 0.5rem;
  padding: 0.3rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface-secondary);
}

.login__mode {
  display: grid;
  gap: 0.1rem;
  min-width: 0;
  min-height: 4.5rem;
  padding: 0.65rem 1rem;
  border: 1px solid transparent;
  border-radius: 0.55rem;
  background: transparent;
  color: var(--color-text-secondary);
  text-align: left;
  font: inherit;
  cursor: pointer;
  transition:
    background-color 150ms ease,
    color 150ms ease,
    box-shadow 150ms ease;
}

.login__mode:hover:not(:disabled):not(.login__mode--active) {
  background: var(--color-surface-hover);
  color: var(--color-text);
}

.login__mode--active {
  border-color: var(--color-border);
  background: var(--color-surface);
  color: var(--color-text);
  box-shadow: 0 0.2rem 0.65rem var(--color-shadow);
}

.login__mode--active strong {
  color: var(--color-primary);
}

.login__mode-label {
  font-size: var(--font-size-sm);
}

.login__mode strong {
  font-size: 1.1rem;
  font-weight: var(--font-weight-semibold);
}

.login__mode:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
  outline-offset: 0.125rem;
}

.login__mode:disabled {
  cursor: not-allowed;
}

@media (max-width: 36rem) {
  .login {
    padding: 1rem 0.75rem;
  }

  .login__header {
    padding: 1.25rem 1rem;
  }

  .login__modes {
    margin: 0 1rem;
  }

  .login__mode {
    padding: 0.6rem 0.7rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .login__mode {
    transition: none;
  }
}
</style>
