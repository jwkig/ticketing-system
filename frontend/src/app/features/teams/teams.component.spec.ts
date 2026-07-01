import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Team } from '../../domain/team.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { TeamFormDialogComponent } from './team-form-dialog.component';
import { TeamsComponent } from './teams.component';

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

/** A MatDialogRef-like stub whose afterClosed emits the given value. */
function dialogClosing(value: unknown) {
  return { afterClosed: () => of(value) };
}

describe('TeamsComponent', () => {
  let fixture: ComponentFixture<TeamsComponent>;
  let component: TeamsComponent;
  const service = { getAll: vi.fn(), delete: vi.fn() };
  const dialog = { open: vi.fn() };
  const notify = { success: vi.fn(), error: vi.fn() };

  async function setup() {
    await TestBed.configureTestingModule({
      imports: [TeamsComponent],
      providers: [
        provideRouter([]),
        { provide: TeamsService, useValue: service },
        { provide: NotificationService, useValue: notify },
      ],
    })
      .overrideComponent(TeamsComponent, {
        add: { providers: [{ provide: MatDialog, useValue: dialog }] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(TeamsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    service.getAll.mockReset().mockReturnValue(of([]));
    service.delete.mockReset();
    dialog.open.mockReset().mockReturnValue(dialogClosing(false));
    notify.success.mockReset();
  });

  it('loads and renders teams on init', async () => {
    service.getAll.mockReturnValue(of([makeTeam({ name: 'Payments' })]));
    await setup();
    expect(service.getAll).toHaveBeenCalledTimes(1);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Payments');
  });

  it('opens the create dialog with no team', async () => {
    await setup();
    component.openCreate();
    expect(dialog.open).toHaveBeenCalledWith(
      TeamFormDialogComponent,
      expect.objectContaining({ data: { team: null } }),
    );
  });

  it('opens the edit dialog pre-loaded with the team', async () => {
    await setup();
    const team = makeTeam();
    component.startEdit(team);
    expect(dialog.open).toHaveBeenCalledWith(
      TeamFormDialogComponent,
      expect.objectContaining({ data: { team } }),
    );
  });

  it('reloads the list after the dialog reports a save', async () => {
    await setup(); // getAll called once on init
    dialog.open.mockReturnValue(dialogClosing(true));
    component.openCreate();
    expect(service.getAll).toHaveBeenCalledTimes(2);
  });

  it('does not reload when the dialog is cancelled', async () => {
    await setup();
    dialog.open.mockReturnValue(dialogClosing(false));
    component.openCreate();
    expect(service.getAll).toHaveBeenCalledTimes(1);
  });

  it('confirms then deletes a team', async () => {
    service.getAll.mockReturnValue(of([makeTeam()]));
    await setup();
    dialog.open.mockReturnValue(dialogClosing(true));
    service.delete.mockReturnValue(of(undefined));

    component.confirmDelete(makeTeam());

    expect(dialog.open).toHaveBeenCalledWith(ConfirmDialogComponent, expect.anything());
    expect(service.delete).toHaveBeenCalledWith('t1');
  });

  it('does not delete when the confirm dialog is cancelled', async () => {
    await setup();
    dialog.open.mockReturnValue(dialogClosing(false));

    component.confirmDelete(makeTeam());

    expect(service.delete).not.toHaveBeenCalled();
  });

  it('disables delete when the team has tickets or epics', async () => {
    await setup();
    expect(component.canDelete(makeTeam({ ticketCount: 0, epicCount: 0 }))).toBe(true);
    expect(component.canDelete(makeTeam({ ticketCount: 2 }))).toBe(false);
    expect(component.canDelete(makeTeam({ epicCount: 1 }))).toBe(false);
  });
});
