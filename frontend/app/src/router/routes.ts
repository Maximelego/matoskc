import ListEquipmentView from "../views/EquipmentListView.vue";
import HomeView from "../views/HomeView.vue";
import LoginView from "../views/LoginView.vue";
import NotFoundView from "../views/NotFoundView.vue";

export const routes = [
  {
    path: "/",
    name: "Home",
    component: HomeView,
    meta: { title: "Home" },
  },
  {
    path: "/equipments",
    name: "Equipments",
    component: ListEquipmentView,
    meta: { title: "Equipments" },
  },
  { path: "/login", name: "Login", component: LoginView, meta: { title: "Login" } },
  // will match everything and put it under `route.params.pathMatch`
  {
    path: "/:pathMatch(.*)*",
    name: "NotFound",
    component: NotFoundView,
    meta: { title: "Not Found" },
  },
];
