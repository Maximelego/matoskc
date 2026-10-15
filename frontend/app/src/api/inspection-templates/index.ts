import { api } from "../client";
import type { CreateInspectionTemplateDto, InspectionTemplateDto, InspectionTemplatePhotoDto } from "./dto";

export const inspectionTemplatesApi = {
  getById(id: string) {
    return api.get<InspectionTemplateDto>(`/inspection-templates/${encodeURIComponent(id)}`);
  },
  getCurrentForCategory(categoryId: string) {
    return api.get<InspectionTemplateDto>(
      `/equipment-categories/${encodeURIComponent(categoryId)}/inspection-template`,
    );
  },
  create(dto: CreateInspectionTemplateDto) {
    return api.post<InspectionTemplateDto, CreateInspectionTemplateDto>("/inspection-templates/", dto);
  },
  uploadPhoto(file: File) {
    return api.postFile<InspectionTemplatePhotoDto>("/inspection-templates/photos", file);
  },
  getPhoto(photoId: string) {
    return api.getBlob(`/inspection-templates/photos/${encodeURIComponent(photoId)}`);
  },
};
