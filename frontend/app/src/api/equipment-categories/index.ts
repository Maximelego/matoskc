import { api } from "../client";
import type { CreatedResourceDto } from "../types";
import type {
  CreateEquipmentCategoryDto,
  EquipmentCategoryDto,
  ListEquipmentCategoriesDto,
} from "./dto";

export const equipmentCategoriesApi = {
  list() {
    return api.get<ListEquipmentCategoriesDto>("/api/equipment-categories/");
  },

  getById(id: string) {
    return api.get<EquipmentCategoryDto>(`/api/equipment-categories/${encodeURIComponent(id)}`);
  },

  create(dto: CreateEquipmentCategoryDto) {
    return api.post<CreatedResourceDto, CreateEquipmentCategoryDto>(
      "/api/equipment-categories/",
      dto,
    );
  },
};
