import { api } from "../client";
import type { CreateInspectionTemplateDto, InspectionTemplateDto } from "./dto";

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
};
