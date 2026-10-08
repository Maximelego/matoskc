import { api } from "../client";
import type { DefectDto, DefectFilters, ListDefectsDto, ResolveDefectDto } from "./dto";
export const defectsApi = {
  list(filters: DefectFilters = {}) {
    return api.get<ListDefectsDto>("/defects", { query: { ...filters } });
  },
  getById(id: string) {
    return api.get<DefectDto>(`/defects/${encodeURIComponent(id)}`);
  },
  resolve(id: string, dto: ResolveDefectDto) {
    return api.post<DefectDto, ResolveDefectDto>(
      `/defects/${encodeURIComponent(id)}/resolution`,
      dto,
    );
  },
};
