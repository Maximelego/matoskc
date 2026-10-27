<script setup lang="ts">
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto";
import { agencyScope } from "../../api/agencies/scope";

defineProps<{
  categories: EquipmentCategoryDto[];
  modelValue: string;
  loading: boolean;
  disabled: boolean;
}>();
const emit = defineEmits<{ "update:modelValue": [value: string] }>();
</script>

<template>
  <section
    class="category"
    aria-labelledby="category-heading"
  >
    <header>
      <span class="category__step">01 · Catégorie</span>
      <h2 id="category-heading">Quel matériel contrôler ?</h2>
    </header>
    <label for="template-category">Catégorie d’équipement</label>
    <select
      id="template-category"
      :value="modelValue"
      :disabled="loading || disabled"
      required
      @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
    >
      <option value="">Sélectionnez une catégorie</option>
      <option
        v-for="category in categories"
        :key="category.id"
        :value="category.id"
      >
        {{ category.name
        }}{{
          agencyScope.isSuperAdmin.value && !agencyScope.activeAgencyId.value
            ? ` (${agencyScope.label(category.agencyId)})`
            : ""
        }}
      </option>
    </select>
    <p v-if="!loading && !categories.length">Aucune catégorie disponible.</p>
  </section>
</template>

<style scoped lang="scss">
.category {
  display: grid;
  gap: 0.8rem;
  padding: 1.4rem;
  border: 1px solid var(--color-border);
  border-radius: 0.85rem;
  background: var(--color-surface);
}
.category header {
  display: grid;
  gap: 0.25rem;
}
.category__step {
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
  font-weight: 700;
}
.category h2,
.category p {
  margin: 0;
}
.category p {
  color: var(--color-text-secondary);
}
.category label {
  font-weight: 600;
}
.category select {
  box-sizing: border-box;
  width: 100%;
  min-height: 2.75rem;
  padding: 0.6rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-input-background);
  color: var(--color-text);
  font: inherit;
}
@media (max-width: 42rem) {
  .category {
    padding: 1rem;
  }
}
</style>
