import { api } from "../client";
import type { AccountDto, CreateAccountDto, ListAccountsDto, UpdateAccountDto } from "./dto";

const path = "/accounts";

export const accountsApi = {
  list(): Promise<ListAccountsDto> {
    return api.get<ListAccountsDto>(path);
  },
  create(input: CreateAccountDto): Promise<AccountDto> {
    return api.post<AccountDto, CreateAccountDto>(path, input);
  },
  update(id: string, input: UpdateAccountDto): Promise<AccountDto> {
    return api.put<AccountDto, UpdateAccountDto>(`${path}/${encodeURIComponent(id)}`, input);
  },
  setActive(account: AccountDto, isActive: boolean): Promise<AccountDto> {
    return this.update(account.id, {
      displayName: account.displayName,
      email: account.email,
      isActive,
    });
  },
};
