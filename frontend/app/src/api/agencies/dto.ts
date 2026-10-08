export type AgencyDto = {
  id: string;
  code: number;
  name: string;
};

export type SaveAgencyDto = Pick<AgencyDto, "code" | "name">;
export type ListAgenciesDto = { agencies: AgencyDto[] };
