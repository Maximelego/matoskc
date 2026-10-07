export type RentalStatus = "Preparing" | "InProgress" | "Closed" | "Cancelled";

export type RentalDto = {
  id: string;
  equipmentId: string;
  agencyId: string;
  contractReference: string | null;
  customerName: string;
  customerEmail: string | null;
  status: RentalStatus;
  createdAt: string;
  closedAt: string | null;
};

export type CreateRentalDto = {
  equipmentId: string;
  contractReference?: string | null;
  customerName: string;
  customerEmail?: string | null;
};

export type UpdateRentalDto = Pick<CreateRentalDto, "contractReference" | "customerName" | "customerEmail">;
export type ListRentalsDto = { rentals: RentalDto[] };
export type RentalFilters = { equipmentId?: string; agencyId?: string; status?: RentalStatus; search?: string };
