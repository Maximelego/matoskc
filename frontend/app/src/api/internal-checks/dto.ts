// Internal checks have not yet been migrated to the inspection API.
export type SaveInspectionAnswerDto = {
  templatePointId: string; result: "Compliant" | "NonCompliant" | null;
  observation: string | null; photoIds: string[];
};
export type InspectionMediaDto = { id: string; inspectionId: string; answerId: string; url: string; contentType: string; uploadedAt: string };
export type InternalCheckStatus = "Draft" | "Validated";
export type InternalCheckDto = {
  id: string; equipmentId: string; templateVersionId: string;
  operatorFirstName: string; status: InternalCheckStatus;
  startedAt: string; validatedAt: string | null;
  answers: SaveInspectionAnswerDto[];
};
export type CreateInternalCheckDto = { equipmentId: string; operatorFirstName: string };
export type SaveInternalCheckDraftDto = { answers: SaveInspectionAnswerDto[] };
export type ValidateInternalCheckDto = { submissionId: string };
export type ListInternalChecksDto = { checks: InternalCheckDto[] };
export type InternalCheckPhotoDto = InspectionMediaDto;
