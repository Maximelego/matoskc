import { api } from "../client";
import type { CreatedResourceDto } from "../types";
import type {
  CreateEquipmentCategoryDto,
  EquipmentCategoryDto,
  ListEquipmentCategoriesDto,
} from "./dto";

export const equipmentCategoriesApi = {
  list() {
    return api.get<ListEquipmentCategoriesDto>("/equipment-categories/");
  },

  getById(id: string) {
    return api.get<EquipmentCategoryDto>(`/equipment-categories/${encodeURIComponent(id)}`);
  },

  create(dto: CreateEquipmentCategoryDto) {
    return api.post<CreatedResourceDto, CreateEquipmentCategoryDto>("/equipment-categories/", dto);
  },
};
