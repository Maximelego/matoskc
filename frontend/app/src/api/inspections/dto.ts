export type InspectionTypeDto = "Departure" | "Return";
export type InspectionStatusDto = "Draft" | "Validated" | "Cancelled";
export type InspectionResultDto = "Compliant" | "NonCompliant";
export type KnownDefectResultDto = "Unchanged" | "Worsened" | "MaintenanceCheckRequired";
export type KnownDefectDto = {
  id: string;
  defectId: string;
  description: string;
  historicalObservation: string;
  observedAt: string;
  historicalPhotoIds: string[];
  result: KnownDefectResultDto | null;
  observation: string | null;
  photoIds: string[];
};
export type InspectionFindingDto = {
  id: string;
  templatePointId: string;
  observation: string;
  photoIds: string[];
};

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
  knownDefects: KnownDefectDto[];
  findings: InspectionFindingDto[];
  signerName: string | null;
  signedAt: string | null;
  acceptanceText: string | null;
  hasReport: boolean;
  cancelledAt: string | null;
  cancelledByAccountId: string | null;
  cancellationReason: string | null;
};
export type InspectionTemplateDto = {
  id: string;
  equipmentCategoryId: string;
  agencyId: string;
  createdAt: string;
  points: {
    id: string;
    templateId: string;
    description: string;
    descriptionDetail: string;
    position: number;
    photoId: string | null;
  }[];
};
export type CreateDepartureDto = {
  equipmentId: string;
  client: ClientInputDto;
  operatorFirstName: string;
};
export type SaveDraftDto = {
  operatorFirstName: string;
  answers: { templatePointId: string; result: InspectionResultDto; observation: null }[];
  knownDefects: { defectId: string; result: KnownDefectResultDto; observation: string | null }[];
  findings: { id: string; templatePointId: string; observation: string }[];
  removedFindingIds: string[];
};
export type SignInspectionDto = {
  signerName: string;
  signaturePngBase64: string;
  accepted: boolean;
};
export type InspectionListItemDto = {
  id: string;
  rentalId: string;
  equipmentId: string;
  equipmentName: string;
  serialNumber: string;
  agencyId: string | null;
  agencyCode: number;
  clientName: string;
  commandNumber: string;
  templateId: string;
  type: InspectionTypeDto;
  status: InspectionStatusDto;
  operatorFirstName: string;
  startedAt: string;
  validatedAt: string | null;
  cancelledAt: string | null;
  hasReport: boolean;
};
export type ListInspectionsDto = {
  inspections: InspectionListItemDto[];
  total: number;
  offset: number;
  limit: number;
};
export type InspectionFiltersDto = {
  rentalId?: string;
  equipmentId?: string;
  agencyId?: string;
  commandNumber?: string;
  from?: string;
  to?: string;
  type?: InspectionTypeDto;
  status?: InspectionStatusDto;
  offset?: number;
  limit?: number;
};
export type InspectionMediaDto = {
  id: string;
  inspectionId: string;
  targetId?: string;
  fileName: string;
  contentType: string;
  contentLength: number;
};
