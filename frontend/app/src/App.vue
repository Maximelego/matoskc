<script setup lang="ts">
import { RouterView } from "vue-router";
import SlideAndFade from "./components/common/transition/SlideAndFade.vue";
import AppLayout from "./components/layout/AppLayout.vue";
</script>

<template>
  <AppLayout title="MatosKC">
    <template #default>
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

<style lang="scss"></style>
