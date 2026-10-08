<script setup lang="ts">
import { RouterView } from "vue-router";
import SlideAndFade from "./components/common/transition/SlideAndFade.vue";
import AppLayout from "./components/layout/AppLayout.vue";
import { authSession } from "./api/auth/session";
import { router } from "./router";
import { ref } from "vue";
const loggingOut = ref(false);
const logoutError = ref("");
async function logout(): Promise<void> {
  if (loggingOut.value) return;
  loggingOut.value = true;
  logoutError.value = "";
  try { await authSession.logout(); await router.replace({ name: "Login" }); }
  catch { logoutError.value = "Impossible de vous déconnecter. Veuillez réessayer."; }
  finally { loggingOut.value = false; }
}
</script>

<template>
  <AppLayout title="MatosKC">
    <template #header-actions>
      <button v-if="authSession.account.value" type="button" class="logout" :disabled="loggingOut" @click="logout">
        {{ loggingOut ? "Déconnexion…" : "Se déconnecter" }}
      </button>
    </template>
    <template #default>
      <p v-if="logoutError" class="logout-error" role="alert">{{ logoutError }}</p>
      <RouterView v-slot="{ Component, route }">
        <SlideAndFade>
          <component
            :is="Component"
            :key="route.path"
          />
        </SlideAndFade>
      </RouterView>
    </template>
  </AppLayout>
</template>

<style scoped lang="scss">
.logout { min-height: 2.75rem; padding: .5rem .8rem; border: 1px solid var(--color-border); border-radius: .5rem; color: var(--color-text); background: var(--color-surface); font: inherit; cursor: pointer; }
.logout-error { margin: 1rem; padding: .75rem; border: 1px solid var(--color-danger); border-radius: .5rem; color: var(--color-danger); }
.logout:hover { background: var(--color-surface-hover); }
.logout:focus-visible { outline: .1875rem solid var(--color-focus); }
</style>
