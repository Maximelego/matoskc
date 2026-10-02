<script setup lang="ts">
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto";
import type { EquipmentFilters, EquipmentStatus } from "../../api/equipments/dto";

const props = defineProps<{
  modelValue: EquipmentFilters;
  categories: readonly EquipmentCategoryDto[];
  categoriesLoading: boolean;
  categoriesError: boolean;
}>();

const emit = defineEmits<{
  "update:modelValue": [filters: EquipmentFilters];
  "retry-categories": [];
}>();

const statuses: { value: EquipmentStatus; label: string }[] = [
  { value: "Available", label: "Disponible" },
  { value: "Unavailable", label: "Indisponible" },
  { value: "ToBeDecided", label: "À décider" },
  { value: "Decommissioned", label: "Réformé" },
  { value: "Maintenance", label: "En maintenance" },
  { value: "Borrowed", label: "Emprunté" },
];

function updateStatus(event: Event): void {
  const value = (event.target as HTMLSelectElement).value;

  update({
    status: value === "" ? undefined : (value as EquipmentStatus),
  });
}

function update(patch: Partial<EquipmentFilters>): void {
  emit("update:modelValue", { ...props.modelValue, ...patch });
}
</script>

<template>
  <div
    class="equipment-filters"
    role="search"
    aria-label="Filtrer les équipements"
  >
    <div class="equipment-filters__field equipment-filters__field--search">
      <label for="equipment-search">Rechercher</label>
      <input
        id="equipment-search"
        type="search"
        :value="modelValue.search ?? ''"
        placeholder="Nom ou numéro de série"
        autocomplete="off"
        @input="update({ search: ($event.target as HTMLInputElement).value })"
      />
    </div>

    <div class="equipment-filters__field">
      <label for="equipment-category-filter">Catégorie</label>
      <select
        id="equipment-category-filter"
        :value="modelValue.categoryId ?? ''"
        :disabled="categoriesLoading || categoriesError"
        @change="update({ categoryId: ($event.target as HTMLSelectElement).value || undefined })"
      >
        <option value="">Toutes les catégories</option>
        <option
          v-for="category in categories"
          :key="category.id"
          :value="category.id"
        >
          {{ category.name }}
        </option>
      </select>
    </div>

    <div class="equipment-filters__field">
      <label for="equipment-status-filter">État</label>
      <select
        id="equipment-status-filter"
        :value="modelValue.status ?? ''"
        @change="updateStatus"
      >
        <option value="">Tous les états</option>
        <option
          v-for="status in statuses"
          :key="status.value"
          :value="status.value"
        >
          {{ status.label }}
        </option>
      </select>
    </div>

    <button
      type="button"
      class="equipment-filters__reset"
      :disabled="!modelValue.search && !modelValue.categoryId && modelValue.status === undefined"
      @click="emit('update:modelValue', {})"
    >
      Réinitialiser
    </button>

    <p
      v-if="categoriesLoading"
      class="equipment-filters__message"
      role="status"
    >
      Chargement des catégories…
    </p>
    <p
      v-else-if="categoriesError"
      class="equipment-filters__message"
      role="alert"
    >
      Les catégories ne sont pas disponibles.
      <button
        type="button"
        @click="emit('retry-categories')"
      >
        Réessayer
      </button>
    </p>
  </div>
</template>

<style scoped lang="scss">
.equipment-filters {
  display: flex;
  align-items: end;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--color-border);

  &__field {
    display: grid;
    gap: 0.35rem;
    min-width: 10rem;
  }
  &__field--search {
    flex: 1 1 16rem;
  }
  &__field label {
    color: var(--color-text-secondary);
    font-size: var(--font-size-sm);
  }
  &__field input,
  &__field select {
    box-sizing: border-box;
    width: 100%;
    min-height: 2.5rem;
    padding: 0.5rem 0.75rem;
    color: var(--color-text);
    background: var(--color-input-background);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
  }
  &__field input:focus-visible,
  &__field select:focus-visible,
  &__reset:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
    outline-offset: 0.125rem;
  }
  &__reset {
    min-height: 2.5rem;
    padding: 0.5rem 0.75rem;
    color: var(--color-primary);
    background: transparent;
    border: 0;
    border-radius: 0.5rem;
    cursor: pointer;
  }
  &__reset:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
  &__message {
    flex-basis: 100%;
    margin: 0;
    color: var(--color-text-secondary);
  }
  &__message button {
    color: var(--color-primary);
    background: none;
    border: 0;
    cursor: pointer;
    text-decoration: underline;
  }
}
</style>
