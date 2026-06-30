import { Injectable, signal } from '@angular/core';

const TOKEN_KEY = 'ticketing.jwt';

/**
 * Persists the JWT in sessionStorage and exposes it as a signal so dependent
 * state (authenticated flag, current user) reacts to login/logout.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly _token = signal<string | null>(sessionStorage.getItem(TOKEN_KEY));

  /** Reactive view of the current token (null when logged out). */
  readonly token = this._token.asReadonly();

  set(token: string): void {
    sessionStorage.setItem(TOKEN_KEY, token);
    this._token.set(token);
  }

  clear(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
  }

  get(): string | null {
    return this._token();
  }
}
