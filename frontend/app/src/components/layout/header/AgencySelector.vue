<script setup lang="ts">
import { ref, watch } from "vue";
import { agenciesApi } from "../../../api/agencies";
import type { AgencyDto } from "../../../api/agencies/dto";
import { agencyScope } from "../../../api/agencies/scope";
import { authSession } from "../../../api/auth/session";

const agencies = ref<AgencyDto[]>([]);
const loading = ref(false);
const error = ref(false);
let version = 0;

async function load(): Promise<void> {
  const current = ++version;
  agencies.value = [];
  error.value = false;
  loading.value = true;
  try {
    const result = await agenciesApi.list();
    if (current !== version) return;
    agencies.value = result.agencies;
    agencyScope.agencies.value = result.agencies;
    if (
      agencyScope.selectedAgencyId.value &&
      !agencies.value.some((agency) => agency.id === agencyScope.selectedAgencyId.value)
    )
      agencyScope.select("");
  } catch {
    if (current === version) error.value = true;
  } finally {
    if (current === version) loading.value = false;
  }
}

watch(
  () => authSession.account.value?.id,
  (id) => {
    ++version;
    agencies.value = [];
    if (id && agencyScope.isSuperAdmin.value) void load();
  },
  { immediate: true },
);
</script>

<template>
  <div
    v-if="agencyScope.isSuperAdmin.value"
    class="agency-selector"
  >
    <label for="header-agency">Agence</label>
    <select
      id="header-agency"
      :value="agencyScope.selectedAgencyId.value"
      :disabled="loading || error"
      @change="agencyScope.select(($event.target as HTMLSelectElement).value)"
    >
      <option value="">Toutes les agences</option>
      <option
        v-for="agency in agencies"
        :key="agency.id"
        :value="agency.id"
      >
        {{ agency.code }} — {{ agency.name }}
      </option>
    </select>
    <button
      v-if="error"
      type="button"
      @click="load"
    >
      Réessayer
    </button>
  </div>
</template>

<style scoped lang="scss">
.agency-selector {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
}
.agency-selector label {
  white-space: nowrap;
}
.agency-selector select {
  min-width: 0;
  max-width: 13rem;
  min-height: 2.75rem;
  padding: 0.45rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-input-background);
  color: var(--color-text);
}
.agency-selector button {
  min-height: 2.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface);
  color: var(--color-text);
  cursor: pointer;
}
.agency-selector :is(select, button):focus-visible {
  outline: 3px solid var(--color-focus);
}
@media (max-width: 40rem) {
  .agency-selector label {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip-path: inset(50%);
  }
  .agency-selector select {
    max-width: 10rem;
  }
}
</style>
