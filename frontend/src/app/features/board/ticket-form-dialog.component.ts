import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { extractErrorMessage } from '../../core/error/error.interceptor';
import { NotificationService } from '../../core/notification/notification.service';
import { TicketsService } from '../../core/tickets/tickets.service';
import { Epic } from '../../domain/epic.model';
import { SaveTicketRequest, TicketDetail, TicketType, TICKET_TYPES } from '../../domain/ticket.model';
import { ErrorBannerComponent } from '../../shared/components/error-banner/error-banner.component';

export interface TicketFormDialogData {
  /** The team the ticket belongs to (fixed at creation). */
  teamId: string;
  /** null → create; a ticket → edit. */
  ticket: TicketDetail | null;
  /** The selected team's epics, offered as an optional link. */
  epics: Epic[];
}

/** Create/edit-ticket form hosted in a Material dialog. Closes with `true` on success. */
@Component({
  selector: 'app-ticket-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    ErrorBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit ticket' : 'New ticket' }}</h2>
    <mat-dialog-content>
      <app-error-banner [message]="errorMessage()" />
      <form id="ticket-form" [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Type</mat-label>
          <mat-select formControlName="type">
            @for (t of ticketTypes; track t.value) {
              <mat-option [value]="t.value">{{ t.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" />
          @if (form.controls.title.hasError('required') && form.controls.title.touched) {
            <mat-error>Title is required.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="body" rows="4"></textarea>
          @if (form.controls.body.hasError('required') && form.controls.body.touched) {
            <mat-error>Description is required.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Epic (optional)</mat-label>
          <mat-select formControlName="epicId">
            <mat-option [value]="null">None</mat-option>
            @for (epic of data.epics; track epic.id) {
              <mat-option [value]="epic.id">{{ epic.title }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">Cancel</button>
      <button mat-flat-button color="primary" type="submit" form="ticket-form" [disabled]="saving()">
        {{ isEdit ? 'Save' : 'Create' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    mat-dialog-content {
      min-width: 380px;
    }
    .full-width {
      width: 100%;
    }
  `,
})
export class TicketFormDialogComponent {
  protected readonly data = inject<TicketFormDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<TicketFormDialogComponent>);
  private readonly service = inject(TicketsService);
  private readonly notify = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly isEdit = this.data.ticket !== null;
  protected readonly ticketTypes = TICKET_TYPES;

  readonly form = this.fb.nonNullable.group({
    type: [this.data.ticket?.type ?? ('feature' as TicketType), Validators.required],
    title: [this.data.ticket?.title ?? '', [Validators.required, Validators.maxLength(500)]],
    body: [this.data.ticket?.body ?? '', Validators.required],
    epicId: [this.data.ticket?.epicId ?? (null as string | null)],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorMessage.set('');

    const raw = this.form.getRawValue();
    const body: SaveTicketRequest = {
      type: raw.type,
      title: raw.title.trim(),
      body: raw.body.trim(),
      epicId: raw.epicId ?? null,
    };
    const ticket = this.data.ticket;
    const request$ = ticket
      ? this.service.update(ticket.id, body)
      : this.service.create(this.data.teamId, body);

    request$.subscribe({
      next: () => {
        this.notify.success(ticket ? 'Ticket updated.' : 'Ticket created.');
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
