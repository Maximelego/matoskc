
import type { InspectionRecord } from "./dto";

// Données de démonstration. À remplacer par les endpoints de consultation des inspections.
// Les GUID et les noms correspondent aux équipements de DevelopmentData.cs.
const records: InspectionRecord[] = [
  {
    id: "30000000-0000-0000-0000-000000000001",
    equipmentId: "20000000-0000-0000-0000-000000000002",
    equipmentName: "Machine à souffler Rockster II",
    serialNumber: "DEV-SOUFFLEUR-002",
    direction: "departure",
    performedAt: "2026-01-15T09:30:00Z",
    operatorFirstName: "Camille",
    templateVersion: 1,
    sections: [{
      id: "condition", title: "Contrôle du matériel", answers: [
        { questionId: "accessories", label: "Accessoires et tuyaux présents", value: "compliant", photoUrls: [] },
        { questionId: "housing", label: "État du carter", value: "nonCompliant", observation: "Rayure sur le carter latéral.", photoUrls: [] },
        { questionId: "startup", label: "Mise en marche et commandes", value: "compliant", photoUrls: [] },
      ],
    }],
  },
  {
    id: "30000000-0000-0000-0000-000000000002",
    equipmentId: "20000000-0000-0000-0000-000000000006",
    equipmentName: "Chariot élévateur Jungheinrich EFG 216",
    serialNumber: "DEV-CHARIOT-003",
    direction: "departure",
    performedAt: "2026-01-15T10:45:00Z",
    operatorFirstName: "Alex",
    templateVersion: 1,
    sections: [{
      id: "condition", title: "Contrôle du matériel", answers: [
        { questionId: "forks", label: "État des fourches", value: "nonCompliant", observation: "Marque d’usure sur la fourche gauche.", photoUrls: [] },
        { questionId: "mast", label: "État du mât", value: "compliant", photoUrls: [] },
        { questionId: "brakes", label: "Freinage", value: "compliant", photoUrls: [] },
      ],
    }],
  },
  {
    id: "30000000-0000-0000-0000-000000000003",
    equipmentId: "20000000-0000-0000-0000-000000000011",
    equipmentName: "Camion plateau Iveco Daily",
    serialNumber: "DEV-CAMION-002",
    direction: "departure",
    performedAt: "2026-01-15T14:00:00Z",
    operatorFirstName: "Morgan",
    templateVersion: 1,
    sections: [{
      id: "condition", title: "Contrôle du matériel", answers: [
        { questionId: "body", label: "Carrosserie et benne", value: "nonCompliant", observation: "Éraflure sur le plateau arrière.", photoUrls: [] },
        { questionId: "tires", label: "État des pneus", value: "compliant", photoUrls: [] },
        { questionId: "lights", label: "Éclairage et signalisation", value: "compliant", photoUrls: [] },
      ],
    }],
  },
];

export const adminInspectionsApi = {
  list(): Promise<InspectionRecord[]> {
    return Promise.resolve(structuredClone(records));
  },
  getById(id: string): Promise<InspectionRecord> {
    const record = records.find((item) => item.id === id);
    return record
      ? Promise.resolve(structuredClone(record))
      : Promise.reject(new Error("État des lieux introuvable."));
  },
};
