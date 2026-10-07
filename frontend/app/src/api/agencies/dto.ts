export type AgencyDto = {
  id: string;
  code: number;
  name: string;
  isActive: boolean;
};

export type SaveAgencyDto = Pick<AgencyDto, "code" | "name">;
export type ListAgenciesDto = { agencies: AgencyDto[] };
