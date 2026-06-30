import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ApiError } from '../../domain/auth.model';
import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../notification/notification.service';

/**
 * Cross-cutting error handling:
 *  - 401 → clear the session and redirect to /login.
 *  - network/server errors (status 0 or ≥500) → toast.
 * 4xx client errors (400/404/409) are re-thrown for components to present
 * inline (e.g. the login "not verified" message or the verify-email screen).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        auth.logout();
        void router.navigate(['/login']);
      } else if (err.status === 0 || err.status >= 500) {
        notify.error(extractErrorMessage(err));
      }
      return throwError(() => err);
    }),
  );
};

/** Extracts a human-readable message from an API error body. */
export function extractErrorMessage(err: HttpErrorResponse): string {
  const body = err.error as ApiError | string | null;
  if (typeof body === 'string' && body.length > 0) {
    return body;
  }
  if (body && typeof body === 'object') {
    if (body.error) {
      return body.error;
    }
    if (body.errors) {
      const firstField = Object.values(body.errors)[0];
      if (firstField && firstField.length > 0) {
        return firstField[0];
      }
    }
  }
  return err.status === 0 ? 'Cannot reach the server.' : 'Something went wrong.';
}
