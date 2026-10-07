export type EquipmentEventType = "InspectionValidated" | "InternalCheckValidated" | "StatusChanged" | "DefectReported" | "DefectResolved" | "MaintenanceRecorded";
export type EquipmentEventDto = {
  id: string; equipmentId: string; type: EquipmentEventType;
  occurredAt: string; operatorName: string | null;
  relatedEntityId: string | null; summary: string;
};
export type EquipmentHistoryDto = { events: EquipmentEventDto[] };
