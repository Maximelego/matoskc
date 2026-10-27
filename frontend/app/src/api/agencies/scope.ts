import { computed, ref, watch } from "vue";
import { authSession } from "../auth/session";
import { api } from "../client";
import type { AgencyDto } from "./dto";

const selectedAgencyId = ref("");
const agencies = ref<AgencyDto[]>([]);
const storagePrefix = "matoskc-selected-agency:";

export const agencyScope = {
  selectedAgencyId,
  agencies,
  label(id: string | null | undefined): string {
    if (!id) return "Agence inconnue";
    const agency = agencies.value.find((item) => item.id === id);
    return agency ? `${agency.code} — ${agency.name}` : id;
  },
  isSuperAdmin: computed(() => authSession.account.value?.role === "SuperAdmin"),
  activeAgencyId: computed(() =>
    authSession.account.value?.role === "SuperAdmin"
      ? selectedAgencyId.value || null
      : (authSession.account.value?.agencyId ?? null),
  ),
  key: computed(
    () =>
      `${authSession.account.value?.id ?? "anonymous"}:${authSession.account.value?.role === "SuperAdmin" ? selectedAgencyId.value || "all" : (authSession.account.value?.agencyId ?? "none")}`,
  ),
  select(id: string): void {
    if (authSession.account.value?.role !== "SuperAdmin") return;
    selectedAgencyId.value = id;
    try {
      window.localStorage.setItem(storagePrefix + authSession.account.value.id, id);
    } catch {
      // Le choix reste actif tant que l'application est ouverte.
    }
  },
};

watch(
  () => authSession.account.value?.id,
  (id) => {
    selectedAgencyId.value = "";
    agencies.value = [];
    if (!id || authSession.account.value?.role !== "SuperAdmin") return;
    try {
      selectedAgencyId.value = window.localStorage.getItem(storagePrefix + id) ?? "";
    } catch {
      // Le stockage peut être désactivé par le navigateur.
    }
  },
  { immediate: true },
);

api.setAgencyResolver(() =>
  authSession.account.value?.role === "SuperAdmin" ? selectedAgencyId.value || null : null,
);
