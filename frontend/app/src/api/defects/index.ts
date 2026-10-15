import { api } from "../client";
import type { DefectPageDto, DefectStatus } from "./dto";
const path = (equipmentId: string) => `/equipments/${encodeURIComponent(equipmentId)}/defects`;
export const defectsApi = {
  list(equipmentId: string, status?: DefectStatus, offset = 0, limit = 50) {
    return api.get<DefectPageDto>(path(equipmentId), { query: { status, offset, limit } });
  },
  getPhoto(equipmentId: string, defectId: string, photoId: string) {
    return api.getBlob(`${path(equipmentId)}/${encodeURIComponent(defectId)}/photos/${encodeURIComponent(photoId)}`);
  },
};
