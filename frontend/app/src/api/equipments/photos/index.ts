import { api } from "../../client";
import type { EquipmentPhotoDto, EquipmentPhotoUploadDto } from "./dto";

function path(equipmentId: string): string {
  return `/equipments/${encodeURIComponent(equipmentId)}/photo`;
}

export const equipmentPhotosApi = {
  get(equipmentId: string): Promise<EquipmentPhotoDto> {
    return api.getBlob(path(equipmentId));
  },
  put(equipmentId: string, file: EquipmentPhotoUploadDto): Promise<void> {
    return api.putFile(path(equipmentId), file);
  },
  delete(equipmentId: string): Promise<void> {
    return api.delete(path(equipmentId));
  },
};
