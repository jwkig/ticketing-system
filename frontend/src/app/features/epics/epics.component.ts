import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { extractErrorMessage } from '../../core/error/error.interceptor';
import { EpicsService } from '../../core/epics/epics.service';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Epic } from '../../domain/epic.model';
import { Team } from '../../domain/team.model';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { ErrorBannerComponent } from '../../shared/components/error-banner/error-banner.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { EpicFormDialogComponent } from './epic-form-dialog.component';

@Component({
  selector: 'app-epics',
  imports: [
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTableModule,
    MatIconModule,
    MatDialogModule,
    ErrorBannerComponent,
    LoadingSpinnerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './epics.component.html',
  styles: `
    .page-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .team-picker {
      min-width: 260px;
      margin: 0.5rem 0 1rem;
    }
    .epics-table {
      width: 100%;
      margin-bottom: 1.5rem;
    }
    .empty {
      opacity: 0.7;
      margin: 1rem 0 1.5rem;
    }
  `,
})
export class EpicsComponent implements OnInit {
  private readonly epicsSvc = inject(EpicsService);
  private readonly teamsSvc = inject(TeamsService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly teams = signal<Team[]>([]);
  protected readonly epics = signal<Epic[]>([]);
  protected readonly selectedTeamId = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal(''); // delete errors (409 when referenced)
  protected readonly columns = ['title', 'tickets', 'modified', 'actions'];

  ngOnInit(): void {
    this.teamsSvc.getAll().subscribe({
      next: (teams) => {
        this.teams.set(teams);
        if (teams.length > 0) {
          this.selectTeam(teams[0].id);
        }
      },
    });
  }

  selectTeam(teamId: string): void {
    this.selectedTeamId.set(teamId);
    this.loadEpics();
  }

  openCreate(): void {
    this.openForm(null);
  }

  startEdit(epic: Epic): void {
    this.openForm(epic);
  }

  canDelete(epic: Epic): boolean {
    return epic.ticketCount === 0;
  }

  confirmDelete(epic: Epic): void {
    const data: ConfirmDialogData = {
      title: 'Delete epic',
      message: `Delete "${epic.title}"? This cannot be undone.`,
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed === true) {
          this.deleteEpic(epic.id);
        }
      });
  }

  /** Opens the create/edit dialog scoped to the selected team; reloads on save. */
  private openForm(epic: Epic | null): void {
    const teamId = this.selectedTeamId();
    if (!teamId) {
      return;
    }
    this.errorMessage.set('');
    this.dialog
      .open(EpicFormDialogComponent, { data: { teamId, epic }, width: '480px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved === true) {
          this.loadEpics();
        }
      });
  }

  private loadEpics(): void {
    const teamId = this.selectedTeamId();
    if (!teamId) {
      this.epics.set([]);
      return;
    }
    this.loading.set(true);
    this.epicsSvc.getByTeam(teamId).subscribe({
      next: (epics) => {
        this.epics.set(epics);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private deleteEpic(id: string): void {
    this.errorMessage.set('');
    this.epicsSvc.delete(id).subscribe({
      next: () => {
        this.notify.success('Epic deleted.');
        this.loadEpics();
      },
      error: (err: HttpErrorResponse) => this.errorMessage.set(extractErrorMessage(err)),
    });
  }
}
