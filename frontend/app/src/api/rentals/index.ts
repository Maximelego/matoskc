import { api } from "../client";
import type { CreateRentalDto, ListRentalsDto, RentalDto, RentalFilters, UpdateRentalDto } from "./dto";

const path = (id: string) => `/api/rentals/${encodeURIComponent(id)}`;
export const rentalsApi = {
  list(filters: RentalFilters = {}) {
    return api.get<ListRentalsDto>("/api/rentals", { query: { ...filters } });
  },
  getById(id: string) { return api.get<RentalDto>(path(id)); },
  getCurrentForEquipment(equipmentId: string) {
    return api.get<RentalDto | null>(`/api/equipments/${encodeURIComponent(equipmentId)}/current-rental`);
  },
  create(dto: CreateRentalDto) { return api.post<RentalDto, CreateRentalDto>("/api/rentals", dto); },
  update(id: string, dto: UpdateRentalDto) { return api.put<RentalDto, UpdateRentalDto>(path(id), dto); },
  cancel(id: string) { return api.post<RentalDto, Record<string, never>>(`${path(id)}/cancel`, {}); },
};
