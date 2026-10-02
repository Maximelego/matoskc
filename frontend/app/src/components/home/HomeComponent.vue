<script setup lang="ts">
import { ref } from "vue";
import AdminHomeComponent from "./adminHomeComponent/AdminHomeComponent.vue";
import MagaHomeComponent from "./magaHomeComponent/MagaHomeComponent.vue";

const viewMode = ref<"admin" | "user">("user");
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
      <label for="user">User</label>
      <input
        id="admin"
        v-model="viewMode"
        type="radio"
        value="admin"
      />
      <label for="admin">Admin</label>
    </div>

    <div class="home">
      <MagaHomeComponent v-if="viewMode === 'user'" />
      <AdminHomeComponent v-else />
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
