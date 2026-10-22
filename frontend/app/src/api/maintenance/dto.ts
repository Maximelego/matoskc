export type MaintenanceDto = {
  id: string;
  equipmentId: string;
  performedAt: string;
  performedBy: string;
  workDescription: string;
  recordedByAccountId: string;
  recordedAt: string;
  resolvedDefectIds: string[];
};
export type RecordMaintenanceDto = {
  id: string;
  performedAt: string;
  performedBy: string;
  workDescription: string;
  resolvedDefectIds: string[];
};
export type MaintenancePageDto = {
  items: MaintenanceDto[];
  total: number;
  offset: number;
  limit: number;
};
