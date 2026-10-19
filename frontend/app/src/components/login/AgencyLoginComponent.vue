<script lang="ts" setup>
import { onMounted } from "vue";

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
  loadAgencies();
});
</script>

<template>
  <div class="">
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
  </div>
</template>

<style lang="scss" scoped></style>
