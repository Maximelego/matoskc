<script setup lang="ts">
import { ref } from "vue";
import AdminHomeComponent from "./adminHomeComponent/AdminHomeComponent.vue";
import MagaHomeComponent from "./magaHomeComponent/MagaHomeComponent.vue";
import SuperAdminHomeComponent from "./superAdminHomeComponent/SuperAdminHomeComponent.vue";

const viewMode = ref<"admin" | "user" | "super-admin">("user");
</script>

<template>
  <div>
    <div class="admin-mode-switch">
      <h2>View-mode : {{ viewMode }}</h2>
      <input
        id="user"
        v-model="viewMode"
        type="radio"
        value="user"
      />
      <label for="user">Agence</label>
      <input
        id="admin"
        v-model="viewMode"
        type="radio"
        value="admin"
      />
      <label for="admin">Admin</label>
      <input
        id="super-admin"
        v-model="viewMode"
        type="radio"
        value="super-admin"
      />
      <label for="super-admin">Super Admin</label>
    </div>

    <div class="home">
      <MagaHomeComponent v-if="viewMode === 'user'" />
      <AdminHomeComponent v-else-if="viewMode === 'admin'" />
      <SuperAdminHomeComponent v-else-if="viewMode === 'super-admin'" />
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
