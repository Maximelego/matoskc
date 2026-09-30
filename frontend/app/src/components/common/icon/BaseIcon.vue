<script setup lang="ts">
export type IconName =
  | "refresh"
  | "view"
  | "edit"
  | "add"
  | "delete"
  | "close"
  | "search"
  | "chevron-left"
  | "chevron-right"
  | "chevron-down"
  | "chevron-up"
  | "arrow-left"
  | "arrow-right"
  | "check"
  | "warning"
  | "error"
  | "info"
  | "equipment"
  | "category"
  | "truck"
  | "forklift"
  | "tools"
  | "maintenance"
  | "calendar"
  | "clock"
  | "user"
  | "users"
  | "lock"
  | "unlock"
  | "login"
  | "logout"
  | "filter"
  | "sort"
  | "menu"
  | "more"
  | "camera"
  | "image"
  | "upload"
  | "download"
  | "document"
  | "clipboard"
  | "signature"
  | "qr-code"
  | "settings"
  | "home"
  | "save";

withDefaults(
  defineProps<{
    name: IconName;
    size?: number;
  }>(),
  { size: 20 },
);
</script>

<template>
  <svg
    :width="size"
    :height="size"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="2"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
    focusable="false"
  >
    <template v-if="name === 'refresh'">
      <path d="M20 11a8 8 0 0 0-14.9-4" />
      <path d="M4 4v5h5" />
      <path d="M4 13a8 8 0 0 0 14.9 4" />
      <path d="M20 20v-5h-5" />
    </template>

    <template v-else-if="name === 'view'">
      <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z" />
      <circle
        cx="12"
        cy="12"
        r="2.5"
      />
    </template>

    <template v-else-if="name === 'edit'">
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L9 17l-4 1 1-4Z" />
    </template>

    <template v-else-if="name === 'add'">
      <path d="M12 5v14" />
      <path d="M5 12h14" />
    </template>

    <template v-else-if="name === 'delete'">
      <path d="M3 6h18" />
      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
      <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
      <path d="M10 11v6M14 11v6" />
    </template>

    <template v-else-if="name === 'close'">
      <path d="M18 6 6 18M6 6l12 12" />
    </template>

    <template v-else-if="name === 'search'">
      <circle
        cx="11"
        cy="11"
        r="7"
      />
      <path d="m20 20-4-4" />
    </template>

    <template v-else-if="name === 'chevron-left'">
      <path d="m15 18-6-6 6-6" />
    </template>

    <template v-else-if="name === 'chevron-right'">
      <path d="m9 18 6-6-6-6" />
    </template>

    <template v-else-if="name === 'chevron-down'">
      <path d="m6 9 6 6 6-6" />
    </template>

    <template v-else-if="name === 'chevron-up'">
      <path d="m6 15 6-6 6 6" />
    </template>

    <template v-else-if="name === 'arrow-left'">
      <path d="m12 19-7-7 7-7M5 12h14" />
    </template>

    <template v-else-if="name === 'arrow-right'">
      <path d="m12 5 7 7-7 7M19 12H5" />
    </template>

    <template v-else-if="name === 'check'">
      <path d="m5 12 4 4L19 6" />
    </template>

    <template v-else-if="name === 'warning'">
      <path
        d="M10.3 3.7 2.4 17.4A2 2 0 0 0 4.1 20h15.8a2 2 0 0 0 1.7-2.6L13.7 3.7a2 2 0 0 0-3.4 0Z"
      />
      <path d="M12 9v4M12 17h.01" />
    </template>

    <template v-else-if="name === 'error'">
      <circle
        cx="12"
        cy="12"
        r="9"
      />
      <path d="m9 9 6 6m0-6-6 6" />
    </template>

    <template v-else-if="name === 'info'">
      <circle
        cx="12"
        cy="12"
        r="9"
      />
      <path d="M12 11v5M12 8h.01" />
    </template>

    <template v-else-if="name === 'equipment'">
      <rect
        x="3"
        y="7"
        width="18"
        height="13"
        rx="2"
      />
      <path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
      <path d="M3 12h18M10 12v2h4v-2" />
    </template>

    <template v-else-if="name === 'category'">
      <rect
        x="3"
        y="3"
        width="7"
        height="7"
        rx="1"
      />
      <rect
        x="14"
        y="3"
        width="7"
        height="7"
        rx="1"
      />
      <rect
        x="3"
        y="14"
        width="7"
        height="7"
        rx="1"
      />
      <rect
        x="14"
        y="14"
        width="7"
        height="7"
        rx="1"
      />
    </template>

    <template v-else-if="name === 'truck'">
      <path d="M3 6h11v11H3zM14 10h4l3 3v4h-7z" />
      <circle
        cx="7"
        cy="18"
        r="2"
      />
      <circle
        cx="18"
        cy="18"
        r="2"
      />
    </template>

    <template v-else-if="name === 'forklift'">
      <path d="M3 17h12M5 17V9h7l3 5v3M9 9V6h5l3 5" />
      <path d="M18 4v13h4M18 13h3" />
      <circle
        cx="7"
        cy="19"
        r="2"
      />
      <circle
        cx="14"
        cy="19"
        r="2"
      />
    </template>

    <template v-else-if="name === 'tools'">
      <path d="M14 7a5 5 0 0 0-6.7 6.7L3 17l4 4 4.3-4.3A5 5 0 0 0 18 10l-3 3-4-4 3-3Z" />
    </template>

    <template v-else-if="name === 'maintenance'">
      <path d="M20 7a5 5 0 0 1-6.7 6.7L7 20l-3-3 6.3-6.3A5 5 0 0 1 17 4l-3 3 3 3 3-3Z" />
    </template>

    <template v-else-if="name === 'calendar'">
      <rect
        x="3"
        y="5"
        width="18"
        height="16"
        rx="2"
      />
      <path d="M7 3v4M17 3v4M3 10h18" />
    </template>

    <template v-else-if="name === 'clock'">
      <circle
        cx="12"
        cy="12"
        r="9"
      />
      <path d="M12 7v5l3 2" />
    </template>

    <template v-else-if="name === 'user'">
      <circle
        cx="12"
        cy="8"
        r="4"
      />
      <path d="M4 21a8 8 0 0 1 16 0" />
    </template>

    <template v-else-if="name === 'users'">
      <circle
        cx="9"
        cy="8"
        r="3"
      />
      <path d="M3 20a6 6 0 0 1 12 0M16 5a3 3 0 0 1 0 6M17 14a6 6 0 0 1 4 6" />
    </template>

    <template v-else-if="name === 'lock'">
      <rect
        x="4"
        y="10"
        width="16"
        height="11"
        rx="2"
      />
      <path d="M8 10V7a4 4 0 0 1 8 0v3M12 15v2" />
    </template>

    <template v-else-if="name === 'unlock'">
      <rect
        x="4"
        y="10"
        width="16"
        height="11"
        rx="2"
      />
      <path d="M8 10V7a4 4 0 0 1 7.5-2M12 15v2" />
    </template>

    <template v-else-if="name === 'login'">
      <path d="M10 4H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h5" />
      <path d="m14 8 4 4-4 4M18 12H8" />
    </template>

    <template v-else-if="name === 'logout'">
      <path d="M14 4h5a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-5" />
      <path d="m10 8-4 4 4 4M6 12h10" />
    </template>

    <template v-else-if="name === 'filter'">
      <path d="M3 5h18l-7 8v6l-4 2v-8L3 5Z" />
    </template>

    <template v-else-if="name === 'sort'">
      <path d="m7 4-3 3 3 3M4 7h11M17 20l3-3-3-3M20 17H9" />
    </template>

    <template v-else-if="name === 'menu'">
      <path d="M4 6h16M4 12h16M4 18h16" />
    </template>

    <template v-else-if="name === 'more'">
      <circle
        cx="5"
        cy="12"
        r="1"
      />
      <circle
        cx="12"
        cy="12"
        r="1"
      />
      <circle
        cx="19"
        cy="12"
        r="1"
      />
    </template>

    <template v-else-if="name === 'camera'">
      <path d="M3 7h4l2-3h6l2 3h4v13H3V7Z" />
      <circle
        cx="12"
        cy="13"
        r="4"
      />
    </template>

    <template v-else-if="name === 'image'">
      <rect
        x="3"
        y="3"
        width="18"
        height="18"
        rx="2"
      />
      <circle
        cx="8"
        cy="8"
        r="1"
      />
      <path d="m21 16-5-5-5 5-2-2-6 6" />
    </template>

    <template v-else-if="name === 'upload'">
      <path d="M12 16V3m-4 4 4-4 4 4" />
      <path d="M4 15v4a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-4" />
    </template>

    <template v-else-if="name === 'download'">
      <path d="M12 3v13m-4-4 4 4 4-4" />
      <path d="M4 16v3a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-3" />
    </template>

    <template v-else-if="name === 'document'">
      <path d="M6 2h8l5 5v15H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2Z" />
      <path d="M14 2v6h5M8 13h8M8 17h8" />
    </template>

    <template v-else-if="name === 'clipboard'">
      <rect
        x="5"
        y="5"
        width="14"
        height="17"
        rx="2"
      />
      <rect
        x="9"
        y="2"
        width="6"
        height="5"
        rx="1"
      />
      <path d="M9 12h6M9 16h6" />
    </template>

    <template v-else-if="name === 'signature'">
      <path d="M3 20h18M4 15c2-5 3-7 5-7 3 0-1 9 2 9 1 0 2-3 4-3 1 0 1 3 3 3 1 0 2-1 3-2" />
    </template>

    <template v-else-if="name === 'qr-code'">
      <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3z" />
      <path d="M5 5h3v3H5zM16 5h3v3h-3zM5 16h3v3H5z" />
      <path d="M14 14h3v3h-3zM20 14v3M17 20h4M14 20v1M20 20v1" />
    </template>

    <template v-else-if="name === 'settings'">
      <circle
        cx="12"
        cy="12"
        r="3"
      />
      <path
        d="M10 2h4l.6 2.3 1.8.8 2.1-1.2 2.8 2.8-1.2 2.1.8 1.8L23 11v4l-2.3.6-.8 1.8 1.2 2.1-2.8 2.8-2.1-1.2-1.8.8L14 23h-4l-.6-2.3-1.8-.8-2.1 1.2-2.8-2.8 1.2-2.1-.8-1.8L1 15v-4l2.3-.6.8-1.8-1.2-2.1 2.8-2.8 2.1 1.2 1.8-.8L10 2Z"
      />
    </template>

    <template v-else-if="name === 'home'">
      <path d="m3 10 9-7 9 7v11H3V10Z" />
      <path d="M9 21v-7h6v7" />
    </template>

    <template v-else-if="name === 'save'">
      <path d="M4 3h13l3 3v15H4V3Z" />
      <path d="M7 3v6h10V3M7 21v-8h10v8" />
    </template>
  </svg>
</template>
