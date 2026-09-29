export interface EquipmentCategoryDto {
  id: string;
  name: string;
  description: string | null;
}

export interface CreateEquipmentCategoryDto {
  name: string;
  description: string | null;
}

export interface ListEquipmentCategoriesDto {
  equipmentCategories: EquipmentCategoryDto[];
}
