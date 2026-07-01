import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../core/notification/notification.service';
import { TeamsService } from '../../core/teams/teams.service';
import { Team } from '../../domain/team.model';
import { TeamsComponent } from './teams.component';

function makeTeam(over: Partial<Team> = {}): Team {
  return {
    id: 't1',
    name: 'Backend',
    createdAt: '2026-01-01T00:00:00Z',
    modifiedAt: '2026-01-01T00:00:00Z',
    ticketCount: 0,
    epicCount: 0,
    ...over,
  };
}

describe('TeamsComponent', () => {
  let fixture: ComponentFixture<TeamsComponent>;
  let component: TeamsComponent;
  const service = { getAll: vi.fn(), create: vi.fn(), update: vi.fn(), delete: vi.fn() };
  const dialog = { open: vi.fn() };
  const notify = { success: vi.fn(), error: vi.fn() };

  async function setup() {
    await TestBed.configureTestingModule({
      imports: [TeamsComponent],
      providers: [
        provideRouter([]),
        { provide: TeamsService, useValue: service },
        { provide: NotificationService, useValue: notify },
      ],
    })
      // MatDialog comes from the component's imported MatDialogModule, so override
      // it on the component injector rather than the root providers.
      .overrideComponent(TeamsComponent, {
        add: { providers: [{ provide: MatDialog, useValue: dialog }] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(TeamsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // triggers ngOnInit → load()
  }

  beforeEach(() => {
    service.getAll.mockReset().mockReturnValue(of([]));
    service.create.mockReset();
    service.update.mockReset();
    service.delete.mockReset();
    dialog.open.mockReset();
    notify.success.mockReset();
  });

  it('loads and renders teams on init', async () => {
    service.getAll.mockReturnValue(of([makeTeam({ name: 'Payments' })]));
    await setup();
    expect(service.getAll).toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Payments');
  });

  it('creates a team and reloads', async () => {
    await setup();
    service.create.mockReturnValue(of(makeTeam({ name: 'New' })));
    service.getAll.mockReturnValue(of([makeTeam({ name: 'New' })]));

    component.form.controls.name.setValue('New');
    component.submit();

    expect(service.create).toHaveBeenCalledWith({ name: 'New' });
    expect(notify.success).toHaveBeenCalled();
  });

  it('edits an existing team via update', async () => {
    await setup();
    service.update.mockReturnValue(of(makeTeam({ name: 'Renamed' })));
    service.getAll.mockReturnValue(of([makeTeam({ name: 'Renamed' })]));

    component.startEdit(makeTeam({ id: 't1', name: 'Backend' }));
    component.form.controls.name.setValue('Renamed');
    component.submit();

    expect(service.update).toHaveBeenCalledWith('t1', { name: 'Renamed' });
    expect(service.create).not.toHaveBeenCalled();
    expect(notify.success).toHaveBeenCalled();
  });

  it('shows an inline error when creation conflicts (409)', async () => {
    await setup();
    service.create.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { error: 'A team with this name already exists.' },
          }),
      ),
    );

    component.form.controls.name.setValue('Backend');
    component.submit();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.error-banner')).not.toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('already exists');
  });

  it('confirms then deletes a team', async () => {
    service.getAll.mockReturnValue(of([makeTeam()]));
    await setup();
    dialog.open.mockReturnValue({ afterClosed: () => of(true) });
    service.delete.mockReturnValue(of(undefined));

    component.confirmDelete(makeTeam());

    expect(dialog.open).toHaveBeenCalled();
    expect(service.delete).toHaveBeenCalledWith('t1');
  });

  it('does not delete when the dialog is cancelled', async () => {
    await setup();
    dialog.open.mockReturnValue({ afterClosed: () => of(false) });

    component.confirmDelete(makeTeam());

    expect(service.delete).not.toHaveBeenCalled();
  });

  it('disables delete when the team has tickets or epics', async () => {
    await setup();
    expect(component.canDelete(makeTeam({ ticketCount: 0, epicCount: 0 }))).toBe(true);
    expect(component.canDelete(makeTeam({ ticketCount: 2 }))).toBe(false);
    expect(component.canDelete(makeTeam({ epicCount: 1 }))).toBe(false);
  });
});
