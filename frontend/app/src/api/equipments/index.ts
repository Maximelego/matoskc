import { api } from "../client";
import type { CreatedResourceDto } from "../types";
import type {
  CreateEquipmentDto,
  EquipmentDto,
  EquipmentFilters,
  ListEquipmentsDto,
  UpdateEquipmentDto,
} from "./dto";

export const equipmentsApi = {
  list(filters: EquipmentFilters = {}) {
    return api.get<ListEquipmentsDto>("/equipments", { query: { ...filters } });
  },

  getById(id: string) {
    return api.get<EquipmentDto>(`/equipments/${encodeURIComponent(id)}`);
  },

  create(dto: CreateEquipmentDto) {
    return api.post<CreatedResourceDto, CreateEquipmentDto>("/equipments", dto);
  },

  update(id: string, dto: UpdateEquipmentDto) {
    return api.put<EquipmentDto, UpdateEquipmentDto>(`/equipments/${encodeURIComponent(id)}`, dto);
  },

  updateStatus(id: string, status: EquipmentDto["status"], reason: string) {
    return api.patch<EquipmentDto>(`/equipments/${encodeURIComponent(id)}/status`, {
      status,
      reason,
    });
  },
};
