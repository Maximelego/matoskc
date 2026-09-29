// Matches the current JSON contracts returned by MatosKC.Api.
export interface EquipmentDto {
  id: string;
  name: string;
  serialNumber: string;
  equipmentCategoryId: string;
}

export interface CreateEquipmentDto {
  name: string;
  categoryId: string;
  serialNumber: string;
}

export interface ListEquipmentsDto {
  equipments: EquipmentDto[];
}

// ASP.NET Core currently serializes EquipmentStatus as its numeric enum value.
export type EquipmentStatus = 0 | 1 | 2 | 3 | 4 | 5;

export interface EquipmentFilters {
  categoryId?: string;
  status?: EquipmentStatus;
  search?: string;
}
