import { api } from "../client";
import type {
  CreateRentalDto,
  ListRentalsDto,
  RentalDto,
  RentalFilters,
  UpdateRentalDto,
} from "./dto";

const path = (id: string) => `/rentals/${encodeURIComponent(id)}`;
export const rentalsApi = {
  list(filters: RentalFilters = {}) {
    return api.get<ListRentalsDto>("/rentals", { query: { ...filters } });
  },
  getById(id: string) {
    return api.get<RentalDto>(path(id));
  },
  getCurrentForEquipment(equipmentId: string) {
    return api.get<RentalDto | null>(
      `/equipments/${encodeURIComponent(equipmentId)}/current-rental`,
    );
  },
  create(dto: CreateRentalDto) {
    return api.post<RentalDto, CreateRentalDto>("/rentals", dto);
  },
  update(id: string, dto: UpdateRentalDto) {
    return api.put<RentalDto, UpdateRentalDto>(path(id), dto);
  },
  cancel(id: string) {
    return api.post<RentalDto, Record<string, never>>(`${path(id)}/cancel`, {});
  },
};
