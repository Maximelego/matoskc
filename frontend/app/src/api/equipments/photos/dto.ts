export type EquipmentPhotoUploadDto = File;
export type EquipmentPhotoDto = Blob;

export interface EquipmentPhotoMetadataDto {
  id: string;
  equipmentId: string;
  fileName: string;
  contentType: string;
  contentLength: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}
