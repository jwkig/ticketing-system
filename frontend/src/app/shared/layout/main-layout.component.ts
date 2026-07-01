import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from '../../core/auth/auth.service';

/** Authenticated shell: top nav + user menu, hosting the guarded feature routes. */
@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main-layout.component.html',
  styles: `
    .brand {
      font-weight: 600;
      margin-right: 1.5rem;
    }
    nav a {
      margin-right: 0.25rem;
    }
    .spacer {
      flex: 1 1 auto;
    }
    .user-email {
      margin-right: 1rem;
      opacity: 0.9;
    }
    .active {
      text-decoration: underline;
    }
    main {
      padding: 1.5rem;
    }
  `,
})
export class MainLayoutComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly userEmail = this.auth.currentUserEmail;

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
