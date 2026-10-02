import type { InspectionEquipmentDto, InspectionTemplateDto } from "./dto";

// Données de démonstration : aucun appel réseau ni enregistrement.
const blowerTemplate: InspectionTemplateDto = {
  id: "blower-depart",
  version: 1,
  equipmentCategoryId: "blower",
  title: "État des lieux de départ — machine à souffler",
  sections: [
    {
      id: "identification",
      title: "Identification et accessoires",
      description: "Vérifiez les éléments remis au locataire.",
      questions: [
        {
          id: "accessories",
          kind: "condition",
          label: "Accessoires et tuyaux présents",
          required: true,
          photoRequiredWhen: "nonCompliant",
        },
      ],
    },
    {
      id: "exterior",
      title: "État extérieur",
      questions: [
        {
          id: "housing",
          kind: "condition",
          label: "État du carter",
          required: true,
          photoRequiredWhen: "nonCompliant",
        },
        {
          id: "wheels",
          kind: "condition",
          label: "État des roues et poignées",
          required: true,
          photoRequiredWhen: "nonCompliant",
        },
        {
          id: "cable",
          kind: "condition",
          label: "État du câble électrique",
          required: true,
          photoRequiredWhen: "nonCompliant",
        },
      ],
    },
    {
      id: "operation",
      title: "Fonctionnement",
      questions: [
        {
          id: "startup",
          kind: "condition",
          label: "Mise en marche et commandes",
          required: true,
          photoRequiredWhen: "nonCompliant",
        },
        {
          id: "hose",
          kind: "condition",
          label: "État du tuyau et des raccords",
          required: true,
          photoRequiredWhen: "nonCompliant",
        },
        {
          id: "counter",
          kind: "number",
          label: "Compteur horaire",
          required: false,
          min: 0,
          unit: "h",
        },
      ],
    },
  ],
};

const availableEquipment: InspectionEquipmentDto[] = [
  {
    id: "blower-001",
    name: "Souffleuse ISOVER 1",
    serialNumber: "ISO-001",
    equipmentCategoryId: "blower",
    status: "Available",
    photoUrl: null,
  },
  {
    id: "blower-002",
    name: "Souffleuse ISOVER 2",
    serialNumber: "ISO-002",
    equipmentCategoryId: "blower",
    status: "Available",
    photoUrl: null,
  },
  {
    id: "blower-003",
    name: "Souffleuse ISOVER 3",
    serialNumber: "ISO-003",
    equipmentCategoryId: "blower",
    status: "Available",
    photoUrl: null,
  },
];

export const inspectionsApi = {
  getEquipment(id: string): Promise<InspectionEquipmentDto> {
    const equipment = availableEquipment.find((item) => item.id === id);
    return equipment
      ? Promise.resolve(structuredClone(equipment))
      : Promise.reject(new Error("Matériel introuvable ou indisponible pour un départ."));
  },
  getDepartureTemplate(categoryId: string): Promise<InspectionTemplateDto> {
    if (categoryId !== blowerTemplate.equipmentCategoryId) {
      return Promise.reject(new Error("Aucun modèle de démonstration pour cette catégorie."));
    }
    return Promise.resolve(structuredClone(blowerTemplate));
  },
};
