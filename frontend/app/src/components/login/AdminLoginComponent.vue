<script setup lang="ts">
import { ref } from "vue";
import type { LoginRequestDto } from "../../api/auth";
import BaseButton from "../common/button/BaseButton.vue";

withDefaults(defineProps<{ busy?: boolean; error?: string | null }>(), {
  busy: false,
  error: null,
});
const emit = defineEmits<{ submit: [credentials: Extract<LoginRequestDto, { mode: "admin" }>] }>();
const email = ref("");
const password = ref("");

function submit(): void {
  if (!email.value.trim() || !password.value) return;
  emit("submit", { mode: "admin", email: email.value.trim(), password: password.value });
}
</script>

<template>
  <form
    class="admin-login"
    :aria-busy="busy"
    @submit.prevent="submit"
  >
    <div class="admin-login__heading">
      <h2>Administration</h2>
      <p>Saisissez votre adresse électronique et votre mot de passe.</p>
    </div>

    <div class="admin-login__field">
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

    <div class="admin-login__field">
      <label for="admin-login-password">Mot de passe</label>
      <input
        id="admin-login-password"
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
      class="admin-login__error"
      role="alert"
    >
      {{ error }}
    </p>
    <BaseButton
      type="submit"
      full-width
      :loading="busy"
      >Se connecter</BaseButton
    >
  </form>
</template>

<style scoped lang="scss">
.admin-login {
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
  &__field input {
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
  &__field input:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
    outline-offset: 0.125rem;
  }
  &__field input:disabled {
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
}

@media (max-width: 36rem) {
  .admin-login {
    padding: 1.5rem 1rem 1.25rem;
  }
}
</style>
