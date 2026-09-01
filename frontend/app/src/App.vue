<script setup lang="ts">
import AppBody from './components/layout/AppBody.vue';

import AppFooter from './components/layout/AppFooter.vue';
import AppHeader from './components/layout/AppHeader.vue';
import AppLayout from './components/layout/AppLayout.vue';

</script>

<template>
    <AppLayout>
        <template #header>
            <AppHeader></AppHeader>
        </template>
        <template #body>
            <AppBody></AppBody>
        </template>
        <template #footer>
           <AppFooter/> 
        </template>
    </AppLayout>
</template>

<style lang=scss>

</style>