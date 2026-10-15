import type { RouteRecordRaw } from "vue-router";
import ListEquipmentView from "../views/EquipmentListView.vue";
import HomeView from "../views/HomeView.vue";
import InspectionView from "../views/InspectionView.vue";
import InspectionTemplateManagementView from "../views/InspectionTemplateManagementView.vue";
import MaintenanceView from "../views/MaintenanceView.vue";
import InspectionAdminView from "../views/InspectionAdminView.vue";
import LoginView from "../views/LoginView.vue";
import NotFoundView from "../views/NotFoundView.vue";
import AgencyManagementView from "../views/AgencyManagementView.vue";
import AccountManagementView from "../views/AccountManagementView.vue";

export const routes: RouteRecordRaw[] = [
  { path: "/", name: "Home", component: HomeView, meta: { title: "Accueil" } },
  { path: "/inspections", name: "Inspections", component: InspectionView, meta: { title: "États des lieux" } },
  { path: "/inspections/:equipmentId", name: "InspectionEquipment", component: InspectionView, meta: { title: "État des lieux" } },
  { path: "/admin/inspection-templates", name: "ManageInspectionTemplates", component: InspectionTemplateManagementView, meta: { title: "Modèles d’état des lieux", roles: ["Admin", "SuperAdmin"] } },
  { path: "/admin/inspections", name: "AdminInspections", component: InspectionAdminView, meta: { title: "Consultation des états des lieux", roles: ["Admin", "SuperAdmin"] } },
  { path: "/admin/maintenances", name: "ManageMaintenances", component: MaintenanceView, meta: { title: "Entretiens", roles: ["Admin", "SuperAdmin"] } },
  { path: "/equipments", name: "Equipments", component: ListEquipmentView, meta: { title: "Équipements" } },
  { path: "/super-admin/agencies", name: "ManageAgencies", component: AgencyManagementView, meta: { title: "Gestion des agences", roles: ["SuperAdmin"] } },
  { path: "/super-admin/accounts", name: "ManageAccounts", component: AccountManagementView, meta: { title: "Gestion des utilisateurs", roles: ["SuperAdmin"] } },
  { path: "/login", name: "Login", component: LoginView, meta: { title: "Connexion", public: true } },
  { path: "/:pathMatch(.*)*", name: "NotFound", component: NotFoundView, meta: { title: "Page introuvable", public: true } },
];
