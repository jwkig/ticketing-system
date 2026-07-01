import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../core/notification/notification.service';
import { TicketsService } from '../../core/tickets/tickets.service';
import { Epic } from '../../domain/epic.model';
import { TicketDetail } from '../../domain/ticket.model';
import { TicketFormDialogComponent, TicketFormDialogData } from './ticket-form-dialog.component';

function makeDetail(over: Partial<TicketDetail> = {}): TicketDetail {
  return {
    id: 'ticket-1',
    teamId: 'team-1',
    type: 'bug',
    state: 'new',
    title: 'Login fails',
    body: 'repro steps',
    epicId: null,
    epicTitle: null,
    createdById: 'user-1',
    createdAt: '2026-01-01T00:00:00Z',
    modifiedAt: '2026-01-01T00:00:00Z',
    ...over,
  };
}

const epic: Epic = {
  id: 'epic-1',
  teamId: 'team-1',
  title: 'Checkout',
  description: null,
  createdAt: '2026-01-01T00:00:00Z',
  modifiedAt: '2026-01-01T00:00:00Z',
  ticketCount: 0,
};

describe('TicketFormDialogComponent', () => {
  const service = { create: vi.fn(), update: vi.fn() };
  const notify = { success: vi.fn(), error: vi.fn() };
  const dialogRef = { close: vi.fn() };

  beforeEach(() => {
    service.create.mockReset();
    service.update.mockReset();
    notify.success.mockReset();
    dialogRef.close.mockReset();
  });

  async function setup(data: TicketFormDialogData) {
    await TestBed.configureTestingModule({
      imports: [TicketFormDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: TicketsService, useValue: service },
        { provide: NotificationService, useValue: notify },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(TicketFormDialogComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('creates a ticket under the team and closes with true', async () => {
    service.create.mockReturnValue(of(makeDetail()));
    const fixture = await setup({ teamId: 'team-1', ticket: null, epics: [epic] });
    const component = fixture.componentInstance;
    component.form.controls.type.setValue('bug');
    component.form.controls.title.setValue('Login fails');
    component.form.controls.body.setValue('repro steps');

    component.submit();

    expect(service.create).toHaveBeenCalledWith('team-1', {
      type: 'bug',
      title: 'Login fails',
      body: 'repro steps',
      epicId: null,
    });
    expect(notify.success).toHaveBeenCalled();
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('pre-fills and updates in edit mode', async () => {
    service.update.mockReturnValue(of(makeDetail({ title: 'New' })));
    const fixture = await setup({
      teamId: 'team-1',
      ticket: makeDetail({ id: 'ticket-9', title: 'Old', body: 'old body', type: 'feature', epicId: 'epic-1' }),
      epics: [epic],
    });
    const component = fixture.componentInstance;
    expect(component.form.controls.title.value).toBe('Old');
    expect(component.form.controls.body.value).toBe('old body');
    expect(component.form.controls.type.value).toBe('feature');
    expect(component.form.controls.epicId.value).toBe('epic-1');

    component.form.controls.title.setValue('New');
    component.submit();

    expect(service.update).toHaveBeenCalledWith('ticket-9', {
      type: 'feature',
      title: 'New',
      body: 'old body',
      epicId: 'epic-1',
    });
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('does not call the service for an invalid form (blank title/body)', async () => {
    const fixture = await setup({ teamId: 'team-1', ticket: null, epics: [] });
    fixture.componentInstance.submit();
    expect(service.create).not.toHaveBeenCalled();
  });

  it('shows an inline error and stays open on failure', async () => {
    service.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { error: 'Bad request.' } })),
    );
    const fixture = await setup({ teamId: 'team-1', ticket: null, epics: [] });
    const component = fixture.componentInstance;
    component.form.controls.title.setValue('T');
    component.form.controls.body.setValue('B');

    component.submit();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.error-banner')).not.toBeNull();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('cancel closes with false', async () => {
    const fixture = await setup({ teamId: 'team-1', ticket: null, epics: [] });
    fixture.componentInstance.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(false);
  });
});
