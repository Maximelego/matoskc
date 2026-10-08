<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { ApiError } from "../api/client";
import { authSession } from "../api/auth/session";
import type { LoginRequestDto } from "../api/auth";
import LoginComponent from "../components/login/LoginComponent.vue";

const route = useRoute();
const router = useRouter();
const busy = ref(false);
const error = ref<string | null>(null);
const displayError = computed(() => error.value ?? (route.query.unavailable === "1"
  ? "Le serveur est momentanément inaccessible. Vous pouvez réessayer de vous connecter."
  : null));
async function submit(credentials: LoginRequestDto): Promise<void> {
  if (busy.value) return;
  busy.value = true; error.value = null;
  try {
    await authSession.login(credentials);
    const redirect = route.query.redirect;
    const destination = typeof redirect === "string" && redirect.startsWith("/") && !redirect.startsWith("//") ? redirect : "/";
    await router.replace(destination);
  } catch (cause) {
    if (cause instanceof ApiError) {
      error.value = cause.status === 401 ? "Identifiants incorrects. Veuillez réessayer."
        : cause.status === 429 ? "Trop de tentatives de connexion. Veuillez patienter avant de réessayer."
        : cause.status >= 500 ? "Le serveur est momentanément indisponible. Veuillez réessayer."
        : cause.status === 400 ? "La demande de connexion est invalide. Veuillez vérifier les informations saisies."
        : "La connexion a échoué. Veuillez réessayer.";
    } else error.value = "Impossible de joindre le serveur. Veuillez vérifier votre connexion.";
  } finally { busy.value = false; }
}
</script>
<template>
  <LoginComponent :busy="busy" :error="displayError" @submit="submit" />
</template>
