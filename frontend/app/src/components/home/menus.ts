import type { HomeTileProps } from "./HomeTile.vue";

export const AgencyMenus: HomeTileProps[] = [
  {
    title: "Etats des Lieux",
    description: "Liste des Etats des Lieux.",
    icon: "clipboard",
    to: "/inspections",
  },
];

export const AdminMenus: HomeTileProps[] = [
  {
    title: "Équipements",
    description: "Consulter et gérer le matériel enregistré.",
    icon: "equipment",
    to: "/equipments",
    accent: "secondary",
  },
  {
    title: "Entretiens",
    description: "Consigner les travaux et résoudre les défauts.",
    icon: "maintenance",
    to: "/admin/maintenances",
    accent: "secondary",
  },
  {
    title: "Modèles d’état des lieux",
    description: "Créer les points de contrôle par catégorie.",
    icon: "clipboard",
    to: "/admin/inspection-templates",
    accent: "secondary",
  },
  {
    title: "Suivi des états des lieux",
    description: "Consulter les états des lieux réalisés.",
    icon: "document",
    to: "/admin/inspections",
    accent: "secondary",
  },
];

export const SuperAdminMenus: HomeTileProps[] = [
  {
    title: "Gestion des agences",
    description: "Consulter et administrer les agences.",
    icon: "home",
    to: "/super-admin/agencies",
    accent: "secondary",
  },
  {
    title: "Gestion des utilisateurs",
    description: "Administrer les comptes et leurs accès.",
    icon: "users",
    to: "/super-admin/accounts",
    accent: "secondary",
  },
];
