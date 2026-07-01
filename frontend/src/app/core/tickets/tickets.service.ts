import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TicketSummary } from '../../domain/ticket.model';

/** Read-only access to a team's tickets for the Kanban board. */
@Injectable({ providedIn: 'root' })
export class TicketsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getByTeam(teamId: string): Observable<TicketSummary[]> {
    return this.http.get<TicketSummary[]>(`${this.base}/teams/${teamId}/tickets`);
  }
}
