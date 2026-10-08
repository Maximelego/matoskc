// Legacy demo contracts remain available until the wizard and admin screen use the HTTP contracts.
export { adminInspectionsApi } from "./admin";
export type {
  ConformityDto,
  CreateInspectionDto,
  InspectionAnswer,
  InspectionAnswerDto,
  InspectionDirection,
  InspectionDraft,
  InspectionDto,
  InspectionFiltersDto,
  InspectionMediaDto,
  InspectionQuestion,
  InspectionRecord,
  InspectionSection,
  InspectionStatusDto,
  InspectionTemplateDto,
  InspectionTypeDto,
  ListInspectionsDto,
  PhotoRule,
  PreviousDefect,
  PreviousInspection,
  SaveInspectionAnswerDto,
  SaveInspectionDraftDto,
  ValidateInspectionDto,
} from "./dto";
export { inspectionsApi } from "./mock";

import { api } from "../client";
import type {
  CreateInspectionDto,
  InspectionDto,
  InspectionFiltersDto,
  InspectionMediaDto,
  ListInspectionsDto,
  SaveInspectionDraftDto,
  ValidateInspectionDto,
} from "./dto";

const path = (id: string) => `/inspections/${encodeURIComponent(id)}`;
export const inspectionHttpApi = {
  list(filters: InspectionFiltersDto = {}) {
    return api.get<ListInspectionsDto>("/inspections", { query: { ...filters } });
  },
  getById(id: string) {
    return api.get<InspectionDto>(path(id));
  },
  create(dto: CreateInspectionDto) {
    return api.post<InspectionDto, CreateInspectionDto>("/inspections", dto);
  },
  saveDraft(id: string, dto: SaveInspectionDraftDto) {
    return api.put<InspectionDto, SaveInspectionDraftDto>(`${path(id)}/draft`, dto);
  },
  validate(id: string, dto: ValidateInspectionDto) {
    return api.post<InspectionDto, ValidateInspectionDto>(`${path(id)}/validation`, dto);
  },
  uploadPhoto(id: string, answerId: string, file: File) {
    return api.putFile<InspectionMediaDto>(
      `${path(id)}/answers/${encodeURIComponent(answerId)}/photos`,
      file,
    );
  },
  uploadSignature(id: string, file: File) {
    return api.putFile<InspectionMediaDto>(`${path(id)}/signature`, file);
  },
  getDeparture(rentalId: string) {
    return api.get<InspectionDto | null>(
      `/rentals/${encodeURIComponent(rentalId)}/departure-inspection`,
    );
  },
  getPhoto(id: string, photoId: string) {
    return api.getBlob(`${path(id)}/photos/${encodeURIComponent(photoId)}`);
  },
  getReport(id: string) {
    return api.getBlob(`${path(id)}/report`);
  },
};
