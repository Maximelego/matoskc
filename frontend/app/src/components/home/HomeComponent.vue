<script setup lang="ts">
import { authSession } from "../../api/auth/session";
import AdminHomeComponent from "./adminHomeComponent/AdminHomeComponent.vue";
import MagaHomeComponent from "./magaHomeComponent/MagaHomeComponent.vue";
import SuperAdminHomeComponent from "./superAdminHomeComponent/SuperAdminHomeComponent.vue";
</script>

<template>
  <SuperAdminHomeComponent v-if="authSession.account.value?.role === 'SuperAdmin'" />
  <AdminHomeComponent v-else-if="authSession.account.value?.role === 'Admin'" />
  <MagaHomeComponent v-else-if="authSession.account.value?.role === 'Agency'" />
</template>
