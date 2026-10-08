import { api } from "../client";
import type {
  CreateInspectionTemplateDto,
  InspectionTemplateSummaryDto,
  InspectionTemplateVersionDto,
  ListInspectionTemplatesDto,
  PublishTemplateVersionDto,
} from "./dto";

const path = (id: string) => `/inspection-templates/${encodeURIComponent(id)}`;
export const inspectionTemplatesApi = {
  list(categoryId?: string) {
    return api.get<ListInspectionTemplatesDto>("/inspection-templates", { query: { categoryId } });
  },
  getById(id: string) {
    return api.get<InspectionTemplateSummaryDto>(path(id));
  },
  getCurrentForCategory(categoryId: string) {
    return api.get<InspectionTemplateVersionDto>(
      `/equipment-categories/${encodeURIComponent(categoryId)}/inspection-template`,
    );
  },
  getVersionById(versionId: string) {
    return api.get<InspectionTemplateVersionDto>(
      `/inspection-template-versions/${encodeURIComponent(versionId)}`,
    );
  },
  getVersion(templateId: string, versionId: string) {
    return api.get<InspectionTemplateVersionDto>(
      `${path(templateId)}/versions/${encodeURIComponent(versionId)}`,
    );
  },
  create(dto: CreateInspectionTemplateDto) {
    return api.post<InspectionTemplateSummaryDto, CreateInspectionTemplateDto>(
      "/inspection-templates",
      dto,
    );
  },
  publishVersion(templateId: string, dto: PublishTemplateVersionDto) {
    return api.post<InspectionTemplateVersionDto, PublishTemplateVersionDto>(
      `${path(templateId)}/versions`,
      dto,
    );
  },
  setActive(id: string, isActive: boolean) {
    return api.patch<InspectionTemplateSummaryDto>(`${path(id)}/status`, { isActive });
  },
};
