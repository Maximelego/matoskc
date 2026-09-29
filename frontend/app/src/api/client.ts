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

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
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

  delete<T = void>(path: string, options?: Options): Promise<T> {
    return this.request<T>("DELETE", path, undefined, options);
  }

  private async request<T>(
    method: string,
    path: string,
    body?: unknown,
    options: Options = {},
  ): Promise<T> {
    const { query, headers: customHeaders, ...init } = options;
    const url = new URL(`${this.baseUrl}/${path.replace(/^\/+/, "")}`, window.location.origin);

    for (const [key, value] of Object.entries(query ?? {})) {
      if (value !== null && value !== undefined) url.searchParams.set(key, String(value));
    }

    const headers = new Headers(customHeaders);
    headers.set("Accept", "application/json");
    if (body !== undefined) headers.set("Content-Type", "application/json");

    const response = await fetch(url, {
      ...init,
      method,
      headers,
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    });

    if (!response.ok) {
      const errorBody: unknown = await response.json().catch(() => undefined);
      const problem = isProblemDetails(errorBody) ? errorBody : undefined;
      throw new ApiError(response.status, problem);
    }

    if (response.status === 204 || response.status === 205) return undefined as T;

    return (await response.json()) as T;
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

// One shared instance. A same-origin /api path works with the Vite development proxy.
export const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? "");
