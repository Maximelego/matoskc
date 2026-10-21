import { api } from "../client";
import type { DefectPageDto, DefectStatus } from "./dto";
const path = (equipmentId: string) => `/equipments/${encodeURIComponent(equipmentId)}/defects`;
export const defectsApi = {
  list(equipmentId: string, status?: DefectStatus, offset = 0, limit = 50) {
    return api.get<DefectPageDto>(path(equipmentId), { query: { status, offset, limit } });
  },
  async listAll(equipmentId: string, status?: DefectStatus) {
    const items: DefectPageDto["items"] = [];
    for (let offset = 0; ; offset += 100) {
      const page = await this.list(equipmentId, status, offset, 100);
      items.push(...page.items);
      if (offset + page.items.length >= page.total || !page.items.length) return items;
    }
  },
  getPhoto(equipmentId: string, defectId: string, photoId: string) {
    return api.getBlob(`${path(equipmentId)}/${encodeURIComponent(defectId)}/photos/${encodeURIComponent(photoId)}`);
  },
};
