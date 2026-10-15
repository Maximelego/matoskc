export type DefectStatus = "Open" | "Resolved";
export type DefectOccurrenceDto = {
  id: string; inspectionId: string; inspectionAnswerId: string;
  description: string; observation: string; observedAt: string; photoIds: string[];
};
export type DefectDto = {
  id: string; equipmentId: string; inspectionId: string; inspectionAnswerId: string;
  description: string; observation: string; reportedAt: string; status: DefectStatus;
  resolvedByMaintenanceId: string | null; resolvedByAccountId: string | null;
  resolvedAt: string | null; occurrences: DefectOccurrenceDto[];
};
export type DefectPageDto = { items: DefectDto[]; total: number; offset: number; limit: number };
