import { createMemoryHistory, createRouter } from "vue-router";
import { routes } from "./routes.ts";

const isAuthenticated = false

export const router = createRouter({
  history: createMemoryHistory(),
  routes,
})

router.beforeEach(async (to, from) => {
        console.log(from.path)
        console.log(to.path)
  if (
    // make sure the user is authenticated
    !isAuthenticated &&
    to.path !== '/login'
  ) {
    // redirect the user to the login page
    return {path: "/login"}
  }
})