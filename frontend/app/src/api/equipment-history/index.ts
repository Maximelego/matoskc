import { api } from "../client";
import type { EquipmentAuditEntryDto } from "./dto";
export const equipmentHistoryApi = {
  list(equipmentId: string, offset = 0, limit = 50) {
    return api.get<EquipmentAuditEntryDto[]>(
      `/equipments/${encodeURIComponent(equipmentId)}/history`,
      { query: { offset, limit } },
    );
  },
};
