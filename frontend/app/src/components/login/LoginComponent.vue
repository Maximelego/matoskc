<script setup lang="ts">
import { defineEmits, defineProps, onMounted, ref, withDefaults } from "vue";
import { agenciesApi } from "../../api/agencies";
import type { AgencyDto } from "../../api/agencies/dto";
import type { LoginRequestDto } from "../../api/auth";
import { ApiError } from "../../api/client";
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
const agencyId = ref("");
const agencies = ref<AgencyDto[]>([]);
const agenciesLoading = ref(false);
const agenciesError = ref("");
const email = ref("");
const password = ref("");

async function loadAgencies(): Promise<void> {
  agenciesLoading.value = true;
  agenciesError.value = "";

  try {
    const result = await agenciesApi.list();
    agencies.value = result.agencies;

    if (!agencies.value.some((agency) => String(agency.code) === agencyId.value)) {
      agencyId.value = String(
        agencies.value.find((agency) => agency.code === 83)?.code ?? agencies.value[0]?.code ?? "",
      );
    }
  } catch (cause: unknown) {
    agencies.value = [];
    agencyId.value = "";

    agenciesError.value =
      cause instanceof ApiError && cause.status >= 500
        ? "Le serveur ne peut pas fournir la liste des agences pour le moment."
        : "Impossible de charger les agences. Veuillez réessayer.";
  } finally {
    agenciesLoading.value = false;
  }
}

onMounted(() => {
  void loadAgencies();
});

function selectMode(value: LoginMode): void {
  if (props.busy || mode.value === value) return;

  mode.value = value;
  password.value = "";
}

function submit(event: Event): void {
  if (props.busy) return;

  const form = event.target as HTMLFormElement;
  const fields = new FormData(form);
  const enteredPassword = String(fields.get("password") ?? "");

  if (!enteredPassword) return;

  if (mode.value === "admin") {
    const enteredEmail = String(fields.get("username") ?? "").trim();

    if (!enteredEmail) return;

    emit("submit", {
      mode: "admin",
      email: enteredEmail,
      password: enteredPassword,
    });

    return;
  }

  if (
    agenciesLoading.value ||
    !agencies.value.some((agency) => String(agency.code) === agencyId.value)
  ) {
    return;
  }

  emit("submit", {
    mode: "agency",
    agencyId: agencyId.value,
    password: enteredPassword,
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
            :disabled="busy || agenciesLoading || agencies.length === 0"
          >
            <option
              v-for="agency in agencies"
              :key="agency.id"
              :value="String(agency.code)"
            >
              {{ agency.name }} ({{ agency.code }})
            </option>
          </select>

          <p
            v-if="agenciesLoading"
            class="login__hint"
            role="status"
          >
            Chargement des agences…
          </p>

          <div
            v-else-if="agenciesError"
            class="login__agency-error"
            role="alert"
          >
            <span>{{ agenciesError }}</span>
            <button
              type="button"
              :disabled="busy"
              @click="loadAgencies"
            >
              Réessayer
            </button>
          </div>

          <p
            v-else-if="agencies.length === 0"
            class="login__agency-error"
            role="status"
          >
            Aucune agence disponible.
          </p>
        </div>

        <div
          v-else
          class="login__field"
        >
          <label for="login-email">Adresse électronique</label>
          <input
            id="login-email"
            v-model="email"
            name="username"
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
          type="submit"
          full-width
          :loading="busy"
          :disabled="mode === 'agency' && (agenciesLoading || agencies.length === 0)"
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

.login__hint {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.login__agency-error {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin: 0;
  color: var(--color-danger);
  font-size: var(--font-size-sm);
}

.login__agency-error button {
  padding: 0.35rem 0.6rem;
  border: 1px solid var(--color-border);
  border-radius: 0.4rem;
  background: var(--color-surface);
  color: var(--color-text);
  font: inherit;
  cursor: pointer;
}

.login__agency-error button:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
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
