<script setup lang="ts">
import { ref } from "vue";
import { equipmentCategoriesApi } from "../../api/equipment-categories";
import BaseButton from "../common/button/BaseButton.vue";
import BaseInput from "../common/input/BaseInput.vue";

const emit = defineEmits<{
  created: [id: string];
  cancel: [];
  busy: [value: boolean];
}>();

const name = ref("");
const description = ref("");
const nameError = ref("");
const descriptionError = ref("");
const error = ref<string>("");
const submitting = ref(false);

function validate(): boolean {
  let valid = true;
  if (!name.value.trim()) {
    nameError.value = "Le nom de la catégorie est requis.";
    valid = false;
  }
  if (!description.value.trim()) {
    descriptionError.value = "La description de la catégorie est requise.";
    valid = false;
  }
  return valid;
}

function errorMessage(cause: unknown, action: string): string {
  if (cause instanceof Error) {
    return `Impossible de ${action}. ${cause.message}`;
  }
  return `Impossible de ${action}.`;
}

async function submit() {
  if (submitting.value || !validate()) return;

  submitting.value = true;
  emit("busy", true);
  error.value = "";

  try {
    const result = await equipmentCategoriesApi.create({
      name: name.value.trim(),
      description: description.value.trim(),
    });
    emit("created", result.id);
  } catch (cause: unknown) {
    error.value = errorMessage(cause, "créer la catégorie");
  } finally {
    submitting.value = false;
    emit("busy", false);
  }
}
</script>

<template>
  <form
    class="equipment-category-create-form"
    novalidate
    @submit.prevent="submit"
  >
    <p
      v-if="error"
      class="equipment-category-create-form__error"
      role="alert"
    >
      {{ error }}
    </p>

    <BaseInput
      id="create-equipment-category-name"
      v-model="name"
      label="Nom de la catégorie"
      :error="nameError"
      :disabled="submitting"
      required
      @update:model-value="nameError = ''"
    />

    <BaseInput
      id="create-equipment-category-description"
      v-model="description"
      label="Description de la catégorie"
      :error="descriptionError"
      :disabled="submitting"
      required
      @update:model-value="descriptionError = ''"
    />

    <BaseButton
      type="submit"
      :disabled="submitting"
    >
      Créer la catégorie
    </BaseButton>
  </form>
</template>

<style lang="scss" scoped>
.equipment-category-create-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;

  .equipment-category-create-form__error {
    color: var(--color-error);
    font-weight: bold;
  }
}
</style>
