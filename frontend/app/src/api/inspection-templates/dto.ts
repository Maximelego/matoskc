export interface InspectionTemplatePointDto {
  id: string;
  templateId: string;
  description: string;
  descriptionDetail: string;
  position: number;
  photoId: string | null;
}

export interface InspectionTemplateDto {
  id: string;
  equipmentCategoryId: string;
  createdAt: string;
  points: InspectionTemplatePointDto[];
}

export interface CreateInspectionTemplateDto {
  equipmentCategoryId: string;
  points: Array<{ description: string; photoId: string | null }>;
}
