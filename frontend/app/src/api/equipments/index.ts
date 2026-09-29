import { api } from "../client";
import type { CreatedResourceDto } from "../types";
import type { CreateEquipmentDto, EquipmentDto, EquipmentFilters, ListEquipmentsDto } from "./dto";

export const equipmentsApi = {
  list(filters: EquipmentFilters = {}) {
    return api.get<ListEquipmentsDto>("/api/equipments", { query: { ...filters } });
  },

  listMock(filters: EquipmentFilters = {}) {
    
  },

  getById(id: string) {
    return api.get<EquipmentDto>(`/api/equipments/${encodeURIComponent(id)}`);
  },

  create(dto: CreateEquipmentDto) {
    return api.post<CreatedResourceDto, CreateEquipmentDto>("/api/equipments", dto);
  },
};
