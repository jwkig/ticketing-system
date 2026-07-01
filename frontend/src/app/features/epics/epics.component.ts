import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
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

@Component({
  selector: 'app-epics',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
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
    .form-card {
      max-width: 520px;
    }
    .full-width {
      width: 100%;
    }
    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
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
  private readonly fb = inject(FormBuilder);

  protected readonly teams = signal<Team[]>([]);
  protected readonly epics = signal<Epic[]>([]);
  protected readonly selectedTeamId = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly columns = ['title', 'tickets', 'modified', 'actions'];

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(500)]],
    description: [''],
  });

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
    this.startCreate();
    this.loadEpics();
  }

  startCreate(): void {
    this.editingId.set(null);
    this.errorMessage.set('');
    this.form.reset({ title: '', description: '' });
  }

  startEdit(epic: Epic): void {
    this.editingId.set(epic.id);
    this.errorMessage.set('');
    this.form.setValue({ title: epic.title, description: epic.description ?? '' });
  }

  submit(): void {
    const teamId = this.selectedTeamId();
    if (this.form.invalid || !teamId) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorMessage.set('');

    const raw = this.form.getRawValue();
    const description = raw.description.trim();
    const body = { title: raw.title.trim(), description: description.length > 0 ? description : null };
    const id = this.editingId();
    const request$ = id ? this.epicsSvc.update(id, body) : this.epicsSvc.create(teamId, body);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success(id ? 'Epic updated.' : 'Epic created.');
        this.startCreate();
        this.loadEpics();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMessage.set(extractErrorMessage(err));
      },
    });
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
