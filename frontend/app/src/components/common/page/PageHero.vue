<script setup lang="ts">
import { computed } from "vue";
import { authSession } from "../../../api/auth/session";

withDefaults(
  defineProps<{ title: string; description?: string; eyebrow?: string; titleId?: string }>(),
  { description: "", eyebrow: "MatosKC" },
);
const agency = computed(() => authSession.account.value?.role === "Agency");
</script>

<template>
  <header
    class="page-hero"
    :class="{ 'page-hero--agency': agency }"
  >
    <div class="page-hero__content">
      <span
        v-if="eyebrow"
        class="page-hero__eyebrow"
        >{{ eyebrow }}</span
      >
      <h1 :id="titleId">{{ title }}</h1>
      <p v-if="description">{{ description }}</p>
    </div>
    <div
      v-if="$slots.actions"
      class="page-hero__actions"
    >
      <slot name="actions" />
    </div>
  </header>
</template>

<style scoped lang="scss">
.page-hero {
  --hero-accent: var(--color-secondary);
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.5rem;
  flex-wrap: wrap;
  min-width: 0;
  overflow: hidden;
  padding: clamp(1.35rem, 4vw, 2.5rem);
  border: 1px solid var(--color-border);
  border-radius: 1.2rem;
  background: var(--color-surface);
  isolation: isolate;
}
.page-hero--agency {
  --hero-accent: var(--color-primary);
}
.page-hero::before {
  content: "";
  position: absolute;
  z-index: -1;
  width: 18rem;
  height: 18rem;
  right: -5rem;
  top: -9rem;
  border-radius: 50%;
  background: var(--hero-accent);
  opacity: 0.12;
  pointer-events: none;
}
.page-hero::after {
  content: "";
  position: absolute;
  z-index: -1;
  width: 10rem;
  height: 10rem;
  right: 8rem;
  bottom: -8rem;
  border-radius: 50%;
  background: var(--color-primary);
  opacity: 0.1;
  pointer-events: none;
}
.page-hero__content {
  display: grid;
  gap: 0.4rem;
  min-width: 0;
  flex: 1 1 20rem;
}
.page-hero__eyebrow {
  color: var(--hero-accent);
  font-size: var(--font-size-sm);
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}
.page-hero h1 {
  margin: 0;
  font-size: clamp(1.65rem, 3vw, 2.25rem);
  line-height: 1.2;
  overflow-wrap: anywhere;
}
.page-hero p {
  max-width: 42rem;
  margin: 0;
  color: var(--color-text-secondary);
  line-height: 1.5;
}
.page-hero__actions {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  flex-wrap: wrap;
}
@media (max-width: 42rem) {
  .page-hero__actions {
    width: 100%;
  }
}
</style>
