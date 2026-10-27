<script setup lang="ts">
import BaseInput from "../common/input/BaseInput.vue";
import { ref, watch } from "vue";
import { equipmentCategoriesApi } from "../../api/equipment-categories";
import { agencyScope } from "../../api/agencies/scope";
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto";
const props = defineProps<{ categories: EquipmentCategoryDto[] }>();
const emit = defineEmits<{ updated: [] }>();
const selected = ref("");
const name = ref("");
const nameTouched = ref(false);
const description = ref("");
const busy = ref(false);
const error = ref("");
watch(selected, (id) => {
  const category = props.categories.find((item) => item.id === id);
  name.value = category?.name ?? "";
  nameTouched.value = false;
  description.value = category?.description ?? "";
  error.value = "";
});
async function save() {
  if (!selected.value || !name.value.trim() || busy.value) return;
  busy.value = true;
  error.value = "";
  try {
    await equipmentCategoriesApi.update(selected.value, {
      name: name.value.trim(),
      description: description.value.trim() || null,
    });
    emit("updated");
  } catch {
    error.value = "Impossible de modifier la catégorie.";
  } finally {
    busy.value = false;
  }
}
</script>
<template>
  <form
    class="category-editor"
    @submit.prevent="save"
  >
    <label for="category-edit-select">Catégorie à modifier</label>
    <select
      id="category-edit-select"
      v-model="selected"
      :disabled="busy"
      required
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
    <template v-if="selected">
      <BaseInput
        label="Nom"
        id="category-edit-name"
        v-model="name"
        :error="nameTouched && !name.trim() ? 'Le nom de la catégorie est obligatoire.' : undefined"
        @blur="nameTouched = true"
        required
        :disabled="busy"
      />
      <label for="category-edit-description">Description</label>
      <textarea
        id="category-edit-description"
        v-model="description"
        rows="3"
        :disabled="busy"
      />
      <p
        v-if="error"
        role="alert"
      >
        {{ error }}
      </p>
      <button
        type="submit"
        :disabled="busy || !name.trim()"
      >
        {{ busy ? "Enregistrement…" : "Enregistrer la catégorie" }}
      </button>
    </template>
  </form>
</template>
<style scoped>
.category-editor {
  display: grid;
  gap: 0.65rem;
}
.category-editor input,
.category-editor select,
.category-editor textarea {
  padding: 0.65rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  color: var(--color-text);
  background: var(--color-input-background);
}
.category-editor button {
  justify-self: start;
  padding: 0.7rem 1rem;
  border: 0;
  border-radius: 0.5rem;
  background: var(--color-primary);
  color: var(--color-text-on-primary);
  cursor: pointer;
}
.category-editor p {
  color: var(--color-danger);
}
</style>
