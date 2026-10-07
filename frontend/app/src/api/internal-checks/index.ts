import { api } from "../client";
import type { CreateInternalCheckDto, InternalCheckDto, InternalCheckPhotoDto, ListInternalChecksDto, SaveInternalCheckDraftDto, ValidateInternalCheckDto } from "./dto";
const path = (id: string) => `/api/internal-checks/${encodeURIComponent(id)}`;
export const internalChecksApi = {
  list(equipmentId?: string) {
    return api.get<ListInternalChecksDto>("/api/internal-checks", { query: { equipmentId } });
  },
  getById(id: string) { return api.get<InternalCheckDto>(path(id)); },
  create(dto: CreateInternalCheckDto) { return api.post<InternalCheckDto, CreateInternalCheckDto>("/api/internal-checks", dto); },
  saveDraft(id: string, dto: SaveInternalCheckDraftDto) {
    return api.put<InternalCheckDto, SaveInternalCheckDraftDto>(`${path(id)}/draft`, dto);
  },
  validate(id: string, dto: ValidateInternalCheckDto) {
    return api.post<InternalCheckDto, ValidateInternalCheckDto>(`${path(id)}/validation`, dto);
  },
  uploadPhoto(id: string, file: File) { return api.putFile<InternalCheckPhotoDto>(`${path(id)}/photos`, file); },
};
