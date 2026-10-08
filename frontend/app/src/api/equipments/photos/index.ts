import { api, ApiError } from "../../client";
import type { EquipmentPhotoDto, EquipmentPhotoMetadataDto, EquipmentPhotoUploadDto } from "./dto";

function path(equipmentId: string): string {
  return `/equipments/${encodeURIComponent(equipmentId)}/photos`;
}

async function firstPhoto(equipmentId: string): Promise<EquipmentPhotoMetadataDto | undefined> {
  const photos = await equipmentPhotosApi.list(equipmentId);

  // Keep a stable preview when an equipment has multiple photographs.
  return photos
    .slice()
    .sort(
      (left, right) =>
        left.createdAtUtc.localeCompare(right.createdAtUtc) || left.id.localeCompare(right.id),
    )[0];
}

export const equipmentPhotosApi = {
  list(equipmentId: string): Promise<EquipmentPhotoMetadataDto[]> {
    return api.get<EquipmentPhotoMetadataDto[]>(path(equipmentId));
  },

  download(equipmentId: string, photoId: string): Promise<EquipmentPhotoDto> {
    return api.getBlob(`${path(equipmentId)}/${encodeURIComponent(photoId)}`);
  },

  upload(equipmentId: string, file: EquipmentPhotoUploadDto): Promise<EquipmentPhotoMetadataDto> {
    return api.postFile<EquipmentPhotoMetadataDto>(path(equipmentId), file);
  },

  replace(
    equipmentId: string,
    photoId: string,
    file: EquipmentPhotoUploadDto,
  ): Promise<EquipmentPhotoMetadataDto> {
    return api.putFile<EquipmentPhotoMetadataDto>(
      `${path(equipmentId)}/${encodeURIComponent(photoId)}`,
      file,
    );
  },

  remove(equipmentId: string, photoId: string): Promise<void> {
    return api.delete(`${path(equipmentId)}/${encodeURIComponent(photoId)}`);
  },

  // Compatibility helpers for the current single-photograph interface.
  async get(equipmentId: string): Promise<EquipmentPhotoDto> {
    const photo = await firstPhoto(equipmentId);

    if (!photo) {
      throw new ApiError(404, { title: "No equipment photograph" });
    }

    return this.download(equipmentId, photo.id);
  },

  async put(equipmentId: string, file: EquipmentPhotoUploadDto): Promise<void> {
    const photo = await firstPhoto(equipmentId);

    if (photo) {
      await this.replace(equipmentId, photo.id, file);
    } else {
      await this.upload(equipmentId, file);
    }
  },

  async delete(equipmentId: string): Promise<void> {
    const photo = await firstPhoto(equipmentId);

    if (photo) {
      await this.remove(equipmentId, photo.id);
    }
  },
};
