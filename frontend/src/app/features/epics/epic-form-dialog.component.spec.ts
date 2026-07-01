import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { EpicsService } from '../../core/epics/epics.service';
import { NotificationService } from '../../core/notification/notification.service';
import { Epic } from '../../domain/epic.model';
import { EpicFormDialogComponent, EpicFormDialogData } from './epic-form-dialog.component';

function makeEpic(over: Partial<Epic> = {}): Epic {
  return {
    id: 'epic-1',
    teamId: 'team-1',
    title: 'Checkout',
    description: null,
    createdAt: '2026-01-01T00:00:00Z',
    modifiedAt: '2026-01-01T00:00:00Z',
    ticketCount: 0,
    ...over,
  };
}

describe('EpicFormDialogComponent', () => {
  const service = { create: vi.fn(), update: vi.fn() };
  const notify = { success: vi.fn(), error: vi.fn() };
  const dialogRef = { close: vi.fn() };

  beforeEach(() => {
    service.create.mockReset();
    service.update.mockReset();
    notify.success.mockReset();
    dialogRef.close.mockReset();
  });

  async function setup(data: EpicFormDialogData) {
    await TestBed.configureTestingModule({
      imports: [EpicFormDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: EpicsService, useValue: service },
        { provide: NotificationService, useValue: notify },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(EpicFormDialogComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('creates an epic under the team and closes with true', async () => {
    service.create.mockReturnValue(of(makeEpic()));
    const fixture = await setup({ teamId: 'team-1', epic: null });
    const component = fixture.componentInstance;
    component.form.controls.title.setValue('Checkout');

    component.submit();

    expect(service.create).toHaveBeenCalledWith('team-1', { title: 'Checkout', description: null });
    expect(notify.success).toHaveBeenCalled();
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('pre-fills and updates in edit mode', async () => {
    service.update.mockReturnValue(of(makeEpic({ title: 'New' })));
    const fixture = await setup({
      teamId: 'team-1',
      epic: makeEpic({ id: 'epic-1', title: 'Old', description: 'desc' }),
    });
    const component = fixture.componentInstance;
    expect(component.form.controls.title.value).toBe('Old');
    expect(component.form.controls.description.value).toBe('desc');

    component.form.controls.title.setValue('New');
    component.submit();

    expect(service.update).toHaveBeenCalledWith('epic-1', { title: 'New', description: 'desc' });
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('does not call the service for an invalid form', async () => {
    const fixture = await setup({ teamId: 'team-1', epic: null });
    fixture.componentInstance.submit();
    expect(service.create).not.toHaveBeenCalled();
  });

  it('shows an inline error and stays open on failure', async () => {
    service.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404, error: { error: 'Team not found.' } })),
    );
    const fixture = await setup({ teamId: 'team-1', epic: null });
    const component = fixture.componentInstance;
    component.form.controls.title.setValue('Orphan');

    component.submit();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.error-banner')).not.toBeNull();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('cancel closes with false', async () => {
    const fixture = await setup({ teamId: 'team-1', epic: null });
    fixture.componentInstance.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(false);
  });
});
