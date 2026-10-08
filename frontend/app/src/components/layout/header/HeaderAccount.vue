<script setup lang="ts">
import { computed, defineEmits, defineProps } from "vue";
import { authSession } from "../../../api/auth/session";
import BaseIcon from "../../common/icon/BaseIcon.vue";

defineProps<{
  loggingOut?: boolean;
}>();

const emit = defineEmits<{
  logout: [];
}>();

const account = authSession.account;

const initials = computed(() => {
  const name = account.value?.displayName?.trim();
  if (!name) return "?";

  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0).toLocaleUpperCase("fr"))
    .join("");
});

const roleLabel = computed(() => {
  switch (account.value?.role) {
    case "SuperAdmin":
      return "Super administration";
    case "Admin":
      return "Administration";
    case "Agency":
      return "Agence";
    default:
      return "";
  }
});
</script>

<template>
  <div
    v-if="account"
    class="header-account"
  >
    <div
      class="header-account__avatar"
      aria-hidden="true"
    >
      {{ initials }}
    </div>

    <div class="header-account__identity">
      <span class="header-account__name">
        {{ account.displayName }}
      </span>
      <span class="header-account__detail">
        {{ account.email || roleLabel }}
      </span>
    </div>

    <button
      type="button"
      class="header-account__logout"
      :disabled="loggingOut"
      :aria-label="loggingOut ? 'Déconnexion en cours' : 'Se déconnecter'"
      @click="emit('logout')"
    >
      <BaseIcon
        name="logout"
        :size="18"
      />
      <span>{{ loggingOut ? "Déconnexion…" : "Se déconnecter" }}</span>
    </button>
  </div>
</template>

<style scoped lang="scss">
.header-account {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;

  &__avatar {
    display: grid;
    flex: none;
    place-items: center;
    width: 2.5rem;
    height: 2.5rem;
    border: 1px solid var(--color-border);
    border-radius: 50%;
    color: var(--color-primary);
    background: var(--color-primary-soft);
    font-size: 0.8rem;
    font-weight: 700;
  }

  &__identity {
    display: grid;
    min-width: 0;
    line-height: 1.25;
  }

  &__name,
  &__detail {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__name {
    color: var(--color-text);
    font-weight: 600;
  }

  &__detail {
    margin-top: 0.15rem;
    color: var(--color-text-secondary);
    font-size: 0.8rem;
  }

  &__logout {
    display: inline-flex;
    flex: none;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    min-height: 2.5rem;
    margin-left: 0.5rem;
    padding: 0.5rem 0.75rem;
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    color: var(--color-text);
    background: transparent;
    font: inherit;
    cursor: pointer;
    transition:
      background-color 150ms ease,
      border-color 150ms ease;

    &:hover:not(:disabled) {
      border-color: var(--color-border-strong);
      background: var(--color-surface-hover);
    }

    &:focus-visible {
      outline: 3px solid var(--color-focus);
      outline-offset: 2px;
    }

    &:disabled {
      opacity: 0.6;
      cursor: wait;
    }
  }
}

@media (max-width: 42rem) {
  .header-account {
    gap: 0.5rem;

    &__identity {
      max-width: 9rem;
    }

    &__detail,
    &__logout span {
      display: none;
    }

    &__logout {
      width: 2.5rem;
      margin-left: 0;
      padding: 0;
    }
  }
}

@media (max-width: 24rem) {
  .header-account__identity {
    display: none;
  }
}
</style>
