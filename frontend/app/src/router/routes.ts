import ListEquipmentView from "../views/EquipmentListView.vue";
import HomeView from "../views/HomeView.vue";
import InspectionView from "../views/InspectionView.vue";
import InspectionAdminView from "../views/InspectionAdminView.vue";
import LoginView from "../views/LoginView.vue";
import NotFoundView from "../views/NotFoundView.vue";
import AgencyManagementView from "../views/AgencyManagementView.vue";
import AccountManagementView from "../views/AccountManagementView.vue";

export const routes = [
  { path: "/", name: "Home", component: HomeView, meta: { title: "Home" } },
  { path: "/inspections", name: "Inspections", component: InspectionView, meta: { title: "Inspections" } },
  { path: "/inspections/:equipmentId", name: "InspectionEquipment", component: InspectionView, meta: { title: "État des lieux" } },
  { path: "/admin/inspections", name: "AdminInspections", component: InspectionAdminView, meta: { title: "Consultation des états des lieux" } },
  { path: "/equipments", name: "Equipments", component: ListEquipmentView, meta: { title: "Equipments" } },
  { path: "/super-admin/agencies", name: "ManageAgencies", component: AgencyManagementView, meta: { title: "Gestion des agences" } },
  { path: "/super-admin/accounts", name: "ManageAccounts", component: AccountManagementView, meta: { title: "Gestion des utilisateurs" } },
  { path: "/login", name: "Login", component: LoginView, meta: { title: "Login" } },
  { path: "/:pathMatch(.*)*", name: "NotFound", component: NotFoundView, meta: { title: "Not Found" } },
];
