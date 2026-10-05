<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { ApiError } from "../../api/client";
import { equipmentPhotosApi } from "../../api/equipments/photos";
import BaseButton from "../common/button/BaseButton.vue";
import BaseIcon from "../common/icon/BaseIcon.vue";

const props = defineProps<{ equipmentId: string; open: boolean }>();
const url = ref<string | null>(null);
const loading = ref(false);
const busy = ref(false);
const error = ref("");
const notice = ref("");
const input = ref<HTMLInputElement | null>(null);
let sequence = 0;

function clear(): void {
  if (url.value) URL.revokeObjectURL(url.value);
  url.value = null;
}

async function load(): Promise<void> {
  const current = ++sequence;
  clear();
  loading.value = true;
  error.value = "";
  try {
    const blob = await equipmentPhotosApi.get(props.equipmentId);
    if (current !== sequence) return;
    if (!blob.type.startsWith("image/")) throw new Error("Format invalide");
    url.value = URL.createObjectURL(blob);
  } catch (cause) {
    if (current !== sequence) return;
    if (!(cause instanceof ApiError && cause.status === 404))
      error.value = "Impossible de charger la photographie de cet équipement.";
  } finally {
    if (current === sequence) loading.value = false;
  }
}

async function upload(event: Event): Promise<void> {
  const element = event.target as HTMLInputElement;
  const file = element.files?.[0];
  element.value = "";
  if (!file) return;
  if (!file.type.startsWith("image/")) {
    error.value = "Veuillez sélectionner une image.";
    return;
  }
  busy.value = true;
  error.value = "";
  notice.value = "";
  try {
    await equipmentPhotosApi.put(props.equipmentId, file);
    await load();
    if (!error.value) notice.value = "Photographie enregistrée.";
  } catch {
    error.value = "Impossible d’enregistrer la photographie. Veuillez réessayer.";
  } finally {
    busy.value = false;
  }
}

async function remove(): Promise<void> {
  if (!window.confirm("Supprimer la photographie de cet équipement ?")) return;
  busy.value = true;
  error.value = "";
  notice.value = "";
  try {
    await equipmentPhotosApi.delete(props.equipmentId);
    ++sequence;
    clear();
    notice.value = "Photographie supprimée.";
  } catch {
    error.value = "Impossible de supprimer la photographie. Veuillez réessayer.";
  } finally {
    busy.value = false;
  }
}

watch(
  () => [props.equipmentId, props.open] as const,
  () => {
    ++sequence;
    clear();
    error.value = "";
    notice.value = "";
    if (props.open) void load();
    else loading.value = false;
  },
  { immediate: true },
);
onUnmounted(() => {
  ++sequence;
  clear();
});
</script>

<template>
  <section
    class="equipment-photo"
    aria-label="Photographie de l’équipement"
  >
    <div class="equipment-photo__preview">
      <p
        v-if="loading"
        role="status"
      >
        Chargement de la photographie…
      </p>
      <img
        v-else-if="url"
        :src="url"
        alt="Photographie de l’équipement"
      />
      <div
        v-else
        class="equipment-photo__placeholder"
      >
        <BaseIcon
          name="camera"
          :size="36"
        /><span>Aucune photographie</span>
      </div>
    </div>
    <p
      v-if="error"
      class="equipment-photo__error"
      role="alert"
    >
      {{ error }}
    </p>
    <p
      v-if="notice"
      role="status"
    >
      {{ notice }}
    </p>
    <div class="equipment-photo__actions">
      <input
        ref="input"
        class="equipment-photo__input"
        type="file"
        accept="image/*"
        :disabled="busy || loading"
        aria-label="Sélectionner une photographie"
        @change="upload"
      />
      <BaseButton
        variant="outline"
        size="small"
        :disabled="busy || loading"
        @click="input?.click()"
        >{{ url ? "Remplacer la photographie" : "Ajouter une photographie" }}</BaseButton
      >
      <BaseButton
        v-if="url"
        variant="danger"
        size="small"
        :disabled="busy || loading"
        @click="remove"
        >Supprimer</BaseButton
      >
    </div>
  </section>
</template>

<style scoped lang="scss">
.equipment-photo {
  display: grid;
  gap: 0.75rem;
  min-width: 0;
}
.equipment-photo__preview {
  display: grid;
  place-items: center;
  min-height: 12rem;
  overflow: hidden;
  border: 1px dashed var(--color-border-strong);
  border-radius: 0.75rem;
  background: var(--color-surface-secondary);
}
.equipment-photo__preview img {
  display: block;
  max-width: 100%;
  max-height: 22rem;
  object-fit: contain;
}
.equipment-photo__placeholder {
  display: grid;
  justify-items: center;
  gap: 0.4rem;
  color: var(--color-text-secondary);
}
.equipment-photo__error {
  margin: 0;
  color: var(--color-danger);
}
.equipment-photo__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.equipment-photo__input {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
  pointer-events: none;
}
@media (max-width: 42rem) {
  .equipment-photo__preview {
    min-height: 9rem;
  }
  .equipment-photo__actions > * {
    flex: 1;
  }
}
</style>
