<script setup lang="ts">
import BaseSpinner from "../common/spinner/BaseSpinner.vue";
import PageHero from "../common/page/PageHero.vue";
import { computed, onMounted, ref, watch } from "vue";
import { ApiError } from "../../api/client.ts";
import { agencyScope } from "../../api/agencies/scope";
import type { EquipmentCategoryDto } from "../../api/equipment-categories/dto.ts";
import { equipmentCategoriesApi } from "../../api/equipment-categories/index.ts";
import type { InspectionTemplateDto } from "../../api/inspection-templates/dto.ts";
import { inspectionTemplatesApi } from "../../api/inspection-templates/index.ts";
import TemplatePointsEditor, { type EditablePoint } from "./TemplatePointsEditor.vue";
import TemplateCategorySelector from "./TemplateCategorySelector.vue";
import CurrentTemplateCard from "./CurrentTemplateCard.vue";

const categories = ref<EquipmentCategoryDto[]>([]);
const categoryId = ref("");
const selectedCategory = computed(() =>
  categories.value.find((item) => item.id === categoryId.value),
);
const current = ref<InspectionTemplateDto | null>(null);
const points = ref<EditablePoint[]>([
  { key: 1, description: "", descriptionDetail: "", photoId: null },
]);
const loading = ref(true);
const loadingTemplate = ref(false);
const saving = ref(false);
const uploading = ref(false);
const error = ref("");
const success = ref("");
const validationRequested = ref(false);
let requestId = 0;

function describeError(cause: unknown) {
  if (cause instanceof ApiError) {
    if (cause.status === 400) return "Le modèle n’a pas pu être créé. Vérifiez les champs.";
  }
  return cause instanceof ApiError ? cause.message : "Une erreur est survenue. Veuillez réessayer.";
}

function resetPoints() {
  validationRequested.value = false;
  points.value = [{ key: Date.now(), description: "", descriptionDetail: "", photoId: null }];
}

function copyCurrent() {
  if (!current.value) return;
  points.value = [...current.value.points]
    .sort((a, b) => a.position - b.position)
    .map((point, index) => ({
      key: Date.now() + index,
      description: point.description,
      descriptionDetail: point.descriptionDetail,
      photoId: point.photoId,
    }));
}

async function loadCurrent(id: string) {
  const token = ++requestId;
  current.value = null;
  resetPoints();
  error.value = "";
  success.value = "";
  if (!id) return;
  loadingTemplate.value = true;
  try {
    const template = await inspectionTemplatesApi.getCurrentForCategory(id);
    if (token === requestId) current.value = template;
  } catch (cause) {
    if (token === requestId && !(cause instanceof ApiError && cause.status === 404))
      error.value = describeError(cause);
  } finally {
    if (token === requestId) loadingTemplate.value = false;
  }
}

watch(categoryId, (id) => {
  void loadCurrent(id);
});

onMounted(async () => {
  try {
    categories.value = (await equipmentCategoriesApi.list()).equipmentCategories.filter(
      (category) =>
        !agencyScope.activeAgencyId.value || category.agencyId === agencyScope.activeAgencyId.value,
    );
  } catch (cause) {
    error.value = describeError(cause);
  } finally {
    loading.value = false;
  }
});

async function submit() {
  if (!categoryId.value || saving.value || uploading.value || loadingTemplate.value) return;
  validationRequested.value = true;
  error.value = "";
  success.value = "";
  const descriptions = points.value.map((point) => point.description.trim());
  if (!descriptions.length || descriptions.some((description) => !description)) {
    error.value = "Renseignez au moins un point et complétez toutes les descriptions.";
    return;
  }
  const details = points.value.map((point) => point.descriptionDetail);
  if (details.some((detail) => !detail.trim())) {
    error.value = "Complétez le détail de chaque point.";
    return;
  }
  saving.value = true;
  const selectedCategory = categoryId.value;
  try {
    const created = await inspectionTemplatesApi.create({
      equipmentCategoryId: selectedCategory,
      points: points.value.map((point, index) => ({
        description: descriptions[index]!,
        descriptionDetail: details[index]!,
        photoId: point.photoId,
      })),
    });
    if (categoryId.value === selectedCategory) {
      current.value = created;
      resetPoints();
      success.value =
        "Le modèle a été créé. Il est désormais le modèle courant de cette catégorie.";
    }
  } catch (cause) {
    error.value = describeError(cause);
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <main class="template-page">
    <PageHero
      title="Modèles d’état des lieux"
      description="Composez un parcours de contrôle clair pour chaque catégorie de matériel."
      eyebrow="Configuration · États des lieux"
    />
    <div class="template-page__overview">
      <TemplateCategorySelector
        v-model="categoryId"
        :categories="categories"
        :loading="loading"
        :disabled="saving || uploading"
      />
      <p
        v-if="loadingTemplate"
        class="template-page__empty"
        role="status"
      >
        <BaseSpinner size="medium" variant="secondary" label="Chargement du modèle courant" />
      </p>
      <CurrentTemplateCard
        v-else-if="current"
        :template="current"
        :disabled="saving || uploading"
        @copy="copyCurrent"
      />
      <p
        v-else-if="categoryId"
        class="template-page__empty"
      >
        Aucun modèle pour cette catégorie. Créez le premier ci-dessous.
      </p>
      <p
        v-else
        class="template-page__empty"
      >
        Choisissez une catégorie pour consulter sa version active et créer un nouveau modèle.
      </p>
    </div>
    <form
      v-if="categoryId"
      class="template-page__form"
      novalidate
      @submit.prevent="submit"
    >
      <div class="template-page__form-heading">
        <span>02 · Composition</span>
        <h2>Nouveau modèle</h2>
      </div>
      <p>
        La création rend ce modèle courant pour la catégorie sélectionnée. Les états des lieux déjà
        créés conservent leur modèle.
      </p>
      <TemplatePointsEditor
        :points="points"
        :agency-id="selectedCategory?.agencyId ?? ''"
        :validation-requested="validationRequested"
        :disabled="saving || uploading || !categoryId || loadingTemplate"
        @change="points = $event"
        @busy="uploading = $event"
      />
      <p
        v-if="error"
        role="alert"
        class="error"
      >
        {{ error }}
      </p>
      <p
        v-if="success"
        role="status"
        class="success"
      >
        {{ success }}
      </p>
      <footer class="template-page__footer">
        <span
          ><strong>{{ points.length }} point{{ points.length > 1 ? "s" : "" }}</strong> de contrôle
          · Ce modèle deviendra la version courante.</span
        >
        <button
          class="submit"
          type="submit"
          :disabled="!categoryId || saving || uploading || loadingTemplate"
        >
          {{ saving ? "Création…" : "Créer le modèle" }}
        </button>
      </footer>
    </form>
  </main>
</template>

<style scoped lang="scss">
.template-page {
  display: grid;
  gap: 1.5rem;
  max-width: 72rem;
  min-width: 0;
  margin: 0 auto;
  padding-bottom: 2rem;
}
.template-page__overview {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  align-items: stretch;
  gap: 1rem;
  min-width: 0;
}
.template-page__empty {
  display: grid;
  align-items: center;
  margin: 0;
  padding: 1.5rem;
  border: 1px dashed var(--color-border);
  border-radius: 0.85rem;
  background: var(--color-surface-secondary);
  color: var(--color-text-secondary);
}
.template-page__form {
  display: grid;
  gap: 1.4rem;
  min-width: 0;
  padding: clamp(1rem, 3vw, 1.8rem);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background: var(--color-surface);
}
.template-page__form-heading span {
  color: var(--color-secondary);
  font-size: var(--font-size-sm);
  font-weight: 700;
}
.template-page__form-heading h2 {
  margin: 0.2rem 0 0;
}
.template-page__form > p {
  margin: 0;
  color: var(--color-text-secondary);
  line-height: 1.5;
}
.template-page__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
  padding: 1.15rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: var(--color-surface-secondary);
}
.template-page__footer span {
  color: var(--color-text-secondary);
}
.template-page__footer strong {
  color: var(--color-text);
}
.submit {
  min-height: 2.75rem;
  padding: 0.7rem 1.15rem;
  border: 0;
  border-radius: 0.5rem;
  background: var(--color-primary);
  color: var(--color-text-on-primary);
  font: inherit;
  font-weight: 600;
  cursor: pointer;
  transition:
    filter 0.2s,
    transform 0.2s;
}
.submit:hover:not(:disabled) {
  filter: brightness(1.08);
  transform: translateY(-2px);
}
.submit:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.error {
  color: var(--color-danger) !important;
}
.success {
  color: var(--color-success) !important;
}
@media (max-width: 48rem) {
  .template-page__overview {
    grid-template-columns: 1fr;
  }
}
@media (max-width: 42rem) {
  .template-page__footer .submit {
    width: 100%;
  }
}
@media (prefers-reduced-motion: reduce) {
  .submit {
    transition: none;
  }
}
</style>
