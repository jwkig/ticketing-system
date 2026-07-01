import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ChangeTicketStateRequest,
  SaveTicketRequest,
  TicketDetail,
  TicketState,
  TicketSummary,
} from '../../domain/ticket.model';

/** Access to a team's tickets for the Kanban board (list + full CRUD + state change). */
@Injectable({ providedIn: 'root' })
export class TicketsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getByTeam(teamId: string): Observable<TicketSummary[]> {
    return this.http.get<TicketSummary[]>(`${this.base}/teams/${teamId}/tickets`);
  }

  getById(id: string): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${this.base}/tickets/${id}`);
  }

  create(teamId: string, body: SaveTicketRequest): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(`${this.base}/teams/${teamId}/tickets`, body);
  }

  update(id: string, body: SaveTicketRequest): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${this.base}/tickets/${id}`, body);
  }

  changeState(id: string, state: TicketState): Observable<TicketDetail> {
    const body: ChangeTicketStateRequest = { state };
    return this.http.patch<TicketDetail>(`${this.base}/tickets/${id}/state`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/tickets/${id}`);
  }
}
