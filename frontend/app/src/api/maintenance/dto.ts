export type MaintenanceEntryDto = {
  id: string; equipmentId: string; defectId: string | null;
  note: string; performedAt: string; operatorName: string;
};
export type CreateMaintenanceEntryDto = {
  equipmentId: string; defectId?: string | null;
  note: string; performedAt: string; operatorName: string;
};
export type ListMaintenanceEntriesDto = { entries: MaintenanceEntryDto[] };
