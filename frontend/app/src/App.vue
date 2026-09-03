<script setup lang="ts">

import { RouterView } from 'vue-router';
import AppLayout from './components/layout/AppLayout.vue';

</script>

<template>
    <AppLayout title="MatosKC">
        <template #default>
            <RouterView />
        </template>
    </AppLayout>
</template>

<style lang=scss>

</style>