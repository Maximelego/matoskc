import type { SaveInspectionAnswerDto, InspectionMediaDto } from "../inspections/dto";
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
