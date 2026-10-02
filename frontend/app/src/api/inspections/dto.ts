export type PhotoRule = "never" | "always" | "nonCompliant";

type QuestionBase = {
  id: string;
  label: string;
  help?: string;
  required: boolean;
};

export type InspectionQuestion =
  | (QuestionBase & { kind: "condition"; photoRequiredWhen: PhotoRule })
  | (QuestionBase & { kind: "text"; maxLength?: number })
  | (QuestionBase & { kind: "number"; unit?: string; min?: number; max?: number });

export type InspectionSection = {
  id: string;
  title: string;
  description?: string;
  questions: InspectionQuestion[];
};

export type InspectionTemplateDto = {
  id: string;
  version: number;
  equipmentCategoryId: string;
  title: string;
  sections: InspectionSection[];
};

export type InspectionAnswer =
  | { questionId: string; kind: "condition"; value: "compliant" | "nonCompliant" | null; observation: string; photos: File[] }
  | { questionId: string; kind: "text"; value: string }
  | { questionId: string; kind: "number"; value: number | null };

export type InspectionDraft = {
  templateId: string;
  templateVersion: number;
  operatorFirstName: string;
  equipmentId: string;
  answers: Record<string, InspectionAnswer>;
};

export type InspectionEquipmentDto = {
  id: string;
  name: string;
  serialNumber: string;
  equipmentCategoryId: string;
  status: "Available";
  photoUrl: string | null;
};
