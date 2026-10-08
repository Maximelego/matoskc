import { readonly, shallowRef } from "vue";
import { ApiError } from "../client";
import { authApi } from "./index";
import type { AuthAccountDto, LoginRequestDto } from "./dto";

const account = shallowRef<AuthAccountDto | null>(null);
let initialized = false;
let pending: Promise<AuthAccountDto | null> | null = null;

async function restore(): Promise<AuthAccountDto | null> {
  try {
    account.value = await authApi.me();
    initialized = true;
    return account.value;
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      account.value = null;
      initialized = true;
      return null;
    }
    throw error;
  }
}

export const authSession = {
  account: readonly(account),
  async ensure(): Promise<AuthAccountDto | null> {
    if (initialized) return account.value;
    if (!pending) pending = restore().finally(() => { pending = null; });
    return pending;
  },
  async login(credentials: LoginRequestDto): Promise<AuthAccountDto> {
    const result = await authApi.login(credentials);
    account.value = result.account;
    initialized = true;
    return result.account;
  },
  async logout(): Promise<void> {
    await authApi.logout();
    account.value = null;
    initialized = true;
  },
  invalidate(): void {
    account.value = null;
    initialized = true;
  },
};

window.addEventListener("matoskc:unauthorized", () => authSession.invalidate());
