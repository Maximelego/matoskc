export interface EquipmentCategoryDto {
  id: string;
  name: string;
  description: string | null;
  agencyId: string;
}

export interface CreateEquipmentCategoryDto {
  name: string;
  description: string | null;
  agencyId?: string;
}

export type UpdateEquipmentCategoryDto = CreateEquipmentCategoryDto;

export interface ListEquipmentCategoriesDto {
  equipmentCategories: EquipmentCategoryDto[];
}
