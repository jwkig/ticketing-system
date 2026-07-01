import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TeamsService } from './teams.service';

describe('TeamsService', () => {
  let service: TeamsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TeamsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll GETs /api/teams', () => {
    service.getAll().subscribe();
    const req = httpMock.expectOne('/api/teams');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('create POSTs the name', () => {
    service.create({ name: 'Backend' }).subscribe();
    const req = httpMock.expectOne('/api/teams');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Backend' });
    req.flush({});
  });

  it('update PUTs to /api/teams/:id', () => {
    service.update('id-1', { name: 'Renamed' }).subscribe();
    const req = httpMock.expectOne('/api/teams/id-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ name: 'Renamed' });
    req.flush({});
  });

  it('delete DELETEs /api/teams/:id', () => {
    service.delete('id-1').subscribe();
    const req = httpMock.expectOne('/api/teams/id-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
