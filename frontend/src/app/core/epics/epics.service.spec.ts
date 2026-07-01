import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EpicsService } from './epics.service';

describe('EpicsService', () => {
  let service: EpicsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(EpicsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getByTeam GETs /api/teams/:teamId/epics', () => {
    service.getByTeam('team-1').subscribe();
    const req = httpMock.expectOne('/api/teams/team-1/epics');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('create POSTs to /api/teams/:teamId/epics', () => {
    service.create('team-1', { title: 'Checkout', description: 'desc' }).subscribe();
    const req = httpMock.expectOne('/api/teams/team-1/epics');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ title: 'Checkout', description: 'desc' });
    req.flush({});
  });

  it('update PUTs /api/epics/:id', () => {
    service.update('epic-1', { title: 'New', description: null }).subscribe();
    const req = httpMock.expectOne('/api/epics/epic-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ title: 'New', description: null });
    req.flush({});
  });

  it('delete DELETEs /api/epics/:id', () => {
    service.delete('epic-1').subscribe();
    const req = httpMock.expectOne('/api/epics/epic-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
