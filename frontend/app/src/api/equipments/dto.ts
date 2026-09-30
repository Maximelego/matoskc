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

export const EquipmentStatus = {
  Available: 0 as EquipmentStatus,
  Rented: 1 as EquipmentStatus,
  Maintenance: 2 as EquipmentStatus,
  Reserved: 3 as EquipmentStatus,
  Lost: 4 as EquipmentStatus,
  Retired: 5 as EquipmentStatus,
};

export type EquipmentFilters = {
  categoryId?: string;
  status?: EquipmentStatus;
  search?: string;
};
