<script setup lang="ts">
import { computed, ref } from "vue";
import { RouterView, useRoute } from "vue-router";
import { authSession } from "./api/auth/session";
import BaseIcon from "./components/common/icon/BaseIcon.vue";
import SlideAndFade from "./components/common/transition/SlideAndFade.vue";
import AppLayout from "./components/layout/AppLayout.vue";
import HeaderAccount from "./components/layout/header/HeaderAccount.vue";
import { router } from "./router";

const loggingOut = ref(false);
const logoutError = ref("");
const route = useRoute();
const showBack = computed(() => route.name !== "Home" && route.name !== "Login");
function goBack(): void {
  const previous = window.history.state?.back;
  if (typeof previous === "string" && previous !== route.fullPath && !previous.startsWith("/login"))
    router.back();
  else void router.push({ name: "Home" });
}

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
      <button
        v-if="showBack"
        type="button"
        class="back-button"
        @click="goBack"
      >
        <BaseIcon
          name="arrow-left"
          :size="24"
        />
      </button>
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
.back-button {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 3.25rem;
  margin-bottom: 1.5rem;
  padding: 0.7rem 1.2rem;
  border: 1px solid var(--color-border);
  border-radius: 0.65rem;
  background: var(--color-surface);
  color: var(--color-text);
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}
.back-button:hover {
  background: var(--color-surface-hover);
}
.back-button:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
}
.logout:focus-visible {
  outline: 0.1875rem solid var(--color-focus);
}
</style>
