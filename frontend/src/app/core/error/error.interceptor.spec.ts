import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../notification/notification.service';
import { errorInterceptor, extractErrorMessage } from './error.interceptor';

describe('extractErrorMessage', () => {
  const err = (status: number, error: unknown) => new HttpErrorResponse({ status, error });

  it('reads a single {error} message', () => {
    expect(extractErrorMessage(err(400, { error: 'Bad thing' }))).toBe('Bad thing');
  });

  it('reads the first validation message from {errors}', () => {
    expect(extractErrorMessage(err(400, { errors: { Email: ['Required'] } }))).toBe('Required');
  });

  it('reads a plain string body', () => {
    expect(extractErrorMessage(err(400, 'plain'))).toBe('plain');
  });

  it('maps a network error (status 0)', () => {
    expect(extractErrorMessage(err(0, null))).toBe('Cannot reach the server.');
  });

  it('falls back for unknown shapes', () => {
    expect(extractErrorMessage(err(500, null))).toBe('Something went wrong.');
  });
});

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  const auth = { logout: jest.fn() };
  const notify = { error: jest.fn(), success: jest.fn() };

  beforeEach(() => {
    auth.logout.mockReset();
    notify.error.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notify },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('logs out and redirects on 401', () => {
    const nav = jest.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    http.get('/api/secure').subscribe({ error: () => undefined });
    httpMock.expectOne('/api/secure').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(auth.logout).toHaveBeenCalled();
    expect(nav).toHaveBeenCalledWith(['/login']);
  });

  it('toasts on a server error (500)', () => {
    http.get('/api/x').subscribe({ error: () => undefined });
    httpMock.expectOne('/api/x').flush(null, { status: 500, statusText: 'Server Error' });
    expect(notify.error).toHaveBeenCalled();
  });

  it('does not toast a 400 (left for the component)', () => {
    http.get('/api/x').subscribe({ error: () => undefined });
    httpMock.expectOne('/api/x').flush({ error: 'nope' }, { status: 400, statusText: 'Bad Request' });
    expect(notify.error).not.toHaveBeenCalled();
  });
});
