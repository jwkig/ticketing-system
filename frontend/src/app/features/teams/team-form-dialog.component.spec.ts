import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Team } from '../../domain/team.model';
import { TeamFormDialogComponent, TeamFormDialogData } from './team-form-dialog.component';

function makeTeam(over: Partial<Team> = {}): Team {
  return {
    id: 't1',
    name: 'Backend',
    createdAt: '2026-01-01T00:00:00Z',
    modifiedAt: '2026-01-01T00:00:00Z',
    ticketCount: 0,
    epicCount: 0,
    ...over,
  };
}

describe('TeamFormDialogComponent', () => {
  const service = { create: vi.fn(), update: vi.fn() };
  const notify = { success: vi.fn(), error: vi.fn() };
  const dialogRef = { close: vi.fn() };

  beforeEach(() => {
    service.create.mockReset();
    service.update.mockReset();
    notify.success.mockReset();
    dialogRef.close.mockReset();
  });

  async function setup(data: TeamFormDialogData) {
    await TestBed.configureTestingModule({
      imports: [TeamFormDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: TeamsService, useValue: service },
        { provide: NotificationService, useValue: notify },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(TeamFormDialogComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('creates a team and closes with true', async () => {
    service.create.mockReturnValue(of(makeTeam()));
    const fixture = await setup({ team: null });
    const component = fixture.componentInstance;
    component.form.controls.name.setValue('Backend');

    component.submit();

    expect(service.create).toHaveBeenCalledWith({ name: 'Backend' });
    expect(notify.success).toHaveBeenCalled();
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('pre-fills and updates in edit mode', async () => {
    service.update.mockReturnValue(of(makeTeam({ name: 'Renamed' })));
    const fixture = await setup({ team: makeTeam({ id: 't1', name: 'Backend' }) });
    const component = fixture.componentInstance;
    expect(component.form.controls.name.value).toBe('Backend');

    component.form.controls.name.setValue('Renamed');
    component.submit();

    expect(service.update).toHaveBeenCalledWith('t1', { name: 'Renamed' });
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('does not call the service for an invalid form', async () => {
    const fixture = await setup({ team: null });
    fixture.componentInstance.submit();
    expect(service.create).not.toHaveBeenCalled();
  });

  it('shows an inline error and stays open on conflict', async () => {
    service.create.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { error: 'A team with this name already exists.' },
          }),
      ),
    );
    const fixture = await setup({ team: null });
    const component = fixture.componentInstance;
    component.form.controls.name.setValue('Backend');

    component.submit();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.error-banner')).not.toBeNull();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('cancel closes with false', async () => {
    const fixture = await setup({ team: null });
    fixture.componentInstance.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(false);
  });
});
