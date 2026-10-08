import { createRouter, createWebHistory } from "vue-router";
import { authSession } from "../api/auth/session";
import { routes } from "./routes";

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

router.beforeEach(async to => {
  try {
    const account = await authSession.ensure();
    if (to.meta.public) {
      if (to.name === "Login" && account) return { name: "Home" };
      return true;
    }
    if (!account) return { name: "Login", query: { redirect: to.fullPath } };
    const roles = to.meta.roles as string[] | undefined;
    if (roles && !roles.includes(account.role)) return { name: "Home" };
    return true;
  } catch {
    if (to.name === "Login") return true;
    return { name: "Login", query: { redirect: to.fullPath, unavailable: "1" } };
  }
});

window.addEventListener("matoskc:unauthorized", () => {
  if (router.currentRoute.value.meta.public) return;
  void router.replace({ name: "Login", query: { redirect: router.currentRoute.value.fullPath } });
});
