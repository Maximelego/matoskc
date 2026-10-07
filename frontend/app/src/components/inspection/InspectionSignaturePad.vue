<script setup lang="ts">
import { onMounted, ref } from "vue";
const emit = defineEmits<{ change: [file: File | null] }>();
const canvas = ref<HTMLCanvasElement | null>(null);
const drawing = ref(false);
const hasStroke = ref(false);
function coords(event: PointerEvent): [number, number] {
  const element = canvas.value!;
  const rect = element.getBoundingClientRect();
  return [(event.clientX - rect.left) * element.width / rect.width, (event.clientY - rect.top) * element.height / rect.height];
}
function begin(event: PointerEvent): void {
  const element = canvas.value; if (!element) return;
  element.setPointerCapture(event.pointerId);
  const context = element.getContext("2d"); if (!context) return;
  const [x, y] = coords(event); context.beginPath(); context.moveTo(x, y);
  drawing.value = true;
}
function move(event: PointerEvent): void {
  if (!drawing.value || !canvas.value) return;
  const context = canvas.value.getContext("2d"); if (!context) return;
  const [x, y] = coords(event); context.lineTo(x, y); context.stroke();
  hasStroke.value = true;
}
function end(): void {
  if (!drawing.value) return;
  drawing.value = false;
  canvas.value?.toBlob(blob => {
    if (blob && hasStroke.value) emit("change", new File([blob], "signature.png", { type: "image/png" }));
  }, "image/png");
}
function clear(): void {
  const element = canvas.value; if (!element) return;
  const context = element.getContext("2d"); if (!context) return;
  context.fillStyle = "#fff"; context.fillRect(0, 0, element.width, element.height);
  context.strokeStyle = "#171717"; context.lineWidth = 3; context.lineCap = "round";
  hasStroke.value = false; emit("change", null);
}
onMounted(clear);
</script>
<template>
  <div class="signature-pad">
    <canvas ref="canvas" width="720" height="230" aria-label="Zone de signature" @pointerdown="begin" @pointermove="move" @pointerup="end" @pointercancel="end" />
    <button type="button" @click="clear">Effacer la signature</button>
  </div>
</template>
<style scoped lang="scss">
.signature-pad { display: grid; gap: .5rem; justify-items: start; }
canvas { display: block; box-sizing: border-box; width: min(100%, 36rem); height: 12rem; border: 1px solid var(--color-border); border-radius: .5rem; background: #fff; touch-action: none; }
button { min-height: 2.75rem; padding: .45rem .8rem; color: var(--color-text); background: var(--color-surface-secondary); border: 1px solid var(--color-border); border-radius: .5rem; cursor: pointer; }
</style>
