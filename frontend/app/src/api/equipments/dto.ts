// Matches the current JSON contracts returned by MatosKC.Api.
export type EquipmentDto = {
  id: string;
  name: string;
  status: EquipmentStatus;
  serialNumber: string;
  equipmentCategoryId: string;
};

export type CreateEquipmentDto = {
  name: string;
  categoryId: string;
  serialNumber: string;
};

export type ListEquipmentsDto = {
  equipments: EquipmentDto[];
};

// ASP.NET Core currently serializes EquipmentStatus as its numeric enum value.
export type EquipmentStatus = 0 | 1 | 2 | 3 | 4 | 5;

export type EquipmentFilters = {
  categoryId?: string;
  status?: EquipmentStatus;
  search?: string;
};
