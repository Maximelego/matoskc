<script setup lang="ts">
import QrcodeVue from "qrcode.vue";
import { computed, defineProps, nextTick, onBeforeUnmount, ref } from "vue";

const props = defineProps<{
  equipmentId: string;
  equipmentName: string;
}>();

type PrintFormat = "small" | "large";

const dialog = ref<HTMLDialogElement | null>(null);
const preview = ref<HTMLElement | null>(null);
const format = ref<PrintFormat>("small");
const printing = ref(false);
const error = ref("");

let printFrame: HTMLIFrameElement | null = null;

const inspectionUrl = computed(() => {
  const baseUrl = (import.meta.env.VITE_PUBLIC_APP_URL?.trim() || window.location.origin).replace(
    /\/+$/,
    "",
  );

  return `${baseUrl}/inspections/${encodeURIComponent(props.equipmentId)}`;
});

function openDialog() {
  error.value = "";
  dialog.value?.showModal();
}

function closeDialog() {
  dialog.value?.close();
}

function onDialogClick(event: MouseEvent) {
  if (event.target !== dialog.value || !dialog.value) return;

  const bounds = dialog.value.getBoundingClientRect();
  const outside =
    event.clientX < bounds.left ||
    event.clientX > bounds.right ||
    event.clientY < bounds.top ||
    event.clientY > bounds.bottom;

  if (outside) closeDialog();
}

function cleanupPrintFrame() {
  printFrame?.remove();
  printFrame = null;
}

async function printQRCode() {
  if (printing.value) return;

  printing.value = true;
  error.value = "";

  try {
    await nextTick();

    const svg = preview.value?.querySelector("svg");
    if (!svg) {
      throw new Error("Le QR code n’est pas encore disponible.");
    }

    cleanupPrintFrame();

    const frame = document.createElement("iframe");
    frame.title = "Impression du QR code";
    frame.style.cssText = "position:fixed;width:1px;height:1px;left:-10000px;top:0;border:0;";

    document.body.appendChild(frame);
    printFrame = frame;

    const doc = frame.contentDocument;
    const printWindow = frame.contentWindow;

    if (!doc || !printWindow) {
      throw new Error("Impossible de préparer l’impression.");
    }

    const small = format.value === "small";

    doc.open();
    doc.write(`<!DOCTYPE html>
      <html lang="fr">
        <head>
          <meta charset="utf-8">
          <title>QR code équipement</title>
          <style>
            @page {
              size: A4 portrait;
              margin: 0;
            }

            * {
              box-sizing: border-box;
            }

            html, body {
              margin: 0;
              padding: 0;
              background: white;
              color: black;
              font-family: Arial, sans-serif;
            }

            .poster {
              width: 210mm;
              height: ${small ? "148.5mm" : "296mm"};
              padding: ${small ? "10mm" : "18mm"};
              display: flex;
              flex-direction: column;
              align-items: center;
              justify-content: center;
              gap: ${small ? "5mm" : "10mm"};
              text-align: center;
              break-inside: avoid;
              ${small ? "border-bottom: 0.25mm dashed #aaa;" : ""}
            }

            h1 {
              margin: 0;
              width: 100%;
              font-size: ${small ? "22pt" : "32pt"};
              line-height: 1.15;
              overflow-wrap: anywhere;
              flex-shrink: 0;
            }

            .qr {
              width: ${small ? "75mm" : "140mm"};
              height: ${small ? "75mm" : "140mm"};
              flex-shrink: 0;
            }

            .qr svg {
              display: block;
              width: 100%;
              height: 100%;
            }

            .instruction {
              margin: 0;
              font-size: ${small ? "15pt" : "22pt"};
              font-weight: 700;
              line-height: 1.3;
              flex-shrink: 0;
            }

            .brand {
              margin: 0;
              font-size: ${small ? "10pt" : "12pt"};
              color: #555;
              flex-shrink: 0;
            }
          </style>
        </head>
        <body>
          <main class="poster">
            <h1></h1>
            <div class="qr"></div>
            <p class="instruction">
              Scannez pour réaliser l’état des lieux
            </p>
            <p class="brand">MatosKC</p>
          </main>
        </body>
      </html>`);
    doc.close();

    const heading = doc.querySelector("h1");
    const qrContainer = doc.querySelector(".qr");

    if (!heading || !qrContainer) {
      throw new Error("Impossible de préparer l’affiche.");
    }

    doc.title = `État des lieux — ${props.equipmentName}`;
    heading.textContent = props.equipmentName;
    qrContainer.appendChild(doc.importNode(svg, true));

    await doc.fonts.ready;

    // Réduit la taille du titre si le nom est particulièrement long.
    const minFontSize = small ? 14 : 18;
    let titleSize = small ? 22 : 32;
    const maxTitleHeight = (small ? 22 : 45) * (96 / 25.4);

    while (heading.getBoundingClientRect().height > maxTitleHeight && titleSize > minFontSize) {
      titleSize -= 1;
      heading.style.fontSize = `${titleSize}pt`;
    }

    // Force le calcul de la mise en page avant l'impression.
    doc.body.getBoundingClientRect();

    printWindow.focus();
    printWindow.print();
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : "Impossible de lancer l’impression.";
  } finally {
    printing.value = false;
  }
}

onBeforeUnmount(cleanupPrintFrame);
</script>

<template>
  <div
    class="equipment-qr"
    @click.stop
  >
    <QrcodeVue
      :value="inspectionUrl"
      :size="160"
      :margin="4"
      level="M"
      render-as="svg"
      foreground="#000000"
      background="#ffffff"
    />

    <button
      type="button"
      class="button primary"
      @click="openDialog"
    >
      Imprimer le QR code
    </button>

    <Teleport to="body">
      <dialog
        ref="dialog"
        class="print-dialog"
        aria-label="Imprimer le QR code"
        @click.stop="onDialogClick"
      >
        <header class="dialog-header">
          <h2>Imprimer le QR code</h2>

          <button
            type="button"
            class="close-button"
            aria-label="Fermer"
            @click="closeDialog"
          >
            ×
          </button>
        </header>

        <fieldset class="format-options">
          <legend>Format de l’affiche</legend>

          <label :class="{ selected: format === 'small' }">
            <input
              v-model="format"
              type="radio"
              value="small"
            />
            <span>
              <strong>Petit format</strong>
              <small>½ feuille A4 — 210 × 148,5 mm</small>
            </span>
          </label>

          <label :class="{ selected: format === 'large' }">
            <input
              v-model="format"
              type="radio"
              value="large"
            />
            <span>
              <strong>Grand format</strong>
              <small>1 feuille A4 — 210 × 297 mm</small>
            </span>
          </label>
        </fieldset>

        <div class="preview-background">
          <article
            ref="preview"
            class="poster-preview"
            :class="format"
            aria-label="Aperçu de l’affiche"
          >
            <h3>{{ equipmentName }}</h3>

            <QrcodeVue
              :value="inspectionUrl"
              :size="400"
              :margin="4"
              level="M"
              render-as="svg"
              foreground="#000000"
              background="#ffffff"
              class="preview-qr"
            />

            <p class="preview-instruction">Scannez pour réaliser l’état des lieux</p>

            <p class="preview-brand">MatosKC</p>
          </article>
        </div>

        <p class="print-help">
          Papier A4, taille réelle (100 %), en-têtes et pieds de page désactivés. Le petit format
          occupe la moitié supérieure de la feuille, avec un repère de découpe.
        </p>

        <p
          v-if="error"
          class="error"
          role="alert"
        >
          {{ error }}
        </p>

        <footer class="dialog-actions">
          <button
            type="button"
            class="button"
            @click="closeDialog"
          >
            Fermer
          </button>

          <button
            type="button"
            class="button primary"
            :disabled="printing"
            @click="printQRCode"
          >
            {{ printing ? "Préparation…" : "Imprimer" }}
          </button>
        </footer>
      </dialog>
    </Teleport>
  </div>
</template>

<style scoped lang="scss">
.equipment-qr {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.button {
  min-height: 2.75rem;
  padding: 0.7rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 0.6rem;
  color: var(--color-text);
  background: var(--color-surface);
  font: inherit;
  font-weight: var(--font-weight-semibold);
  cursor: pointer;

  &:hover:not(:disabled) {
    background: var(--color-surface-hover);
  }

  &.primary {
    border-color: var(--color-primary);
    color: var(--color-text-on-primary);
    background: var(--color-primary);

    &:hover:not(:disabled) {
      border-color: var(--color-primary-hover);
      background: var(--color-primary-hover);
    }
  }

  &:disabled {
    opacity: 0.6;
    cursor: wait;
  }

  &:focus-visible {
    outline: 3px solid var(--color-focus);
    outline-offset: 3px;
  }
}

.print-dialog {
  box-sizing: border-box;
  width: min(40rem, calc(100vw - 2rem));
  max-height: calc(100dvh - 2rem);
  padding: 1.5rem;
  overflow-y: auto;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  color: var(--color-text);
  background: var(--color-surface);
  box-shadow: 0 1.25rem 4rem var(--color-shadow);
  font-family: var(--font-family-sans);
  color-scheme: inherit;

  &::backdrop {
    background: var(--color-overlay);
  }
}

.dialog-header,
.dialog-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.dialog-header h2 {
  margin: 0;
  color: var(--color-text);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
}

.close-button {
  display: grid;
  place-items: center;
  flex: none;
  width: 2.75rem;
  height: 2.75rem;
  padding: 0;
  border: 0;
  border-radius: 0.5rem;
  color: var(--color-text);
  background: transparent;
  font-size: 2rem;
  cursor: pointer;

  &:hover {
    background: var(--color-surface-hover);
  }

  &:focus-visible {
    outline: 3px solid var(--color-focus);
    outline-offset: 2px;
  }
}

.format-options {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  min-width: 0;
  margin: 1rem 0;
  padding: 0;
  border: 0;

  legend {
    margin-bottom: 0.6rem;
    color: var(--color-text);
    font-weight: var(--font-weight-semibold);
  }

  label {
    display: flex;
    align-items: center;
    flex: 1 1 12.5rem;
    gap: 0.6rem;
    padding: 0.8rem;
    border: 2px solid var(--color-border);
    border-radius: 0.75rem;
    color: var(--color-text);
    background: var(--color-surface);
    cursor: pointer;

    &:hover {
      background: var(--color-surface-hover);
    }

    &.selected {
      border-color: var(--color-primary);
      background: var(--color-primary-soft);
    }

    &:focus-within {
      outline: 3px solid var(--color-focus);
      outline-offset: 2px;
    }
  }

  input {
    accent-color: var(--color-primary);
  }

  strong,
  small {
    display: block;
  }

  small {
    margin-top: 0.25rem;
    color: var(--color-text-secondary);
    font-weight: var(--font-weight-regular);
  }
}

.preview-background {
  display: flex;
  justify-content: center;
  padding: 1rem;
  border-radius: 0.75rem;
  background: var(--color-surface-secondary);
}

.poster-preview {
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.8rem;
  width: 100%;
  max-width: 21.25rem;
  padding: 1rem;
  color: #000;
  background: #fff;
  text-align: center;
  font-family: Arial, sans-serif;
  box-shadow: 0 0.125rem 0.5rem var(--color-shadow);

  &.small {
    aspect-ratio: 210 / 148.5;
    gap: 0.45rem;
    border-bottom: 1px dashed #aaa;

    .preview-qr {
      width: 36%;
    }
  }

  &.large {
    aspect-ratio: 210 / 297;

    .preview-qr {
      width: 67%;
    }
  }

  h3 {
    max-width: 100%;
    margin: 0;
    color: #000;
    font-size: var(--font-size-lg);
    overflow-wrap: anywhere;
  }
}

.preview-qr {
  display: block;
  flex-shrink: 0;
  height: auto;
}

.preview-instruction {
  margin: 0;
  color: #000;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-bold);
}

.preview-brand {
  margin: 0;
  color: #555;
  font-size: var(--font-size-xs);
}

.print-help {
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  line-height: var(--line-height-normal);
}

.error {
  color: var(--color-danger);
}

.dialog-actions {
  justify-content: flex-end;
  margin-top: 1rem;
}

@media (max-width: 42rem) {
  .print-dialog {
    padding: 1rem;
  }

  .dialog-actions {
    flex-wrap: wrap;

    .button {
      flex: 1;
    }
  }
}
</style>
