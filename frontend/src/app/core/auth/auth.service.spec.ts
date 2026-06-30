import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { TokenStorageService } from './token-storage.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let tokens: TokenStorageService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    tokens = TestBed.inject(TokenStorageService);
  });

  afterEach(() => httpMock.verify());

  it('signUp POSTs the credentials to /api/auth/signup', () => {
    service.signUp({ email: 'a@b.com', password: 'password1' }).subscribe();
    const req = httpMock.expectOne('/api/auth/signup');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com', password: 'password1' });
    req.flush(null);
  });

  it('login stores the returned token and flips isAuthenticated', () => {
    let completed = false;
    service.login({ email: 'a@b.com', password: 'x' }).subscribe(() => (completed = true));
    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'jwt-123' });

    expect(completed).toBe(true);
    expect(tokens.get()).toBe('jwt-123');
    expect(service.isAuthenticated()).toBe(true);
  });

  it('verifyEmail GETs verify-email with the token query param', () => {
    service.verifyEmail('tok').subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/auth/verify-email');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('token')).toBe('tok');
    req.flush(null);
  });

  it('resendVerification POSTs the email', () => {
    service.resendVerification('a@b.com').subscribe();
    const req = httpMock.expectOne('/api/auth/resend-verification');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com' });
    req.flush(null);
  });

  it('logout clears the session', () => {
    tokens.set('x');
    expect(service.isAuthenticated()).toBe(true);
    service.logout();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('exposes the email claim from the JWT', () => {
    const payload = btoa(JSON.stringify({ email: 'me@example.com' }));
    tokens.set(`header.${payload}.sig`);
    expect(service.currentUserEmail()).toBe('me@example.com');
  });

  it('returns null email for a malformed token', () => {
    tokens.set('not-a-jwt');
    expect(service.currentUserEmail()).toBeNull();
  });
});
