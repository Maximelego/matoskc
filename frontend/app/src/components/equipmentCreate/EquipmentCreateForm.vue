<script setup lang="ts">
import { onMounted, ref } from "vue";
import { ApiError } from "../../api/client";
import { equipmentCategoriesApi } from "../../api/equipment-categories";
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto";
import { equipmentsApi } from "../../api/equipments";
import BaseButton from "../common/button/BaseButton.vue";
import BaseInput from "../common/input/BaseInput.vue";

const emit = defineEmits<{
  created: [id: string];
  cancel: [];
  busy: [value: boolean];
}>();

const name = ref("");
const serialNumber = ref("");
const categoryId = ref("");

const categories = ref<EquipmentCategoryDto[]>([]);
const categoriesLoading = ref(true);
const submitting = ref(false);

const error = ref("");
const nameError = ref("");
const serialError = ref("");
const categoryError = ref("");

function errorMessage(cause: unknown, action: string): string {
  if (cause instanceof ApiError) {
    if (cause.status === 409) return "Ce numéro de série existe déjà.";
    if (cause.status === 400) {
      return "Certaines données sont invalides. Veuillez vérifier le formulaire.";
    }
    if (cause.status === 401) return "Veuillez vous connecter pour continuer.";
    if (cause.status === 403) {
      return "Vous ne disposez pas des droits nécessaires.";
    }
    if (cause.status >= 500) {
      return "Le serveur rencontre un problème. Veuillez réessayer plus tard.";
    }
  }

  if (cause instanceof TypeError) {
    return "Impossible de joindre l’API. Veuillez vérifier votre connexion.";
  }

  return `Impossible de ${action}. Veuillez réessayer.`;
}

async function loadCategories(): Promise<void> {
  categoriesLoading.value = true;
  error.value = "";

  try {
    const result = await equipmentCategoriesApi.list();
    categories.value = result.equipmentCategories;
  } catch (cause: unknown) {
    error.value = errorMessage(cause, "charger les catégories");
  } finally {
    categoriesLoading.value = false;
  }
}

function validate(): boolean {
  nameError.value = name.value.trim() ? "" : "Veuillez saisir un nom.";
  serialError.value = serialNumber.value.trim() ? "" : "Veuillez saisir un numéro de série.";

  categoryError.value = categories.value.some((category) => category.id === categoryId.value)
    ? ""
    : "Veuillez sélectionner une catégorie.";

  return !nameError.value && !serialError.value && !categoryError.value;
}

async function submit(): Promise<void> {
  if (submitting.value || categoriesLoading.value || !validate()) return;

  submitting.value = true;
  emit("busy", true);
  error.value = "";

  try {
    const result = await equipmentsApi.create({
      name: name.value.trim(),
      serialNumber: serialNumber.value.trim(),
      categoryId: categoryId.value,
    });

    emit("created", result.id);
  } catch (cause: unknown) {
    error.value = errorMessage(cause, "créer l’équipement");
  } finally {
    submitting.value = false;
    emit("busy", false);
  }
}

onMounted(loadCategories);
</script>

<template>
  <form
    class="equipment-create-form"
    novalidate
    @submit.prevent="submit"
  >
    <p
      v-if="error"
      class="equipment-create-form__error"
      role="alert"
    >
      {{ error }}
    </p>

    <BaseInput
      id="create-equipment-name"
      v-model="name"
      label="Nom de l’équipement"
      :error="nameError"
      :disabled="submitting"
      required
      @update:model-value="nameError = ''"
    />

    <BaseInput
      id="create-equipment-serial"
      v-model="serialNumber"
      label="Numéro de série"
      :error="serialError"
      :disabled="submitting"
      required
      @update:model-value="serialError = ''"
    />

    <div class="equipment-create-form__field">
      <label for="create-equipment-category"> Catégorie <span aria-hidden="true">*</span> </label>

      <select
        id="create-equipment-category"
        v-model="categoryId"
        required
        :disabled="submitting || categoriesLoading || categories.length === 0"
        :aria-invalid="Boolean(categoryError)"
        :aria-describedby="categoryError ? 'create-equipment-category-error' : undefined"
        @change="categoryError = ''"
      >
        <option value="">Sélectionnez une catégorie</option>

        <option
          v-for="category in categories"
          :key="category.id"
          :value="category.id"
        >
          {{ category.name }}
        </option>
      </select>

      <p
        v-if="categoryError"
        id="create-equipment-category-error"
        role="alert"
      >
        {{ categoryError }}
      </p>

      <p
        v-else-if="categoriesLoading"
        role="status"
      >
        Chargement des catégories…
      </p>

      <p v-else-if="categories.length === 0 && !error">Aucune catégorie disponible.</p>
    </div>

    <BaseButton
      v-if="error && categories.length === 0 && !categoriesLoading"
      variant="outline"
      size="small"
      @click="loadCategories"
    >
      Réessayer
    </BaseButton>

    <div class="equipment-create-form__actions">
      <BaseButton
        variant="ghost"
        :disabled="submitting"
        @click="emit('cancel')"
      >
        Annuler
      </BaseButton>

      <BaseButton
        variant="primary"
        :loading="submitting"
        :disabled="categoriesLoading || categories.length === 0"
        @click="submit"
      >
        Créer l’équipement
      </BaseButton>
    </div>
  </form>
</template>

<style scoped lang="scss">
.equipment-create-form {
  display: grid;
  gap: 1rem;

  &__error {
    margin: 0;
    color: var(--color-danger);
  }

  &__field {
    display: grid;
    gap: 0.5rem;
  }

  &__field label {
    font-weight: var(--font-weight-semibold);
  }

  &__field select {
    width: 100%;
    min-height: 2.75rem;
    padding: 0.625rem;
    color: var(--color-text);
    background: var(--color-input-background);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
  }

  &__field select:focus-visible {
    outline: 0.1875rem solid var(--color-focus);
  }

  &__field p {
    margin: 0;
    color: var(--color-danger);
  }

  &__actions {
    display: flex;
    justify-content: flex-end;
    flex-wrap: wrap;
    gap: 0.75rem;
    margin-top: 0.5rem;
  }
}

@media (max-width: 42rem) {
  .equipment-create-form__actions { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .equipment-create-form__actions :deep(button) { width: 100%; min-height: 2.75rem; }
}
@media (max-width: 23rem) {
  .equipment-create-form__actions { grid-template-columns: 1fr; }
}
</style>
