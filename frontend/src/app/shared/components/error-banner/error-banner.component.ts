import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/** Inline error message box; renders nothing when `message` is empty. */
@Component({
  selector: 'app-error-banner',
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (message()) {
      <div class="error-banner" role="alert">
        <mat-icon aria-hidden="true">error_outline</mat-icon>
        <span>{{ message() }}</span>
      </div>
    }
  `,
  styles: `
    .error-banner {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      margin-bottom: 1rem;
      border-radius: 8px;
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }
  `,
})
export class ErrorBannerComponent {
  readonly message = input('');
}
