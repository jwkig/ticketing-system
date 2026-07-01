import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { EpicsService } from '../../core/epics/epics.service';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Epic } from '../../domain/epic.model';
import { Team } from '../../domain/team.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EpicFormDialogComponent } from './epic-form-dialog.component';
import { EpicsComponent } from './epics.component';

function makeTeam(over: Partial<Team> = {}): Team {
  return {
    id: 'team-1',
    name: 'Payments',
    createdAt: '2026-01-01T00:00:00Z',
    modifiedAt: '2026-01-01T00:00:00Z',
    ticketCount: 0,
    epicCount: 0,
    ...over,
  };
}

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

function dialogClosing(value: unknown) {
  return { afterClosed: () => of(value) };
}

describe('EpicsComponent', () => {
  let fixture: ComponentFixture<EpicsComponent>;
  let component: EpicsComponent;
  const epicsSvc = { getByTeam: vi.fn(), delete: vi.fn() };
  const teamsSvc = { getAll: vi.fn() };
  const dialog = { open: vi.fn() };
  const notify = { success: vi.fn(), error: vi.fn() };

  async function setup() {
    await TestBed.configureTestingModule({
      imports: [EpicsComponent],
      providers: [
        provideRouter([]),
        { provide: EpicsService, useValue: epicsSvc },
        { provide: TeamsService, useValue: teamsSvc },
        { provide: NotificationService, useValue: notify },
      ],
    })
      .overrideComponent(EpicsComponent, {
        add: { providers: [{ provide: MatDialog, useValue: dialog }] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(EpicsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    epicsSvc.getByTeam.mockReset().mockReturnValue(of([]));
    epicsSvc.delete.mockReset();
    teamsSvc.getAll.mockReset().mockReturnValue(of([makeTeam()]));
    dialog.open.mockReset().mockReturnValue(dialogClosing(false));
    notify.success.mockReset();
  });

  it('loads teams, selects the first, and lists its epics', async () => {
    epicsSvc.getByTeam.mockReturnValue(of([makeEpic({ title: 'Reliability' })]));
    await setup();
    expect(teamsSvc.getAll).toHaveBeenCalled();
    expect(epicsSvc.getByTeam).toHaveBeenCalledWith('team-1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Reliability');
  });

  it('opens the create dialog scoped to the selected team', async () => {
    await setup();
    component.openCreate();
    expect(dialog.open).toHaveBeenCalledWith(
      EpicFormDialogComponent,
      expect.objectContaining({ data: { teamId: 'team-1', epic: null } }),
    );
  });

  it('opens the edit dialog pre-loaded with the epic', async () => {
    await setup();
    const epic = makeEpic();
    component.startEdit(epic);
    expect(dialog.open).toHaveBeenCalledWith(
      EpicFormDialogComponent,
      expect.objectContaining({ data: { teamId: 'team-1', epic } }),
    );
  });

  it('reloads the epic list after the dialog reports a save', async () => {
    await setup(); // getByTeam called once (initial team select)
    dialog.open.mockReturnValue(dialogClosing(true));
    component.openCreate();
    expect(epicsSvc.getByTeam).toHaveBeenCalledTimes(2);
  });

  it('does not reload when the dialog is cancelled', async () => {
    await setup();
    dialog.open.mockReturnValue(dialogClosing(false));
    component.openCreate();
    expect(epicsSvc.getByTeam).toHaveBeenCalledTimes(1);
  });

  it('confirms then deletes an epic', async () => {
    epicsSvc.getByTeam.mockReturnValue(of([makeEpic()]));
    await setup();
    dialog.open.mockReturnValue(dialogClosing(true));
    epicsSvc.delete.mockReturnValue(of(undefined));

    component.confirmDelete(makeEpic());

    expect(dialog.open).toHaveBeenCalledWith(ConfirmDialogComponent, expect.anything());
    expect(epicsSvc.delete).toHaveBeenCalledWith('epic-1');
  });

  it('does not delete when the confirm dialog is cancelled', async () => {
    await setup();
    dialog.open.mockReturnValue(dialogClosing(false));
    component.confirmDelete(makeEpic());
    expect(epicsSvc.delete).not.toHaveBeenCalled();
  });

  it('disables delete when the epic has tickets', async () => {
    await setup();
    expect(component.canDelete(makeEpic({ ticketCount: 0 }))).toBe(true);
    expect(component.canDelete(makeEpic({ ticketCount: 3 }))).toBe(false);
  });

  it('prompts to create a team when none exist', async () => {
    teamsSvc.getAll.mockReturnValue(of([]));
    await setup();
    expect(epicsSvc.getByTeam).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Create a team first');
  });
});
