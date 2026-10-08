import { api } from "../client";
import type { AgencyDto, ListAgenciesDto, SaveAgencyDto } from "./dto";

const path = "/agencies";

export const agenciesApi = {
  list(): Promise<ListAgenciesDto> {
    return api.get<ListAgenciesDto>(path);
  },
  create(input: SaveAgencyDto): Promise<AgencyDto> {
    return api.post<AgencyDto, SaveAgencyDto>(path, input);
  },
  update(id: string, input: SaveAgencyDto): Promise<AgencyDto> {
    return api.put<AgencyDto, SaveAgencyDto>(`${path}/${encodeURIComponent(id)}`, input);
  },
};
