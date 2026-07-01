import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { extractErrorMessage } from '../../core/error/error.interceptor';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Team } from '../../domain/team.model';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { ErrorBannerComponent } from '../../shared/components/error-banner/error-banner.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { TeamFormDialogComponent } from './team-form-dialog.component';

@Component({
  selector: 'app-teams',
  imports: [
    DatePipe,
    MatButtonModule,
    MatTableModule,
    MatIconModule,
    MatDialogModule,
    ErrorBannerComponent,
    LoadingSpinnerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './teams.component.html',
  styles: `
    .page-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .teams-table {
      width: 100%;
      margin-bottom: 1.5rem;
    }
    .empty {
      opacity: 0.7;
      margin: 1rem 0 1.5rem;
    }
  `,
})
export class TeamsComponent implements OnInit {
  private readonly service = inject(TeamsService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly teams = signal<Team[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal(''); // delete errors (409 when referenced)
  protected readonly columns = ['name', 'tickets', 'epics', 'modified', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  openCreate(): void {
    this.openForm(null);
  }

  startEdit(team: Team): void {
    this.openForm(team);
  }

  canDelete(team: Team): boolean {
    return team.ticketCount + team.epicCount === 0;
  }

  confirmDelete(team: Team): void {
    const data: ConfirmDialogData = {
      title: 'Delete team',
      message: `Delete "${team.name}"? This cannot be undone.`,
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed === true) {
          this.deleteTeam(team.id);
        }
      });
  }

  /** Opens the create/edit dialog; reloads the list if it reports a save. */
  private openForm(team: Team | null): void {
    this.errorMessage.set('');
    this.dialog
      .open(TeamFormDialogComponent, { data: { team }, width: '420px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved === true) {
          this.load();
        }
      });
  }

  private load(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (teams) => {
        this.teams.set(teams);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private deleteTeam(id: string): void {
    this.errorMessage.set('');
    this.service.delete(id).subscribe({
      next: () => {
        this.notify.success('Team deleted.');
        this.load();
      },
      error: (err: HttpErrorResponse) => this.errorMessage.set(extractErrorMessage(err)),
    });
  }
}
