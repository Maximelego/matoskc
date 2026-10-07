export type DefectStatus = "Open" | "Resolved";
export type DefectDto = {
  id: string; equipmentId: string; discoveredInInspectionId: string;
  templatePointId: string | null; description: string; status: DefectStatus;
  photoIds: string[]; createdAt: string; resolvedAt: string | null;
};
export type ListDefectsDto = { defects: DefectDto[] };
export type DefectFilters = { equipmentId?: string; status?: DefectStatus };
export type ResolveDefectDto = { resolutionNote: string };
