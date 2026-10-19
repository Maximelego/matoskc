<script lang="ts" setup>
import BaseButton from "../common/button/BaseButton.vue";


</script>

<template>
  <div class="">
    <div class="login__form-heading">
      <h2>Administration</h2>
      <p>Saisissez votre adresse électronique et votre mot de passe.</p>
    </div>
    <form
        class="login__form"
        :aria-busy="busy"
        @submit.prevent="submit"
      >
        <div
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
        >
          Se connecter
        </BaseButton>
      </form>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
