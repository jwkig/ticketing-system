import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { EpicsService } from '../../core/epics/epics.service';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { TicketsService } from '../../core/tickets/tickets.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { Team } from '../../domain/team.model';
import { TicketDetail, TicketSummary } from '../../domain/ticket.model';
import { BoardComponent } from './board.component';
import { TicketFormDialogComponent } from './ticket-form-dialog.component';

const team: Team = {
  id: 'team-1',
  name: 'Platform',
  createdAt: '2026-01-01T00:00:00Z',
  modifiedAt: '2026-01-01T00:00:00Z',
  ticketCount: 2,
  epicCount: 1,
};

function ticket(overrides: Partial<TicketSummary>): TicketSummary {
  return {
    id: 'ticket-1',
    teamId: 'team-1',
    type: 'bug',
    state: 'new',
    title: 'Untitled',
    epicId: null,
    epicTitle: null,
    createdAt: '2026-01-01T00:00:00Z',
    modifiedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

function dialogClosing(value: unknown) {
  return { afterClosed: () => of(value) };
}

describe('BoardComponent', () => {
  let fixture: ComponentFixture<BoardComponent>;
  let component: BoardComponent;
  const teamsSvc = { getAll: vi.fn() };
  const ticketsSvc = {
    getByTeam: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    changeState: vi.fn(),
    delete: vi.fn(),
  };
  const epicsSvc = { getByTeam: vi.fn() };
  const dialog = { open: vi.fn() };
  const notify = { success: vi.fn(), error: vi.fn() };

  async function setup() {
    await TestBed.configureTestingModule({
      imports: [BoardComponent],
      providers: [
        { provide: TeamsService, useValue: teamsSvc },
        { provide: TicketsService, useValue: ticketsSvc },
        { provide: EpicsService, useValue: epicsSvc },
        { provide: NotificationService, useValue: notify },
      ],
    })
      .overrideComponent(BoardComponent, {
        add: { providers: [{ provide: MatDialog, useValue: dialog }] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(BoardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  beforeEach(() => {
    teamsSvc.getAll.mockReset().mockReturnValue(of([team]));
    ticketsSvc.getByTeam.mockReset().mockReturnValue(
      of([
        ticket({ id: 't1', title: 'Login fails', type: 'bug', state: 'new', epicId: 'e1', epicTitle: 'Auth' }),
        ticket({ id: 't2', title: 'Dark mode', type: 'feature', state: 'in_progress' }),
      ]),
    );
    ticketsSvc.getById.mockReset();
    ticketsSvc.create.mockReset();
    ticketsSvc.update.mockReset();
    ticketsSvc.changeState.mockReset().mockReturnValue(of({}));
    ticketsSvc.delete.mockReset().mockReturnValue(of(undefined));
    epicsSvc.getByTeam.mockReset().mockReturnValue(
      of([{ id: 'e1', teamId: 'team-1', title: 'Auth', description: null, createdAt: '', modifiedAt: '', ticketCount: 1 }]),
    );
    dialog.open.mockReset().mockReturnValue(dialogClosing(false));
    notify.success.mockReset();
  });

  it('loads the first team and renders its tickets in the right columns', async () => {
    await setup();
    expect(teamsSvc.getAll).toHaveBeenCalled();
    expect(ticketsSvc.getByTeam).toHaveBeenCalledWith('team-1');
    const content = text();
    expect(content).toContain('Login fails');
    expect(content).toContain('Dark mode');
    expect(content).toContain('Epic: Auth');
  });

  it('narrows visible cards by Type filter and restores them on clear', async () => {
    await setup();
    component.setTypeFilter('feature');
    fixture.detectChanges();
    expect(text()).toContain('Dark mode');
    expect(text()).not.toContain('Login fails');

    component.clearFilters();
    fixture.detectChanges();
    expect(text()).toContain('Login fails');
    expect(text()).toContain('Dark mode');
  });

  it('narrows visible cards by title search', async () => {
    await setup();
    component.setSearch('dark');
    fixture.detectChanges();
    expect(text()).toContain('Dark mode');
    expect(text()).not.toContain('Login fails');
  });

  it('narrows visible cards by Epic filter', async () => {
    await setup();
    component.setEpicFilter('e1');
    fixture.detectChanges();
    expect(text()).toContain('Login fails');
    expect(text()).not.toContain('Dark mode');
  });

  it('shows an error banner when tickets fail to load', async () => {
    ticketsSvc.getByTeam.mockReturnValue(throwError(() => new Error('boom')));
    await setup();
    expect(text()).toContain('Failed to load tickets.');
  });

  it('shows an empty-state message when there are no teams', async () => {
    teamsSvc.getAll.mockReturnValue(of([]));
    await setup();
    expect(text()).toContain('Create a team first');
    expect(ticketsSvc.getByTeam).not.toHaveBeenCalled();
  });

  it('opens the create dialog scoped to the selected team and its epics', async () => {
    await setup();
    component.openCreate();
    expect(dialog.open).toHaveBeenCalledWith(
      TicketFormDialogComponent,
      expect.objectContaining({ data: expect.objectContaining({ teamId: 'team-1', ticket: null }) }),
    );
  });

  it('fetches the full ticket then opens the edit dialog', async () => {
    const detail: TicketDetail = {
      ...ticket({ id: 't1', title: 'Login fails' }),
      body: 'full body',
      createdById: 'user-1',
    };
    ticketsSvc.getById.mockReturnValue(of(detail));
    await setup();

    component.startEdit(ticket({ id: 't1', title: 'Login fails' }));

    expect(ticketsSvc.getById).toHaveBeenCalledWith('t1');
    expect(dialog.open).toHaveBeenCalledWith(
      TicketFormDialogComponent,
      expect.objectContaining({ data: expect.objectContaining({ ticket: detail }) }),
    );
  });

  it('reloads tickets after the form dialog reports a save', async () => {
    await setup(); // getByTeam called once
    dialog.open.mockReturnValue(dialogClosing(true));
    component.openCreate();
    expect(ticketsSvc.getByTeam).toHaveBeenCalledTimes(2);
  });

  it('confirms then deletes a ticket', async () => {
    await setup();
    dialog.open.mockReturnValue(dialogClosing(true));
    component.confirmDelete(ticket({ id: 't1', title: 'Login fails' }));
    expect(dialog.open).toHaveBeenCalledWith(ConfirmDialogComponent, expect.anything());
    expect(ticketsSvc.delete).toHaveBeenCalledWith('t1');
  });

  it('does not delete when the confirm dialog is cancelled', async () => {
    await setup();
    dialog.open.mockReturnValue(dialogClosing(false));
    component.confirmDelete(ticket({ id: 't1' }));
    expect(ticketsSvc.delete).not.toHaveBeenCalled();
  });

  it('changes state via the selector and reloads', async () => {
    await setup(); // getByTeam called once
    component.changeState(ticket({ id: 't1', state: 'new' }), 'done');
    expect(ticketsSvc.changeState).toHaveBeenCalledWith('t1', 'done');
    expect(ticketsSvc.getByTeam).toHaveBeenCalledTimes(2);
  });

  it('ignores a no-op state change', async () => {
    await setup();
    component.changeState(ticket({ id: 't1', state: 'new' }), 'new');
    expect(ticketsSvc.changeState).not.toHaveBeenCalled();
  });
});
