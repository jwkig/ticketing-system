import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Epic, SaveEpicRequest } from '../../domain/epic.model';

/** CRUD access to epics (listed/created per team, updated/deleted by id). */
@Injectable({ providedIn: 'root' })
export class EpicsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getByTeam(teamId: string): Observable<Epic[]> {
    return this.http.get<Epic[]>(`${this.base}/teams/${teamId}/epics`);
  }

  create(teamId: string, body: SaveEpicRequest): Observable<Epic> {
    return this.http.post<Epic>(`${this.base}/teams/${teamId}/epics`, body);
  }

  update(id: string, body: SaveEpicRequest): Observable<Epic> {
    return this.http.put<Epic>(`${this.base}/epics/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/epics/${id}`);
  }
}
