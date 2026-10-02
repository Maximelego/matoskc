<script setup lang="ts">
import type { InspectionAnswer, InspectionSection } from "../../api/inspections/dto";
import InspectionQuestion from "./InspectionQuestion.vue";

defineProps<{ section: InspectionSection; answers: Record<string, InspectionAnswer>; errors: Record<string, string[]> }>();
const emit = defineEmits<{ update: [answer: InspectionAnswer] }>();
</script>

<template>
  <section class="inspection-section" :aria-labelledby="`section-${section.id}`">
    <header>
      <h2 :id="`section-${section.id}`">{{ section.title }}</h2>
      <p v-if="section.description">{{ section.description }}</p>
    </header>
    <InspectionQuestion
      v-for="question in section.questions"
      :key="question.id"
      :question="question"
      :answer="answers[question.id]"
      :errors="errors[question.id]"
      @update="emit('update', $event)"
    />
  </section>
</template>

<style scoped lang="scss">
.inspection-section { display: grid; gap: 1rem; min-width: 0; }
.inspection-section h2 { margin: 0; }
.inspection-section header p { margin: 0.35rem 0 0; color: var(--color-text-secondary); }
</style>
