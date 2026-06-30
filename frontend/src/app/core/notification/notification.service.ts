import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Thin wrapper over MatSnackBar for transient success/error toasts. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  error(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 6000 });
  }

  success(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 4000 });
  }
}
