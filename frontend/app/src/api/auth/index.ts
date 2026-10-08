import { api } from "../client";
import type { AuthAccountDto, CreateSessionDto, LoginRequestDto, LoginResponseDto } from "./dto";
export type { AuthAccountDto, CreateSessionDto, LoginRequestDto, LoginResponseDto } from "./dto";

export const AUTH_MODES = [
  { value: "agency", label: "Agence" },
  { value: "admin", label: "Administration" },
] as const;
export const AGENCIES = [{ id: "83", label: "Agence 83" }] as const;

export const authApi = {
  async login(credentials: LoginRequestDto): Promise<LoginResponseDto> {
    const dto: CreateSessionDto =
      credentials.mode === "agency"
        ? { agencyCode: Number(credentials.agencyId), email: null, password: credentials.password }
        : { agencyCode: null, email: credentials.email, password: credentials.password };
    const result = await api.post<LoginResponseDto, CreateSessionDto>("/auth/login", dto);
    api.clearCsrf();
    return result;
  },
  me(): Promise<AuthAccountDto> {
    return api.get<AuthAccountDto>("/auth/me", { cache: "no-store" });
  },
  async logout(): Promise<void> {
    await api.post<void, Record<string, never>>("/auth/logout", {});
    api.clearCsrf();
  },
};
