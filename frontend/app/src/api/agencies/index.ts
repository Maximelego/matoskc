import type { AgencyDto, ListAgenciesDto, SaveAgencyDto } from "./dto";

// API mock : aucun endpoint de gestion des agences n'est encore exposé sur dev.
// Remplacer ces méthodes par les appels au client API lorsque les routes seront définies.
const agencies: AgencyDto[] = [
  { id: "83000000-0000-4000-8000-000000000001", code: 83, name: "Agence 83", isActive: true },
];

const copy = <T>(value: T): T => structuredClone(value);

export const agenciesApi = {
  list(): Promise<ListAgenciesDto> {
    return Promise.resolve(copy({ agencies }));
  },
  create(input: SaveAgencyDto): Promise<AgencyDto> {
    if (!input.name.trim() || !Number.isInteger(input.code) || input.code <= 0)
      return Promise.reject(new Error("Renseignez un nom et un code entier positif."));
    if (agencies.some((agency) => agency.code === input.code))
      return Promise.reject(new Error("Ce code d’agence est déjà utilisé."));
    const agency: AgencyDto = { ...input, name: input.name.trim(), id: crypto.randomUUID(), isActive: true };
    agencies.push(agency);
    return Promise.resolve(copy(agency));
  },
  update(id: string, input: SaveAgencyDto): Promise<AgencyDto> {
    const agency = agencies.find((item) => item.id === id);
    if (!agency) return Promise.reject(new Error("Cette agence est introuvable."));
    if (!input.name.trim() || !Number.isInteger(input.code) || input.code <= 0)
      return Promise.reject(new Error("Renseignez un nom et un code entier positif."));
    if (agencies.some((item) => item.id !== id && item.code === input.code))
      return Promise.reject(new Error("Ce code d’agence est déjà utilisé."));
    Object.assign(agency, input, { name: input.name.trim() });
    return Promise.resolve(copy(agency));
  },

};
