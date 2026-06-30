import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from '../../core/auth/auth.service';

/**
 * Placeholder for the future Kanban board. For now it just proves the auth
 * guard and post-login landing work, and provides the Log out action.
 */
@Component({
  selector: 'app-board',
  imports: [MatToolbarModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './board.component.html',
  styles: `
    .spacer {
      flex: 1 1 auto;
    }
    .user-email {
      margin-right: 1rem;
      opacity: 0.9;
    }
    .board-placeholder {
      padding: 2rem;
      text-align: center;
    }
  `,
})
export class BoardComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly userEmail = this.auth.currentUserEmail;

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
