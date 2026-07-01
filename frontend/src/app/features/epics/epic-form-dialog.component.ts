import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extractErrorMessage } from '../../core/error/error.interceptor';
import { EpicsService } from '../../core/epics/epics.service';
import { NotificationService } from '../../core/notification/notification.service';
import { Epic } from '../../domain/epic.model';
import { ErrorBannerComponent } from '../../shared/components/error-banner/error-banner.component';

export interface EpicFormDialogData {
  /** The team the epic belongs to (fixed at creation). */
  teamId: string;
  /** null → create; an epic → edit. */
  epic: Epic | null;
}

/** Create/edit-epic form hosted in a Material dialog. Closes with `true` on success. */
@Component({
  selector: 'app-epic-form-dialog',
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
    <h2 mat-dialog-title>{{ isEdit ? 'Edit epic' : 'Create epic' }}</h2>
    <mat-dialog-content>
      <app-error-banner [message]="errorMessage()" />
      <form id="epic-form" [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" />
          @if (form.controls.title.hasError('required') && form.controls.title.touched) {
            <mat-error>Title is required.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description (optional)</mat-label>
          <textarea matInput formControlName="description" rows="3"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">Cancel</button>
      <button mat-flat-button color="primary" type="submit" form="epic-form" [disabled]="saving()">
        {{ isEdit ? 'Save' : 'Create' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    mat-dialog-content {
      min-width: 360px;
    }
    .full-width {
      width: 100%;
    }
  `,
})
export class EpicFormDialogComponent {
  private readonly data = inject<EpicFormDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<EpicFormDialogComponent>);
  private readonly service = inject(EpicsService);
  private readonly notify = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly isEdit = this.data.epic !== null;

  readonly form = this.fb.nonNullable.group({
    title: [this.data.epic?.title ?? '', [Validators.required, Validators.maxLength(500)]],
    description: [this.data.epic?.description ?? ''],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorMessage.set('');

    const raw = this.form.getRawValue();
    const description = raw.description.trim();
    const body = { title: raw.title.trim(), description: description.length > 0 ? description : null };
    const epic = this.data.epic;
    const request$ = epic
      ? this.service.update(epic.id, body)
      : this.service.create(this.data.teamId, body);

    request$.subscribe({
      next: () => {
        this.notify.success(epic ? 'Epic updated.' : 'Epic created.');
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
