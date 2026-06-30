import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JwtToken, LoginRequest, SignUpRequest } from '../../domain/auth.model';
import { TokenStorageService } from './token-storage.service';

/** Talks to the backend auth endpoints and owns the client-side session. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(TokenStorageService);
  private readonly base = `${environment.apiBaseUrl}/auth`;

  /** True while a JWT is held. */
  readonly isAuthenticated = computed(() => this.tokens.token() !== null);

  /** Email claim decoded from the current JWT, or null. */
  readonly currentUserEmail = computed(() => decodeEmailClaim(this.tokens.token()));

  /** POST /auth/signup → 201 (no body). */
  signUp(body: SignUpRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/signup`, body);
  }

  /** POST /auth/login → 200 { token }; stores the token on success. */
  login(body: LoginRequest): Observable<JwtToken> {
    return this.http
      .post<JwtToken>(`${this.base}/login`, body)
      .pipe(tap((res) => this.tokens.set(res.token)));
  }

  /** GET /auth/verify-email?token=… → 204. */
  verifyEmail(token: string): Observable<void> {
    const params = new HttpParams().set('token', token);
    return this.http.get<void>(`${this.base}/verify-email`, { params });
  }

  /** POST /auth/resend-verification → 204. */
  resendVerification(email: string): Observable<void> {
    return this.http.post<void>(`${this.base}/resend-verification`, { email });
  }

  /** Clears the session (bearer logout is purely client-side). */
  logout(): void {
    this.tokens.clear();
  }
}

/** Best-effort decode of the `email` claim from a JWT payload (display only). */
function decodeEmailClaim(token: string | null): string | null {
  if (!token) {
    return null;
  }
  const segments = token.split('.');
  if (segments.length < 2) {
    return null;
  }
  try {
    const base64 = segments[1].replace(/-/g, '+').replace(/_/g, '/');
    const payload = JSON.parse(atob(base64)) as { email?: string };
    return payload.email ?? null;
  } catch {
    return null;
  }
}
