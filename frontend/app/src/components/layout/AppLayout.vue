<script setup lang=ts>
import AppBody from './AppBody.vue';
import AppFooter from './AppFooter.vue';
import AppHeader from './AppHeader.vue';
</script>

<template>
    <div class="app-layout">
        <AppHeader title="hello"/>
        <AppBody/>
        <AppFooter/>
    </div>
</template>

<style lang=scss>
.app-layout {
  min-height: 100dvh;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
}
</style>