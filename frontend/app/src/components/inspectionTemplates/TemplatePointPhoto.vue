<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { inspectionTemplatesApi } from "../../api/inspection-templates";
import BaseIcon from "../common/icon/BaseIcon.vue";

const props = defineProps<{ photoId: string | null; disabled: boolean; agencyId: string }>();
const emit = defineEmits<{ change: [id: string | null]; busy: [value: boolean] }>();
const url = ref("");
const busy = ref(false);
const error = ref("");
let sequence = 0;
function release() {
  if (url.value) URL.revokeObjectURL(url.value);
  url.value = "";
}
watch(
  () => props.photoId,
  async (id) => {
    const token = ++sequence;
    release();
    error.value = "";
    if (!id) return;
    try {
      const blob = await inspectionTemplatesApi.getPhoto(id);
      if (token === sequence) url.value = URL.createObjectURL(blob);
    } catch {
      if (token === sequence) error.value = "Impossible de charger l’illustration.";
    }
  },
  { immediate: true },
);
onUnmounted(() => {
  ++sequence;
  release();
});
async function upload(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = "";
  if (!file) return;
  if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
    error.value = "Choisissez une image JPEG, PNG ou WebP.";
    return;
  }
  busy.value = true;
  emit("busy", true);
  error.value = "";
  try {
    emit("change", (await inspectionTemplatesApi.uploadPhoto(file, props.agencyId)).id);
  } catch {
    error.value = "Impossible d’envoyer l’illustration.";
  } finally {
    busy.value = false;
    emit("busy", false);
  }
}
</script>
<template>
  <div class="photo">
    <img
      v-if="url"
      :src="url"
      alt="Illustration du point de contrôle"
    />
    <BaseIcon
      v-else
      name="image"
      :size="28"
    />
    <label
      >Illustration (facultative)
      <input
        type="file"
        accept="image/jpeg,image/png,image/webp"
        :disabled="disabled || busy"
        @change="upload"
      />
    </label>
    <button
      v-if="photoId"
      type="button"
      :disabled="disabled || busy"
      @click="emit('change', null)"
    >
      Retirer du point
    </button>
    <span
      v-if="busy"
      role="status"
      >Envoi en cours…</span
    >
    <span
      v-if="error"
      role="alert"
      class="error"
      >{{ error }}</span
    >
  </div>
</template>
<style scoped>
.photo {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  color: var(--color-text-secondary);
}
.photo img {
  width: 4rem;
  height: 4rem;
  object-fit: cover;
  border-radius: 0.4rem;
}
.photo label {
  display: grid;
  gap: 0.3rem;
}
.photo button {
  border: 1px solid var(--color-border);
  border-radius: 0.4rem;
  padding: 0.5rem;
  background: var(--color-surface);
  color: inherit;
  cursor: pointer;
}
.error {
  color: var(--color-danger);
}
</style>
