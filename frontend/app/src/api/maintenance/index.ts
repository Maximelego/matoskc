import { api } from "../client";
import type { MaintenanceDto, MaintenancePageDto, RecordMaintenanceDto } from "./dto";
const path = (equipmentId: string) => `/equipments/${encodeURIComponent(equipmentId)}/maintenances`;
export const maintenanceApi = {
  list(equipmentId: string, offset = 0, limit = 50) {
    return api.get<MaintenancePageDto>(path(equipmentId), { query: { offset, limit } });
  },
  getById(equipmentId: string, maintenanceId: string) {
    return api.get<MaintenanceDto>(`${path(equipmentId)}/${encodeURIComponent(maintenanceId)}`);
  },
  create(equipmentId: string, dto: RecordMaintenanceDto) {
    return api.post<MaintenanceDto, RecordMaintenanceDto>(path(equipmentId), dto);
  },
};
