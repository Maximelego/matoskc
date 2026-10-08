import { api } from "../client";
import type { CreateMaintenanceEntryDto, ListMaintenanceEntriesDto, MaintenanceEntryDto } from "./dto";
export const maintenanceApi = {
  list(equipmentId: string) {
    return api.get<ListMaintenanceEntriesDto>("/maintenance-entries", { query: { equipmentId } });
  },
  getById(id: string) { return api.get<MaintenanceEntryDto>(`/maintenance-entries/${encodeURIComponent(id)}`); },
  create(dto: CreateMaintenanceEntryDto) {
    return api.post<MaintenanceEntryDto, CreateMaintenanceEntryDto>("/maintenance-entries", dto);
  },
};
