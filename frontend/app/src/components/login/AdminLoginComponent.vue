<script setup lang="ts">
import BaseInput from "../common/input/BaseInput.vue";
import { computed, ref } from "vue";
import { validateEmail } from "../../utils/validators";
import type { LoginRequestDto } from "../../api/auth";
import BaseButton from "../common/button/BaseButton.vue";

withDefaults(defineProps<{ busy?: boolean; error?: string | null }>(), {
  busy: false,
  error: null,
});
const emit = defineEmits<{ submit: [credentials: Extract<LoginRequestDto, { mode: "admin" }>] }>();
const email = ref("");
const password = ref("");
const attempted = ref(false);
const emailTouched = ref(false);
const emailError = computed(() =>
  attempted.value || emailTouched.value ? validateEmail(email.value) : undefined,
);
const passwordError = computed(() =>
  attempted.value && !password.value ? "Le mot de passe est obligatoire." : undefined,
);

function submit(): void {
  attempted.value = true;
  if (emailError.value || passwordError.value) return;
  emit("submit", { mode: "admin", email: email.value.trim(), password: password.value });
}
</script>

<template>
  <form
    class="admin-login"
    novalidate
    :aria-busy="busy"
    @submit.prevent="submit"
  >
    <div class="admin-login__heading">
      <h2>Administration</h2>
      <p>Saisissez votre adresse électronique et votre mot de passe.</p>
    </div>

    <div class="admin-login__field">
      <BaseInput
        label="Adresse électronique"
        id="login-email"
        v-model="email"
        :error="emailError"
        @blur="emailTouched = true"
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
      <BaseInput
        label="Mot de passe"
        id="admin-login-password"
        v-model="password"
        :error="passwordError"
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
      variant="secondary"
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
