<script setup lang="ts">
import BaseInput from "../common/input/BaseInput.vue";
import { onMounted, ref } from "vue";
import { agenciesApi } from "../../api/agencies";
import type { AgencyDto } from "../../api/agencies/dto";
import type { LoginRequestDto } from "../../api/auth";
import { ApiError } from "../../api/client";
import BaseButton from "../common/button/BaseButton.vue";

const props = withDefaults(defineProps<{ busy?: boolean; error?: string | null }>(), {
  busy: false,
  error: null,
});
const emit = defineEmits<{ submit: [credentials: Extract<LoginRequestDto, { mode: "agency" }>] }>();

const agencies = ref<AgencyDto[]>([]);
const agencyId = ref("");
const agenciesLoading = ref(false);
const agenciesError = ref("");
const password = ref("");
const passwordAttempted = ref(false);

async function loadAgencies(): Promise<void> {
  if (agenciesLoading.value) return;
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
  } catch (cause) {
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

function submit(): void {
  passwordAttempted.value = true;
  if (
    props.busy ||
    agenciesLoading.value ||
    !password.value ||
    !agencies.value.some((agency) => String(agency.code) === agencyId.value)
  )
    return;
  emit("submit", { mode: "agency", agencyId: agencyId.value, password: password.value });
}

onMounted(() => {
  void loadAgencies();
});
</script>

<template>
  <form
    class="agency-login"
    novalidate
    :aria-busy="busy"
    @submit.prevent="submit"
  >
    <div class="agency-login__heading">
      <h2>Espace agence</h2>
      <p>Sélectionnez votre agence et saisissez son mot de passe.</p>
    </div>

    <div class="agency-login__field">
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
        class="agency-login__hint"
        role="status"
      >
        Chargement des agences…
      </p>
      <div
        v-else-if="agenciesError"
        class="agency-login__agency-error"
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
        class="agency-login__agency-error"
        role="status"
      >
        Aucune agence disponible.
      </p>
    </div>

    <div class="agency-login__field">
      <BaseInput
        label="Mot de passe"
        id="agency-login-password"
        v-model="password"
        :error="passwordAttempted && !password ? 'Le mot de passe est obligatoire.' : undefined"
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
      class="agency-login__error"
      role="alert"
    >
      {{ error }}
    </p>
    <BaseButton
      type="submit"
      full-width
      :loading="busy"
      :disabled="agenciesLoading || agencies.length === 0"
    >
      Se connecter
    </BaseButton>
  </form>
</template>

<style scoped lang="scss">
.agency-login {
  display: grid;
  gap: 1.25rem;
  padding: 1.75rem;

  &__heading h2 {
    margin: 0;
    font-size: 1.1rem;
  }
  &__heading p {
    margin: 0.4rem 0 0;
    color: var(--color-text-secondary);
    line-height: 1.5;
  }
  &__field {
    display: grid;
    gap: 0.5rem;
    min-width: 0;
  }
  &__field label {
    font-weight: var(--font-weight-semibold);
  }
  &__field input,
  &__field select {
    width: 100%;
    min-height: 3rem;
    padding: 0.7rem 0.875rem;
    border: 1px solid var(--color-border);
    border-radius: 0.55rem;
    background: var(--color-surface);
    color: var(--color-text);
  }
  &__field input::placeholder {
    color: var(--color-text-secondary);
    opacity: 0.8;
  }
  &__field input:focus-visible,
  &__field select:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
    outline-offset: 0.125rem;
  }
  &__field input:disabled,
  &__field select:disabled {
    opacity: 0.65;
    cursor: not-allowed;
  }
  &__error {
    margin: 0;
    padding: 0.75rem 0.875rem;
    border: 1px solid var(--color-danger);
    border-radius: 0.55rem;
    color: var(--color-danger);
  }
  &__hint {
    margin: 0;
    color: var(--color-text-secondary);
    font-size: var(--font-size-sm);
  }
  &__agency-error {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin: 0;
    color: var(--color-danger);
    font-size: var(--font-size-sm);
  }
  &__agency-error button {
    min-height: 2.75rem;
    padding: 0.35rem 0.6rem;
    border: 1px solid var(--color-border);
    border-radius: 0.4rem;
    background: var(--color-surface);
    color: var(--color-text);
    cursor: pointer;
  }
  &__agency-error button:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
  }
}

@media (max-width: 36rem) {
  .agency-login {
    padding: 1.5rem 1rem 1.25rem;
  }
}
</style>
