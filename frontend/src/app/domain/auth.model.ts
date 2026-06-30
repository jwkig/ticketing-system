/** Request/response shapes mirroring the backend auth API contract. */

export interface SignUpRequest {
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

/** POST /api/auth/login response body. */
export interface JwtToken {
  token: string;
}

/** Error body shapes returned by the API. */
export interface ApiError {
  /** Single domain/validation message, e.g. { "error": "..." }. */
  error?: string;
  /** Field-keyed validation errors, e.g. { "errors": { "Email": ["..."] } }. */
  errors?: Record<string, string[]>;
}
