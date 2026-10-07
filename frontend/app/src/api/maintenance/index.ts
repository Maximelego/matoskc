import { api } from "../client";
import type { CreateMaintenanceEntryDto, ListMaintenanceEntriesDto, MaintenanceEntryDto } from "./dto";
export const maintenanceApi = {
  list(equipmentId: string) {
    return api.get<ListMaintenanceEntriesDto>("/api/maintenance-entries", { query: { equipmentId } });
  },
  getById(id: string) { return api.get<MaintenanceEntryDto>(`/api/maintenance-entries/${encodeURIComponent(id)}`); },
  create(dto: CreateMaintenanceEntryDto) {
    return api.post<MaintenanceEntryDto, CreateMaintenanceEntryDto>("/api/maintenance-entries", dto);
  },
};
