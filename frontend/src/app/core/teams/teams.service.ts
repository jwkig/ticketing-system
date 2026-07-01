import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Team, TeamNameRequest } from '../../domain/team.model';

/** CRUD access to the /api/teams endpoints. */
@Injectable({ providedIn: 'root' })
export class TeamsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/teams`;

  getAll(): Observable<Team[]> {
    return this.http.get<Team[]>(this.base);
  }

  create(body: TeamNameRequest): Observable<Team> {
    return this.http.post<Team>(this.base, body);
  }

  update(id: string, body: TeamNameRequest): Observable<Team> {
    return this.http.put<Team>(`${this.base}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
