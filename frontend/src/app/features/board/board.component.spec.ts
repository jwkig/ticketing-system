import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { EpicsService } from '../../core/epics/epics.service';
import { TeamsService } from '../../core/teams/teams.service';
import { TicketsService } from '../../core/tickets/tickets.service';
import { Team } from '../../domain/team.model';
import { TicketSummary } from '../../domain/ticket.model';
import { BoardComponent } from './board.component';

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

describe('BoardComponent', () => {
  let teamsSvc: { getAll: ReturnType<typeof vi.fn> };
  let ticketsSvc: { getByTeam: ReturnType<typeof vi.fn> };
  let epicsSvc: { getByTeam: ReturnType<typeof vi.fn> };

  function createComponent() {
    const fixture = TestBed.createComponent(BoardComponent);
    fixture.detectChanges();
    return fixture;
  }

  function text(fixture: ReturnType<typeof createComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  beforeEach(() => {
    teamsSvc = { getAll: vi.fn().mockReturnValue(of([team])) };
    ticketsSvc = {
      getByTeam: vi.fn().mockReturnValue(
        of([
          ticket({ id: 't1', title: 'Login fails', type: 'bug', state: 'new', epicId: 'e1', epicTitle: 'Auth' }),
          ticket({ id: 't2', title: 'Dark mode', type: 'feature', state: 'in_progress' }),
        ]),
      ),
    };
    epicsSvc = {
      getByTeam: vi.fn().mockReturnValue(of([{ id: 'e1', teamId: 'team-1', title: 'Auth', description: null, createdAt: '', modifiedAt: '', ticketCount: 1 }])),
    };

    TestBed.configureTestingModule({
      imports: [BoardComponent],
      providers: [
        { provide: TeamsService, useValue: teamsSvc },
        { provide: TicketsService, useValue: ticketsSvc },
        { provide: EpicsService, useValue: epicsSvc },
      ],
    });
  });

  it('loads the first team and renders its tickets in the right columns', () => {
    const fixture = createComponent();
    expect(teamsSvc.getAll).toHaveBeenCalled();
    expect(ticketsSvc.getByTeam).toHaveBeenCalledWith('team-1');
    const content = text(fixture);
    expect(content).toContain('Login fails');
    expect(content).toContain('Dark mode');
    expect(content).toContain('Epic: Auth');
  });

  it('narrows visible cards by Type filter and restores them on clear', () => {
    const fixture = createComponent();
    const comp = fixture.componentInstance;

    comp.setTypeFilter('feature');
    fixture.detectChanges();
    let content = text(fixture);
    expect(content).toContain('Dark mode');
    expect(content).not.toContain('Login fails');

    comp.clearFilters();
    fixture.detectChanges();
    content = text(fixture);
    expect(content).toContain('Login fails');
    expect(content).toContain('Dark mode');
  });

  it('narrows visible cards by title search', () => {
    const fixture = createComponent();
    const comp = fixture.componentInstance;

    comp.setSearch('dark');
    fixture.detectChanges();
    const content = text(fixture);
    expect(content).toContain('Dark mode');
    expect(content).not.toContain('Login fails');
  });

  it('narrows visible cards by Epic filter', () => {
    const fixture = createComponent();
    const comp = fixture.componentInstance;

    comp.setEpicFilter('e1');
    fixture.detectChanges();
    const content = text(fixture);
    expect(content).toContain('Login fails');
    expect(content).not.toContain('Dark mode');
  });

  it('shows an error banner when tickets fail to load', () => {
    ticketsSvc.getByTeam.mockReturnValue(throwError(() => new Error('boom')));
    const fixture = createComponent();
    expect(text(fixture)).toContain('Failed to load tickets.');
  });

  it('shows an empty-state message when there are no teams', () => {
    teamsSvc.getAll.mockReturnValue(of([]));
    const fixture = createComponent();
    expect(text(fixture)).toContain('Create a team first');
    expect(ticketsSvc.getByTeam).not.toHaveBeenCalled();
  });
});
