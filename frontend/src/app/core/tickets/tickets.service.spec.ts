import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TicketsService } from './tickets.service';

describe('TicketsService', () => {
  let service: TicketsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TicketsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getByTeam GETs /api/teams/:teamId/tickets', () => {
    service.getByTeam('team-1').subscribe();
    const req = httpMock.expectOne('/api/teams/team-1/tickets');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getById GETs /api/tickets/:id', () => {
    service.getById('ticket-1').subscribe();
    const req = httpMock.expectOne('/api/tickets/ticket-1');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('create POSTs to /api/teams/:teamId/tickets', () => {
    const body = { type: 'bug' as const, title: 'T', body: 'B', epicId: null };
    service.create('team-1', body).subscribe();
    const req = httpMock.expectOne('/api/teams/team-1/tickets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  it('update PUTs /api/tickets/:id', () => {
    const body = { type: 'feature' as const, title: 'New', body: 'B', epicId: 'epic-1' };
    service.update('ticket-1', body).subscribe();
    const req = httpMock.expectOne('/api/tickets/ticket-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  it('changeState PATCHes /api/tickets/:id/state', () => {
    service.changeState('ticket-1', 'in_progress').subscribe();
    const req = httpMock.expectOne('/api/tickets/ticket-1/state');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ state: 'in_progress' });
    req.flush({});
  });

  it('delete DELETEs /api/tickets/:id', () => {
    service.delete('ticket-1').subscribe();
    const req = httpMock.expectOne('/api/tickets/ticket-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
