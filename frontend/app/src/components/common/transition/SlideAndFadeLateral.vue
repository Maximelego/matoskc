<script lang="ts" setup>
import { onMounted, ref } from "vue";

const mounted = ref(false);

onMounted(() => {
  mounted.value = true;
});
</script>

<template>
  <Transition
    v-if="mounted"
    name="slide-and-fade-lateral"
    mode="out-in"
  >
    <slot />
  </Transition>
</template>

<style scoped lang="scss">
.slide-and-fade-lateral-enter-active,
.slide-and-fade-lateral-leave-active {
  transition: all 0.3s ease;
}
.slide-and-fade-lateral-enter-from,
.slide-and-fade-lateral-leave-to {
  opacity: 0;
  transform: translateX(1rem);
}
.slide-and-fade-lateral-enter-to,
.slide-and-fade-lateral-leave-from {
  opacity: 1;
  transform: translateX(0);
}
</style>
