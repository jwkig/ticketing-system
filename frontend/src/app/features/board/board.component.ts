import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { EpicsService } from '../../core/epics/epics.service';
import { TeamsService } from '../../core/teams/teams.service';
import { TicketsService } from '../../core/tickets/tickets.service';
import { Epic } from '../../domain/epic.model';
import { Team } from '../../domain/team.model';
import {
  BOARD_COLUMNS,
  TICKET_TYPES,
  TicketSummary,
  TicketType,
} from '../../domain/ticket.model';
import { ErrorBannerComponent } from '../../shared/components/error-banner/error-banner.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

interface BoardColumn {
  state: string;
  label: string;
  tickets: TicketSummary[];
}

/**
 * Read-only Kanban board: pick a team, then view its tickets grouped into the
 * five fixed workflow columns. Client-side Type/Epic/title filters narrow the
 * cards. Ticket create/edit and drag-and-drop state changes are out of scope.
 */
@Component({
  selector: 'app-board',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatIconModule,
    ErrorBannerComponent,
    LoadingSpinnerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './board.component.html',
  styles: `
    .board-controls {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      align-items: center;
      margin: 0.5rem 0 1rem;
    }
    .team-picker {
      min-width: 240px;
    }
    .filter-row {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      align-items: center;
      margin-bottom: 1rem;
    }
    .search-field {
      min-width: 220px;
    }
    .filter-field {
      min-width: 180px;
    }
    .board {
      display: flex;
      gap: 1rem;
      overflow-x: auto;
      align-items: flex-start;
    }
    .column {
      flex: 1 0 220px;
      min-width: 220px;
      background: rgba(0, 0, 0, 0.03);
      border-radius: 8px;
      padding: 0.75rem;
    }
    .column-head {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      margin-bottom: 0.75rem;
      font-weight: 600;
    }
    .column-count {
      opacity: 0.6;
      font-weight: 400;
    }
    .ticket-card {
      margin-bottom: 0.75rem;
    }
    .type-badge {
      display: inline-block;
      font-size: 0.7rem;
      font-weight: 700;
      letter-spacing: 0.04em;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      background: #e0e0e0;
      color: #333;
      text-transform: uppercase;
    }
    .type-bug { background: #ffcdd2; color: #b71c1c; }
    .type-feature { background: #c8e6c9; color: #1b5e20; }
    .type-fix { background: #bbdefb; color: #0d47a1; }
    .card-title {
      margin: 0.4rem 0 0.2rem;
      font-weight: 500;
    }
    .card-epic {
      font-size: 0.8rem;
      opacity: 0.7;
    }
    .column-empty {
      font-size: 0.85rem;
      opacity: 0.5;
    }
    .empty {
      opacity: 0.7;
      margin: 1rem 0;
    }
  `,
})
export class BoardComponent implements OnInit {
  private readonly teamsSvc = inject(TeamsService);
  private readonly ticketsSvc = inject(TicketsService);
  private readonly epicsSvc = inject(EpicsService);

  protected readonly teams = signal<Team[]>([]);
  protected readonly epics = signal<Epic[]>([]);
  protected readonly tickets = signal<TicketSummary[]>([]);
  protected readonly selectedTeamId = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');

  protected readonly typeFilter = signal<TicketType | ''>('');
  protected readonly epicFilter = signal<string>(''); // epic id, or '' for all
  protected readonly search = signal('');

  protected readonly ticketTypes = TICKET_TYPES;

  /** The five fixed columns, each with the tickets matching its state + active filters. */
  protected readonly columns = computed<BoardColumn[]>(() => {
    const type = this.typeFilter();
    const epicId = this.epicFilter();
    const term = this.search().trim().toLowerCase();
    const filtered = this.tickets().filter((t) => {
      if (type && t.type !== type) return false;
      if (epicId && t.epicId !== epicId) return false;
      if (term && !t.title.toLowerCase().includes(term)) return false;
      return true;
    });
    return BOARD_COLUMNS.map((col) => ({
      state: col.state,
      label: col.label,
      tickets: filtered.filter((t) => t.state === col.state),
    }));
  });

  ngOnInit(): void {
    this.teamsSvc.getAll().subscribe({
      next: (teams) => {
        this.teams.set(teams);
        if (teams.length > 0) {
          this.selectTeam(teams[0].id);
        }
      },
      error: () => this.errorMessage.set('Failed to load teams.'),
    });
  }

  selectTeam(teamId: string): void {
    this.selectedTeamId.set(teamId);
    this.clearFilters();
    this.loadEpics();
    this.loadTickets();
  }

  setTypeFilter(value: TicketType | ''): void {
    this.typeFilter.set(value);
  }

  setEpicFilter(value: string): void {
    this.epicFilter.set(value);
  }

  setSearch(value: string): void {
    this.search.set(value);
  }

  clearFilters(): void {
    this.typeFilter.set('');
    this.epicFilter.set('');
    this.search.set('');
  }

  private loadEpics(): void {
    const teamId = this.selectedTeamId();
    if (!teamId) {
      this.epics.set([]);
      return;
    }
    this.epicsSvc.getByTeam(teamId).subscribe({
      next: (epics) => this.epics.set(epics),
      error: () => this.epics.set([]),
    });
  }

  private loadTickets(): void {
    const teamId = this.selectedTeamId();
    if (!teamId) {
      this.tickets.set([]);
      return;
    }
    this.loading.set(true);
    this.errorMessage.set('');
    this.ticketsSvc.getByTeam(teamId).subscribe({
      next: (tickets) => {
        this.tickets.set(tickets);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load tickets.');
        this.loading.set(false);
      },
    });
  }
}
