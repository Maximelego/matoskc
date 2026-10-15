<script setup lang="ts">
import { onMounted, ref, watch } from "vue";
import { ApiError } from "../api/client";
import { equipmentCategoriesApi } from "../api/equipment-categories";
import type { EquipmentCategoryDto } from "../api/equipment-categories/dto";
import { inspectionTemplatesApi } from "../api/inspection-templates";
import type { InspectionTemplateDto } from "../api/inspection-templates/dto";
import TemplatePointsEditor, { type EditablePoint } from "../components/inspectionTemplates/TemplatePointsEditor.vue";

const categories = ref<EquipmentCategoryDto[]>([]);
const categoryId = ref("");
const current = ref<InspectionTemplateDto | null>(null);
const points = ref<EditablePoint[]>([{ key: 1, description: "", photoId: null }]);
const loading = ref(true);
const loadingTemplate = ref(false);
const saving = ref(false);
const error = ref("");
const success = ref("");
let requestId = 0;
function describeError(cause: unknown) { return cause instanceof ApiError ? cause.message : "Une erreur est survenue. Veuillez réessayer."; }
function resetPoints() { points.value = [{ key: Date.now(), description: "", photoId: null }]; }
function copyCurrent() {
  if (!current.value) return;
  points.value = [...current.value.points].sort((a, b) => a.position - b.position)
    .map((point, index) => ({ key: Date.now() + index, description: point.description, photoId: point.photoId }));
}
async function loadCurrent(id: string) {
  const token = ++requestId;
  current.value = null; resetPoints(); error.value = ""; success.value = "";
  if (!id) return;
  loadingTemplate.value = true;
  try {
    const template = await inspectionTemplatesApi.getCurrentForCategory(id);
    if (token === requestId) current.value = template;
  } catch (cause) {
    if (token === requestId && !(cause instanceof ApiError && cause.status === 404)) error.value = describeError(cause);
  } finally { if (token === requestId) loadingTemplate.value = false; }
}
watch(categoryId, id => { void loadCurrent(id); });
onMounted(async () => {
  try { categories.value = (await equipmentCategoriesApi.list()).equipmentCategories; }
  catch (cause) { error.value = describeError(cause); }
  finally { loading.value = false; }
});
async function submit() {
  if (!categoryId.value || saving.value || loadingTemplate.value) return;
  error.value = ""; success.value = "";
  const descriptions = points.value.map(point => point.description.trim());
  if (!descriptions.length || descriptions.some(description => !description)) {
    error.value = "Renseignez au moins un point et complétez toutes les descriptions."; return;
  }
  saving.value = true;
  const selectedCategory = categoryId.value;
  try {
    const created = await inspectionTemplatesApi.create({ equipmentCategoryId: selectedCategory,
      points: points.value.map((point, index) => ({ description: descriptions[index]!, photoId: point.photoId })) });
    if (categoryId.value === selectedCategory) {
      current.value = created; resetPoints();
      success.value = "Le modèle a été créé. Il est désormais le modèle courant de cette catégorie.";
    }
  } catch (cause) { error.value = describeError(cause); }
  finally { saving.value = false; }
}
</script>

<template>
  <main class="template-page">
    <header><h1>Modèles d’état des lieux</h1><p>Créez les points de contrôle pour une catégorie d’équipement.</p></header>
    <section class="panel" aria-labelledby="category-heading">
      <h2 id="category-heading">Catégorie</h2>
      <label for="template-category">Catégorie d’équipement</label>
      <select id="template-category" v-model="categoryId" :disabled="loading || saving" required>
        <option value="">Sélectionnez une catégorie</option>
        <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option>
      </select>
      <p v-if="!loading && !categories.length">Aucune catégorie disponible.</p>
      <p v-if="loadingTemplate" role="status">Chargement du modèle courant…</p>
      <div v-else-if="current" class="current">
        <h3>Modèle courant</h3>
        <p>{{ current.points.length }} point{{ current.points.length > 1 ? 's' : '' }} · Créé le {{ new Date(current.createdAt).toLocaleDateString('fr-FR') }}</p>
        <ol><li v-for="point in [...current.points].sort((a, b) => a.position - b.position)" :key="point.id">{{ point.description }}</li></ol>
        <button type="button" :disabled="saving" @click="copyCurrent">Reprendre ces points</button>
      </div>
      <p v-else-if="categoryId">Aucun modèle pour cette catégorie.</p>
    </section>
    <form class="panel" @submit.prevent="submit">
      <h2>Nouveau modèle</h2>
      <p>La création rend ce modèle courant pour la catégorie sélectionnée. Les états des lieux déjà créés conservent leur modèle.</p>
      <TemplatePointsEditor :points="points" :disabled="saving || !categoryId || loadingTemplate" @change="points = $event" />
      <p v-if="error" role="alert" class="error">{{ error }}</p>
      <p v-if="success" role="status" class="success">{{ success }}</p>
      <button class="submit" type="submit" :disabled="!categoryId || saving || loadingTemplate">{{ saving ? 'Création…' : 'Créer le modèle' }}</button>
    </form>
  </main>
</template>

<style scoped>
.template-page { max-width: 62rem; margin: 0 auto; display: grid; gap: 1.5rem; }
header h1 { margin: 0; } header p, .panel > p { color: var(--color-text-secondary); }
.panel { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: .75rem; padding: 1.5rem; display: grid; gap: 1rem; min-width: 0; }
.panel h2, .current h3 { margin: 0; } .panel > label { font-weight: 600; }
select { width: 100%; padding: .7rem; border: 1px solid var(--color-border); border-radius: .4rem; background: var(--color-input-background); color: inherit; }
.current { border-top: 1px solid var(--color-border); padding-top: 1rem; } .current ol { padding-left: 1.4rem; }
.current button, .submit { border: 0; border-radius: .4rem; padding: .7rem 1rem; cursor: pointer; }
.current button { background: var(--color-surface-secondary); color: inherit; }
.submit { justify-self: start; background: var(--color-primary); color: var(--color-text-on-primary); font-weight: 600; }
button:disabled { opacity: .5; cursor: not-allowed; } .error { color: var(--color-danger) !important; } .success { color: var(--color-success) !important; }
@media (max-width: 42rem) { .panel { padding: 1rem; } }
</style>
