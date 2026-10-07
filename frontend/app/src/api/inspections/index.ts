// Legacy demo contracts remain available until the wizard and admin screen use the HTTP contracts.
export { inspectionsApi } from "./mock";
export { adminInspectionsApi } from "./admin";
export type {
  InspectionRecord, InspectionAnswer, InspectionDirection, InspectionDraft, InspectionQuestion,
  InspectionSection, InspectionTemplateDto, PhotoRule, PreviousDefect, PreviousInspection,
} from "./dto";
export type {
  InspectionTypeDto, InspectionStatusDto, ConformityDto, InspectionAnswerDto,
  SaveInspectionAnswerDto, InspectionDto, CreateInspectionDto, SaveInspectionDraftDto,
  ValidateInspectionDto, ListInspectionsDto, InspectionFiltersDto, InspectionMediaDto,
} from "./dto";

import { api } from "../client";
import type { CreateInspectionDto, InspectionDto, InspectionFiltersDto, InspectionMediaDto, ListInspectionsDto, SaveInspectionDraftDto, ValidateInspectionDto } from "./dto";

const path = (id: string) => `/api/inspections/${encodeURIComponent(id)}`;
export const inspectionHttpApi = {
  list(filters: InspectionFiltersDto = {}) {
    return api.get<ListInspectionsDto>("/api/inspections", { query: { ...filters } });
  },
  getById(id: string) { return api.get<InspectionDto>(path(id)); },
  create(dto: CreateInspectionDto) { return api.post<InspectionDto, CreateInspectionDto>("/api/inspections", dto); },
  saveDraft(id: string, dto: SaveInspectionDraftDto) {
    return api.put<InspectionDto, SaveInspectionDraftDto>(`${path(id)}/draft`, dto);
  },
  validate(id: string, dto: ValidateInspectionDto) {
    return api.post<InspectionDto, ValidateInspectionDto>(`${path(id)}/validation`, dto);
  },
  uploadPhoto(id: string, answerId: string, file: File) {
    return api.putFile<InspectionMediaDto>(`${path(id)}/answers/${encodeURIComponent(answerId)}/photos`, file);
  },
  uploadSignature(id: string, file: File) {
    return api.putFile<InspectionMediaDto>(`${path(id)}/signature`, file);
  },
  getDeparture(rentalId: string) {
    return api.get<InspectionDto | null>(`/api/rentals/${encodeURIComponent(rentalId)}/departure-inspection`);
  },
  getPhoto(id: string, photoId: string) {
    return api.getBlob(`${path(id)}/photos/${encodeURIComponent(photoId)}`);
  },
  getReport(id: string) { return api.getBlob(`${path(id)}/report`); },
};
