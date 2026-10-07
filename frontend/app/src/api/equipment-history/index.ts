import { api } from "../client";
import type { EquipmentHistoryDto } from "./dto";
export const equipmentHistoryApi = {
  list(equipmentId: string) {
    return api.get<EquipmentHistoryDto>(`/api/equipments/${encodeURIComponent(equipmentId)}/history`);
  },
};
