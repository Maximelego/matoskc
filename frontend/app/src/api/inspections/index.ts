import { api } from "../client";
import type {
  CreateDepartureDto, InspectionDto, InspectionFiltersDto, InspectionMediaDto,
  InspectionTemplateDto, ListInspectionsDto, RentalDto, SaveDraftDto, SignInspectionDto,
} from "./dto";

export type * from "./dto";
const path = (id: string) => `/inspections/${encodeURIComponent(id)}`;

export const inspectionHttpApi = {
  list: (filters: InspectionFiltersDto = {}) =>
    api.get<ListInspectionsDto>("/inspections", { query: { ...filters } }),
  getById: (id: string) => api.get<InspectionDto>(path(id)),
  createDeparture: (dto: CreateDepartureDto) =>
    api.post<InspectionDto, CreateDepartureDto>("/inspections/departure", dto),
  createReturn: (rentalId: string, operatorFirstName: string) =>
    api.post<InspectionDto>(`/rentals/${encodeURIComponent(rentalId)}/return-inspection`, { operatorFirstName }),
  saveDraft: (id: string, dto: SaveDraftDto) => api.put<InspectionDto, SaveDraftDto>(`${path(id)}/draft`, dto),
  sign: (id: string, dto: SignInspectionDto) => api.put<InspectionDto, SignInspectionDto>(`${path(id)}/signature`, dto),
  validate: (id: string) => api.post<InspectionDto>(`${path(id)}/validation`, {}),
  uploadPhoto: (id: string, answerId: string, file: File) =>
    api.postFile<InspectionMediaDto>(`${path(id)}/answers/${encodeURIComponent(answerId)}/photos`, file),
  removePhoto: (id: string, photoId: string) =>
    api.delete(`${path(id)}/photos/${encodeURIComponent(photoId)}`),
  getPhoto: (id: string, photoId: string) => api.getBlob(`${path(id)}/photos/${encodeURIComponent(photoId)}`),
  getSignature: (id: string) => api.getBlob(`${path(id)}/signature`),
  getReport: (id: string) => api.getBlob(`${path(id)}/report`),
  getRental: (id: string) => api.get<RentalDto>(`/rentals/${encodeURIComponent(id)}`),
  getCurrentRental: (equipmentId: string) =>
    api.get<RentalDto | null>(`/equipments/${encodeURIComponent(equipmentId)}/current-rental`),
  getDeparture: (rentalId: string) =>
    api.get<InspectionDto | null>(`/rentals/${encodeURIComponent(rentalId)}/departure-inspection`),
  getTemplate: (id: string) => api.get<InspectionTemplateDto>(`/inspection-templates/${encodeURIComponent(id)}`),
  getLatestTemplate: (categoryId: string) =>
    api.get<InspectionTemplateDto>(`/equipment-categories/${encodeURIComponent(categoryId)}/inspection-template`),
};
