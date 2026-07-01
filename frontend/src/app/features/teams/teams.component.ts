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

@Component({
  selector: 'app-teams',
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
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
    .form-card {
      max-width: 480px;
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
export class TeamsComponent implements OnInit {
  private readonly service = inject(TeamsService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  protected readonly teams = signal<Team[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly columns = ['name', 'tickets', 'epics', 'modified', 'actions'];

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
  });

  ngOnInit(): void {
    this.load();
  }

  startCreate(): void {
    this.editingId.set(null);
    this.errorMessage.set('');
    this.form.reset({ name: '' });
  }

  startEdit(team: Team): void {
    this.editingId.set(team.id);
    this.errorMessage.set('');
    this.form.setValue({ name: team.name });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorMessage.set('');

    const name = this.form.controls.name.value.trim();
    const id = this.editingId();
    const request$ = id ? this.service.update(id, { name }) : this.service.create({ name });

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.notify.success(id ? 'Team updated.' : 'Team created.');
        this.startCreate();
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMessage.set(extractErrorMessage(err));
      },
    });
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
