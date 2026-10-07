import type { AccountDto, CreateAccountDto, ListAccountsDto, UpdateAccountDto } from "./dto";

// API mock : aucun endpoint de gestion des comptes n'est encore exposé sur dev.
// Le mot de passe n'est jamais renvoyé ni conservé dans ce mock.
const accounts: AccountDto[] = [
  { id: "84000000-0000-4000-8000-000000000001", displayName: "Accès partagé - Agence 83",
    email: null, role: "Agency", agencyId: "83000000-0000-4000-8000-000000000001", isActive: true },
];
const copy = <T>(value: T): T => structuredClone(value);

export const accountsApi = {
  list(): Promise<ListAccountsDto> {
    return Promise.resolve(copy({ accounts }));
  },
  create(input: CreateAccountDto): Promise<AccountDto> {
    if (!input.password.trim()) return Promise.reject(new Error("Un mot de passe est requis."));
    if (!input.displayName.trim()) return Promise.reject(new Error("Le nom affiché est requis."));
    if (input.role === "SuperAdmin" ? input.agencyId !== null : input.agencyId === null)
      return Promise.reject(new Error("L’association à une agence ne correspond pas au rôle."));
    if (input.agencyId === "") return Promise.reject(new Error("Veuillez sélectionner une agence."));
    if (input.role === "Agency" ? input.email !== null : !input.email?.trim())
      return Promise.reject(new Error("L’adresse électronique ne correspond pas au rôle."));
    if (input.role === "Agency" && accounts.some((account) => account.role === "Agency" && account.agencyId === input.agencyId))
      return Promise.reject(new Error("Cette agence dispose déjà d’un accès partagé."));
    if (input.email && accounts.some((account) => account.email?.toLowerCase() === input.email?.toLowerCase()))
      return Promise.reject(new Error("Cette adresse électronique est déjà utilisée."));
    const account: AccountDto = {
      id: crypto.randomUUID(), displayName: input.displayName.trim(),
      email: input.email?.trim() ?? null, role: input.role,
      agencyId: input.agencyId, isActive: true,
    };
    accounts.push(account);
    return Promise.resolve(copy(account));
  },
  update(id: string, input: UpdateAccountDto): Promise<AccountDto> {
    const account = accounts.find((item) => item.id === id);
    if (!account) return Promise.reject(new Error("Ce compte est introuvable."));
    if (!input.displayName.trim()) return Promise.reject(new Error("Le nom affiché est requis."));
    if (account.role === "Agency" ? input.email !== null : !input.email?.trim())
      return Promise.reject(new Error("L’adresse électronique ne correspond pas au rôle."));
    if (input.email && accounts.some((item) => item.id !== id && item.email?.toLowerCase() === input.email?.toLowerCase()))
      return Promise.reject(new Error("Cette adresse électronique est déjà utilisée."));
    account.displayName = input.displayName.trim();
    account.email = input.email?.trim() ?? null;
    return Promise.resolve(copy(account));
  },
  setActive(id: string, isActive: boolean): Promise<AccountDto> {
    const account = accounts.find((item) => item.id === id);
    if (!account) return Promise.reject(new Error("Ce compte est introuvable."));
    account.isActive = isActive;
    return Promise.resolve(copy(account));
  },
};
