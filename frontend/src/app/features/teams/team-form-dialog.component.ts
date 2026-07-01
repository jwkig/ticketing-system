import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extractErrorMessage } from '../../core/error/error.interceptor';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Team } from '../../domain/team.model';
import { ErrorBannerComponent } from '../../shared/components/error-banner/error-banner.component';

export interface TeamFormDialogData {
  /** null → create; a team → edit. */
  team: Team | null;
}

/** Create/edit-team form hosted in a Material dialog. Closes with `true` on success. */
@Component({
  selector: 'app-team-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    ErrorBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit team' : 'Create team' }}</h2>
    <mat-dialog-content>
      <app-error-banner [message]="errorMessage()" />
      <form id="team-form" [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Team name</mat-label>
          <input matInput formControlName="name" placeholder="e.g. Platform Engineering" />
          @if (form.controls.name.hasError('required') && form.controls.name.touched) {
            <mat-error>Team name is required.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">Cancel</button>
      <button mat-flat-button color="primary" type="submit" form="team-form" [disabled]="saving()">
        {{ isEdit ? 'Save' : 'Create' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    mat-dialog-content {
      min-width: 320px;
    }
    .full-width {
      width: 100%;
    }
  `,
})
export class TeamFormDialogComponent {
  private readonly data = inject<TeamFormDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<TeamFormDialogComponent>);
  private readonly service = inject(TeamsService);
  private readonly notify = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly isEdit = this.data.team !== null;

  readonly form = this.fb.nonNullable.group({
    name: [this.data.team?.name ?? '', [Validators.required, Validators.maxLength(200)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorMessage.set('');

    const name = this.form.controls.name.value.trim();
    const team = this.data.team;
    const request$ = team ? this.service.update(team.id, { name }) : this.service.create({ name });

    request$.subscribe({
      next: () => {
        this.notify.success(team ? 'Team updated.' : 'Team created.');
        this.dialogRef.close(true);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMessage.set(extractErrorMessage(err));
      },
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
