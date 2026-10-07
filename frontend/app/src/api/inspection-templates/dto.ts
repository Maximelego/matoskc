export type CheckResponseType = "Conformity" | "Choice" | "Text" | "Number";
export type PhotoRequirement = "Never" | "Always" | "WhenNonCompliant";

export type TemplatePointDto = {
  id: string;
  label: string;
  instructions: string | null;
  position: number;
  responseType: CheckResponseType;
  isRequired: boolean;
  photoRequirement: PhotoRequirement;
  illustrationId: string | null;
  choices: string[] | null;
};
export type TemplateStepDto = { id: string; title: string; position: number; points: TemplatePointDto[] };
export type InspectionTemplateVersionDto = {
  id: string;
  templateId: string;
  equipmentCategoryId: string;
  versionNumber: number;
  publishedAt: string;
  steps: TemplateStepDto[];
};
export type InspectionTemplateSummaryDto = {
  id: string;
  equipmentCategoryId: string;
  name: string;
  isActive: boolean;
  currentVersionId: string | null;
};
export type ListInspectionTemplatesDto = { templates: InspectionTemplateSummaryDto[] };
export type PublishTemplateVersionDto = {
  steps: Array<{ title: string; position: number; points: Array<{
    label: string; instructions?: string | null; position: number;
    responseType: CheckResponseType; isRequired: boolean;
    photoRequirement: PhotoRequirement; illustrationId?: string | null; choices?: string[] | null;
  }> }>;
};
export type CreateInspectionTemplateDto = { equipmentCategoryId: string; name: string };
