import type { InspectionTemplateDto, PreviousInspection } from "./dto";

// Catégories et identifiants repris de DevelopmentData.cs. Les équipements proviennent de l’API réelle.
const categoryNames = ["Machines à souffler", "Chariots élévateurs", "Tire-palettes", "Camions", "Véhicules utilitaires"] as const;
const checks: Record<number, [string, string][]> = {
  1: [["accessories", "Accessoires et tuyaux présents"], ["housing", "État du carter"], ["wheels", "État des roues et poignées"], ["cable", "État du câble électrique"], ["startup", "Mise en marche et commandes"], ["hose", "État du tuyau et des raccords"]],
  2: [["forks", "État des fourches"], ["mast", "État du mât"], ["tires", "État des pneus"], ["brakes", "Freinage"], ["controls", "Commandes et avertisseur"]],
  3: [["forks", "État des fourches"], ["wheels", "État des galets et roues"], ["handle", "État du timon"], ["lift", "Levage et descente"]],
  4: [["body", "Carrosserie et benne"], ["tires", "État des pneus"], ["lights", "Éclairage et signalisation"], ["cab", "Cabine et accessoires"], ["brakes", "Freinage"]],
  5: [["body", "Carrosserie"], ["tires", "État des pneus"], ["lights", "Éclairage et signalisation"], ["cargo", "Espace de chargement"], ["cab", "Cabine et accessoires"]],
};
const categoryPrefix = "10000000-0000-0000-0000-";

export const inspectionsApi = {
  getTemplate(categoryId: string): Promise<InspectionTemplateDto> {
    const number = Number(categoryId.slice(categoryPrefix.length));
    if (!categoryId.startsWith(categoryPrefix) || !Number.isInteger(number) || !(number in checks))
      return Promise.reject(new Error("Aucune liste de vérification pour cette catégorie."));
    const template: InspectionTemplateDto = {
      id: `inspection-category-${number}`, version: 1, equipmentCategoryId: categoryId,
      title: `État des lieux — ${categoryNames[number - 1]}`,
      sections: [{ id: "condition", title: "Contrôle du matériel", questions: checks[number]!.map(([id, label]) => ({ id, label, kind: "condition" as const, required: true, photoRequiredWhen: "nonCompliant" as const })) }],
    };
    return Promise.resolve(structuredClone(template));
  },
  getPreviousInspection(equipmentId: string): Promise<PreviousInspection | null> {
    // Exemples explicites liés aux GUID des équipements empruntés en base de développement.
    const examples: Record<string, PreviousInspection> = {
      "20000000-0000-0000-0000-000000000002": { id: "demo-prior-2", performedAt: "2026-01-15", defects: [{ questionId: "housing", observation: "Rayure sur le carter latéral.", photoUrls: [] }] },
      "20000000-0000-0000-0000-000000000006": { id: "demo-prior-6", performedAt: "2026-01-15", defects: [{ questionId: "forks", observation: "Marque d’usure sur la fourche gauche.", photoUrls: [] }] },
      "20000000-0000-0000-0000-000000000011": { id: "demo-prior-11", performedAt: "2026-01-15", defects: [{ questionId: "body", observation: "Éraflure sur le plateau arrière.", photoUrls: [] }] },
    };
    return Promise.resolve(examples[equipmentId.toLowerCase()] ? structuredClone(examples[equipmentId.toLowerCase()]) : null);
  },
};
