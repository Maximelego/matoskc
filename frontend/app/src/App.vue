<script setup lang="ts">
import BaseForm from './components/common/form/BaseForm.vue';
import AppLayout from './components/layout/AppLayout.vue';

import { ref } from "vue";

import type {
  FormFieldDefinition,
  FormValues,
} from "./components/common/form/form.types.ts"

interface LoginCredentials {
  email: string;
  password: string;
}

defineProps<{
  loading?: boolean;
}>();

const emit = defineEmits<{
  submit: [credentials: LoginCredentials];
}>();

const values = ref<FormValues>({
  email: "",
  password: "",
});

const fields = [
  {
    name: "email",
    label: "Adresse e-mail",
    type: "email",
    autocomplete: "username",
    inputmode: "email",
    placeholder: "nom@exemple.fr",
    rules: [
      {
        type: "required",
        message: "L’adresse e-mail est obligatoire.",
      },
      {
        type: "email",
      },
    ],
  },
  {
    name: "password",
    label: "Mot de passe",
    type: "password",
    autocomplete: "current-password",
    rules: [
      {
        type: "required",
        message: "Le mot de passe est obligatoire.",
      },
      {
        type: "minLength",
        value: 8,
      },
      {
        type: "pattern",
        value: /[A-Z]/,
        message: "Le mot de passe doit contenir une majuscule.",
      },
      {
        type: "pattern",
        value: /[0-9]/,
        message: "Le mot de passe doit contenir un chiffre.",
      },
    ],
  },
] satisfies readonly FormFieldDefinition[];

function handleSubmit(formValues: FormValues): void {
  emit("submit", {
    email: formValues.email,
    password: formValues.password,
  });
}

</script>

<template>
    <AppLayout title="MatosKC">
  <BaseForm
    v-model="values"
    :fields="fields"
    :loading="loading"
    submit-label="Se connecter"
    @submit="handleSubmit"
  />
    </AppLayout>
</template>

<style lang=scss>

</style>