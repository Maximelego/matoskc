export type AccountRole = "SuperAdmin" | "Admin" | "Agency";

// Champs publics de MatosKC.Domain.Entities.Accounts.Account, sans HashedPassword.
export type AccountDto = {
  id: string;
  displayName: string;
  email: string | null;
  role: AccountRole;
  isActive: boolean;
  agencyId: string | null;
};

export type CreateAccountDto = Pick<AccountDto, "displayName" | "email" | "role" | "agencyId"> & {
  password: string;
};
export type UpdateAccountDto = Pick<AccountDto, "displayName" | "email">;
export type ListAccountsDto = { accounts: AccountDto[] };
