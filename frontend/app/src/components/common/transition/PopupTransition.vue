<script setup lang="ts">
defineEmits<{
  "after-leave": [];
}>();
</script>

<template>
  <Transition
    name="popup"
    appear
    @after-leave="$emit('after-leave')"
  >
    <slot />
  </Transition>
</template>

<style lang="scss">
.popup-enter-active,
.popup-leave-active {
  transition:
    opacity 180ms ease,
    transform 180ms ease;
}

.popup-enter-from,
.popup-leave-to {
  opacity: 0;
  transform: translateY(0.75rem) scale(0.97);
}

@media (prefers-reduced-motion: reduce) {
  .popup-enter-active,
  .popup-leave-active {
    transition: none;
  }
}
</style>
