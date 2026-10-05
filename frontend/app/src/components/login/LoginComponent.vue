<script setup lang="ts">
import { defineEmits, defineProps, ref, withDefaults } from "vue";
import type { LoginRequestDto } from "../../api/auth";
import { AGENCIES } from "../../api/auth";
import BaseButton from "../common/button/BaseButton.vue";

const props = withDefaults(
  defineProps<{
    busy?: boolean;
    error?: string | null;
  }>(),
  {
    busy: false,
    error: null,
  },
);

const emit = defineEmits<{
  submit: [credentials: LoginRequestDto];
}>();

type LoginMode = LoginRequestDto["mode"];

const mode = ref<LoginMode>("agency");
const agencyId = ref<string>(AGENCIES[0].id);
const email = ref("");
const password = ref("");
const form = ref<HTMLFormElement | null>(null);

function selectMode(value: LoginMode): void {
  if (props.busy || mode.value === value) return;

  mode.value = value;
  password.value = "";
}

function submit(): void {
  if (props.busy || !password.value) return;

  if (mode.value === "admin") {
    if (!email.value.trim()) return;

    emit("submit", {
      mode: "admin",
      email: email.value.trim(),
      password: password.value,
    });
    return;
  }

  emit("submit", {
    mode: "agency",
    agencyId: agencyId.value,
    password: password.value,
  });
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

      <form
        ref="form"
        class="login__form"
        :aria-busy="busy"
        @submit.prevent="submit"
      >
        <div class="login__form-heading">
          <h2>{{ mode === "agency" ? "Espace agence" : "Administration" }}</h2>
          <p>
            {{
              mode === "agency"
                ? "Sélectionnez votre agence et saisissez son mot de passe."
                : "Saisissez votre adresse électronique et votre mot de passe."
            }}
          </p>
        </div>

        <div
          v-if="mode === 'agency'"
          class="login__field"
        >
          <label for="login-agency">Agence</label>
          <select
            id="login-agency"
            v-model="agencyId"
            name="agency"
            required
            :disabled="busy"
          >
            <option
              v-for="agency in AGENCIES"
              :key="agency.id"
              :value="agency.id"
            >
              {{ agency.label }}
            </option>
          </select>
        </div>

        <div
          v-else
          class="login__field"
        >
          <label for="login-email">Adresse électronique</label>
          <input
            id="login-email"
            v-model="email"
            name="email"
            type="email"
            inputmode="email"
            autocomplete="username"
            placeholder="nom@exemple.fr"
            required
            :disabled="busy"
          />
        </div>

        <div class="login__field">
          <label for="login-password">Mot de passe</label>
          <input
            id="login-password"
            v-model="password"
            name="password"
            type="password"
            autocomplete="current-password"
            placeholder="Saisissez votre mot de passe"
            required
            :disabled="busy"
          />
        </div>

        <p
          v-if="error"
          class="login__error"
          role="alert"
        >
          {{ error }}
        </p>

        <BaseButton
          full-width
          :loading="busy"
          @click="form?.requestSubmit()"
        >
          Se connecter
        </BaseButton>
      </form>
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

.login__form {
  display: grid;
  gap: 1.25rem;
  padding: 1.75rem;
}

.login__form-heading h2 {
  margin: 0;
  font-size: 1.1rem;
}

.login__form-heading p {
  margin: 0.4rem 0 0;
  color: var(--color-text-secondary);
  line-height: 1.5;
}

.login__field {
  display: grid;
  gap: 0.5rem;
}

.login__field label {
  font-weight: var(--font-weight-semibold);
}

.login__field input,
.login__field select {
  box-sizing: border-box;
  width: 100%;
  min-height: 3rem;
  padding: 0.7rem 0.875rem;
  border: 1px solid var(--color-border);
  border-radius: 0.55rem;
  background: var(--color-surface);
  color: var(--color-text);
  font: inherit;
}

.login__field input::placeholder {
  color: var(--color-text-secondary);
  opacity: 0.8;
}

.login__field input:focus-visible,
.login__field select:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
  outline-offset: 0.125rem;
}

.login__field input:disabled,
.login__field select:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.login__error {
  margin: 0;
  padding: 0.75rem 0.875rem;
  border: 1px solid var(--color-danger);
  border-radius: 0.55rem;
  color: var(--color-danger);
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

  .login__form {
    padding: 1.5rem 1rem 1.25rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .login__mode {
    transition: none;
  }
}
</style>
