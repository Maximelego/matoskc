<script setup lang="ts">
import { ref } from "vue";
import { RouterView } from "vue-router";
import { authSession } from "./api/auth/session";
import SlideAndFade from "./components/common/transition/SlideAndFade.vue";
import AppLayout from "./components/layout/AppLayout.vue";
import HeaderAccount from "./components/layout/header/HeaderAccount.vue";
import { router } from "./router";

const loggingOut = ref(false);
const logoutError = ref("");

async function logout(): Promise<void> {
  if (loggingOut.value) return;
  loggingOut.value = true;
  logoutError.value = "";
  try {
    await authSession.logout();
    await router.replace({ name: "Login" });
  } catch {
    logoutError.value = "Impossible de vous déconnecter. Veuillez réessayer.";
  } finally {
    loggingOut.value = false;
  }
}
</script>

<template>
  <AppLayout title="MatosKC">
    <template #header-actions>
      <HeaderAccount
        :logging-out="loggingOut"
        @logout="logout"
      />
    </template>
    <template #default>
      <p
        v-if="logoutError"
        class="logout-error"
        role="alert"
      >
        {{ logoutError }}
      </p>
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
.logout {
  min-height: 2.75rem;
  padding: 0.5rem 0.8rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-surface);
  font: inherit;
  cursor: pointer;
}
.logout-error {
  margin: 1rem;
  padding: 0.75rem;
  border: 1px solid var(--color-danger);
  border-radius: 0.5rem;
  color: var(--color-danger);
}
.logout:hover {
  background: var(--color-surface-hover);
}
.logout:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
}
</style>
