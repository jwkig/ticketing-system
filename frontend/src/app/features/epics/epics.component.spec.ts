import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { EpicsService } from '../../core/epics/epics.service';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Epic } from '../../domain/epic.model';
import { Team } from '../../domain/team.model';
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

describe('EpicsComponent', () => {
  let fixture: ComponentFixture<EpicsComponent>;
  let component: EpicsComponent;
  const epicsSvc = { getByTeam: vi.fn(), create: vi.fn(), update: vi.fn(), delete: vi.fn() };
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
    fixture.detectChanges(); // ngOnInit → load teams → select first → load epics
  }

  beforeEach(() => {
    epicsSvc.getByTeam.mockReset().mockReturnValue(of([]));
    epicsSvc.create.mockReset();
    epicsSvc.update.mockReset();
    epicsSvc.delete.mockReset();
    teamsSvc.getAll.mockReset().mockReturnValue(of([makeTeam()]));
    dialog.open.mockReset();
    notify.success.mockReset();
  });

  it('loads teams, selects the first, and lists its epics', async () => {
    epicsSvc.getByTeam.mockReturnValue(of([makeEpic({ title: 'Reliability' })]));
    await setup();
    expect(teamsSvc.getAll).toHaveBeenCalled();
    expect(epicsSvc.getByTeam).toHaveBeenCalledWith('team-1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Reliability');
  });

  it('creates an epic under the selected team', async () => {
    await setup();
    epicsSvc.create.mockReturnValue(of(makeEpic()));
    component.form.controls.title.setValue('New epic');
    component.submit();
    expect(epicsSvc.create).toHaveBeenCalledWith('team-1', { title: 'New epic', description: null });
    expect(notify.success).toHaveBeenCalled();
  });

  it('shows an inline error when creation conflicts', async () => {
    await setup();
    epicsSvc.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { error: 'Team not found.' } })),
    );
    component.form.controls.title.setValue('Orphan');
    component.submit();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.error-banner')).not.toBeNull();
  });

  it('confirms then deletes an epic', async () => {
    epicsSvc.getByTeam.mockReturnValue(of([makeEpic()]));
    await setup();
    dialog.open.mockReturnValue({ afterClosed: () => of(true) });
    epicsSvc.delete.mockReturnValue(of(undefined));

    component.confirmDelete(makeEpic());

    expect(dialog.open).toHaveBeenCalled();
    expect(epicsSvc.delete).toHaveBeenCalledWith('epic-1');
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
