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

export const mockEquipmentsApi = {
  list(filters: EquipmentFilters = {}) {
    return Promise.resolve({
      data: [
        {
          id: "1",
          name: "Equipment 1",
          description: "Description for Equipment 1",
        },
        {
          id: "2",
          name: "Equipment 2",
          description: "Description for Equipment 2",
        },
      ],
      total: 2,
    });
  },

  getById(id: string) {
    return Promise.resolve({
      id,
      name: `Equipment ${id}`,
      description: `Description for Equipment ${id}`,
    });
  },

  create(dto: CreateEquipmentDto) {
    return Promise.resolve({
      id: "3",
    });
  },
};
