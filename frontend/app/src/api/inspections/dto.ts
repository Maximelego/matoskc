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
  direction: InspectionDirection;
  answers: Record<string, InspectionAnswer>;
};

export type InspectionDirection = "departure" | "return";

export type PreviousDefect = {
  questionId: string;
  observation: string;
  photoUrls: string[];
};

export type PreviousInspection = {
  id: string;
  performedAt: string;
  defects: PreviousDefect[];
};

export type InspectionRecord = {
  id: string;
  equipmentId: string;
  equipmentName: string;
  serialNumber: string;
  direction: InspectionDirection;
  performedAt: string;
  operatorFirstName: string;
  templateVersion: number;
  sections: {
    id: string;
    title: string;
    answers: {
      questionId: string;
      label: string;
      value: "compliant" | "nonCompliant" | string | number;
      observation?: string;
      photoUrls: string[];
    }[];
  }[];
};


// Proposed backend contracts. Legacy types above remain for the current mock UI.
export type InspectionTypeDto = "Departure" | "Return";
export type InspectionStatusDto = "Draft" | "Validated";
export type ConformityDto = "Compliant" | "NonCompliant" | "NotApplicable";
export type InspectionAnswerDto = {
  id: string;
  templatePointId: string;
  result: ConformityDto | null;
  textValue: string | null;
  numberValue: number | null;
  choiceValue: string | null;
  observation: string | null;
  photoIds: string[];
  existingDefectId: string | null;
};
export type SaveInspectionAnswerDto = Omit<InspectionAnswerDto, "id">;
export type InspectionDto = {
  id: string;
  rentalId: string;
  equipmentId: string;
  templateVersionId: string;
  type: InspectionTypeDto;
  status: InspectionStatusDto;
  operatorFirstName: string;
  startedAt: string;
  validatedAt: string | null;
  answers: InspectionAnswerDto[];
};
export type CreateInspectionDto = {
  rentalId: string;
  type: InspectionTypeDto;
  operatorFirstName: string;
};
export type SaveInspectionDraftDto = { answers: SaveInspectionAnswerDto[] };
export type ValidateInspectionDto = {
  submissionId: string;
  acceptance?: {
    signerName: string;
    signerCapacity?: string | null;
    acceptedTextVersion: string;
    signatureId: string;
  };
};
export type ListInspectionsDto = { inspections: InspectionDto[] };
export type InspectionFiltersDto = { rentalId?: string; equipmentId?: string; type?: InspectionTypeDto; status?: InspectionStatusDto };
export type InspectionMediaDto = { id: string; inspectionId: string; answerId: string | null; url: string; contentType: string; uploadedAt: string };
