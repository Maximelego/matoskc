export type InspectionTypeDto = "Departure" | "Return";
export type InspectionStatusDto = "Draft" | "Validated";
export type InspectionResultDto = "Compliant" | "NonCompliant";

export type ClientInputDto = {
  displayName: string;
  email: string;
  phone: string;
  agencyCode: number;
  commandValue: string;
};
export type RentalDto = {
  id: string;
  clientId: string;
  equipmentId: string;
  status: "Draft" | "Active" | "Completed";
  createdAt: string;
  departureInspectionId: string | null;
  returnInspectionId: string | null;
  departedAt: string | null;
  returnedAt: string | null;
  client: ClientInputDto;
};
export type InspectionAnswerDto = {
  id: string;
  templatePointId: string;
  result: InspectionResultDto;
  observation: string | null;
  photoIds: string[];
};
export type InspectionDto = {
  id: string;
  rentalId: string;
  equipmentId: string;
  templateId: string;
  type: InspectionTypeDto;
  status: InspectionStatusDto;
  createdByAccountId: string;
  operatorFirstName: string;
  startedAt: string;
  validatedAt: string | null;
  answers: InspectionAnswerDto[];
  signerName: string | null;
  signedAt: string | null;
  acceptanceText: string | null;
  hasReport: boolean;
};
export type InspectionTemplateDto = {
  id: string;
  equipmentCategoryId: string;
  createdAt: string;
  points: { id: string; templateId: string; description: string; position: number; photoId: string | null }[];
};
export type CreateDepartureDto = { equipmentId: string; client: ClientInputDto; operatorFirstName: string };
export type SaveDraftDto = {
  operatorFirstName: string;
  answers: { templatePointId: string; result: InspectionResultDto; observation: string | null }[];
};
export type SignInspectionDto = { signerName: string; signaturePngBase64: string; accepted: boolean };
export type ListInspectionsDto = { inspections: InspectionDto[] };
export type InspectionFiltersDto = {
  rentalId?: string; type?: InspectionTypeDto; status?: InspectionStatusDto; offset?: number; limit?: number;
};
export type InspectionMediaDto = {
  id: string; inspectionId: string; answerId: string; fileName: string; contentType: string; contentLength: number;
};
