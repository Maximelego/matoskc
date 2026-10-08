export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  [key: string]: unknown;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | undefined;

  constructor(status: number, problem: ProblemDetails | undefined) {
    super(problem?.detail ?? problem?.title ?? `HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

type QueryValue = string | number | boolean | null | undefined;
type Query = Record<string, QueryValue>;
type Options = Omit<RequestInit, "method" | "body"> & { query?: Query };

class ApiClient {
  private readonly baseUrl: string;
  private csrfToken: string | null = null;
  private csrfPromise: Promise<string> | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  url(path: string): string {
    const base = this.baseUrl.replace(/\/+$/, "");
    const relativePath = path.replace(/^\/+/, "");

    return new URL(`${base}/${relativePath}`, window.location.origin).toString();
  }

  postFile<T>(path: string, file: File, options?: Options): Promise<T> {
    const form = new FormData();
    form.append("file", file);

    return this.request<T>("POST", path, form, options);
  }

  get<T>(path: string, options?: Options): Promise<T> {
    return this.request<T>("GET", path, undefined, options);
  }

  post<T, B = unknown>(path: string, body: B, options?: Options): Promise<T> {
    return this.request<T>("POST", path, body, options);
  }

  put<T, B = unknown>(path: string, body: B, options?: Options): Promise<T> {
    return this.request<T>("PUT", path, body, options);
  }

  patch<T, B = unknown>(path: string, body: B, options?: Options): Promise<T> {
    return this.request<T>("PATCH", path, body, options);
  }

  getBlob(path: string, options?: Options): Promise<Blob> {
    return this.request<Blob>("GET", path, undefined, options, "blob");
  }

  putFile<T = void>(path: string, file: File, options?: Options): Promise<T> {
    const form = new FormData();
    form.append("file", file);
    return this.request<T>("PUT", path, form, options);
  }

  delete<T = void>(path: string, options?: Options): Promise<T> {
    return this.request<T>("DELETE", path, undefined, options);
  }

  clearCsrf(): void {
    this.csrfToken = null;
  }

  private async getCsrfToken(): Promise<string> {
    if (this.csrfToken) return this.csrfToken;
    if (!this.csrfPromise) {
      this.csrfPromise = this.request<{ token: string }>("GET", "/auth/csrf", undefined, {
        cache: "no-store",
      })
        .then(({ token }) => {
          if (!token) throw new Error("Le jeton de sécurité est absent.");
          this.csrfToken = token;
          return token;
        })
        .finally(() => {
          this.csrfPromise = null;
        });
    }
    return this.csrfPromise;
  }

  private async request<T>(
    method: string,
    path: string,
    body?: unknown,
    options: Options = {},
    responseType: "json" | "blob" = "json",
    retriedCsrf = false,
  ): Promise<T> {
    const { query, headers: customHeaders, ...init } = options;
    const url = new URL(this.url(path));

    for (const [key, value] of Object.entries(query ?? {})) {
      if (value !== null && value !== undefined) url.searchParams.set(key, String(value));
    }

    const headers = new Headers(customHeaders);
    headers.set("Accept", "application/json");
    if (!["GET", "HEAD", "OPTIONS"].includes(method)) {
      headers.set("X-CSRF-TOKEN", await this.getCsrfToken());
    }
    if (body !== undefined && !(body instanceof FormData))
      headers.set("Content-Type", "application/json");

    const response = await fetch(url, {
      ...init,
      method,
      credentials: "same-origin",
      headers,
      ...(body === undefined
        ? {}
        : { body: body instanceof FormData ? body : JSON.stringify(body) }),
    });

    if (!response.ok) {
      const errorBody: unknown = await response.json().catch(() => undefined);
      const problem = isProblemDetails(errorBody) ? errorBody : undefined;
      if (response.status === 400 && problem?.title === "Invalid CSRF token" && !retriedCsrf) {
        this.clearCsrf();
        return this.request<T>(method, path, body, options, responseType, true);
      }
      if (response.status === 401 && path !== "/auth/login" && path !== "/auth/me") {
        window.dispatchEvent(new Event("matoskc:unauthorized"));
      }
      throw new ApiError(response.status, problem);
    }

    if (response.status === 204 || response.status === 205) return undefined as T;

    return (responseType === "blob" ? await response.blob() : await response.json()) as T;
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

// NGINX forwards /backend/ to the backend without changing resource paths.
const env = import.meta.env;
export const api = new ApiClient(env.VITE_API_BASE_URL ?? "/backend");
