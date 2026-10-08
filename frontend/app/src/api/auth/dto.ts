import type { AccountDto } from "../accounts/dto";

export type LoginRequestDto =
  | { mode: "agency"; agencyId: string; password: string }
  | { mode: "admin"; email: string; password: string };

export type CreateSessionDto = { agencyCode: number | null; email: string | null; password: string };
export type AuthAccountDto = AccountDto;
export type LoginResponseDto = { account: AuthAccountDto; sessionExpiresAt: string };
