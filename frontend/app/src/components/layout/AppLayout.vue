<script setup lang="ts">
import AppBody from "./AppBody.vue";
import AppFooter from "./AppFooter.vue";
import AppHeader from "./AppHeader.vue";

defineProps<{
  title: string;
}>();
</script>

<template>
  <div class="app-layout">
    <AppHeader :title="title">
      <template #navigation>
        <slot name="navigation" />
      </template>

      <template #actions>
        <slot name="header-actions" />
      </template>
    </AppHeader>

    <AppBody>
      <slot />
    </AppBody>

    <AppFooter>
      <slot name="footer" />
    </AppFooter>
  </div>
</template>

<style scoped lang="scss">
.app-layout {
  min-height: 100vh;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  color: var(--color-text);
  background-color: var(--color-background);
}
</style>