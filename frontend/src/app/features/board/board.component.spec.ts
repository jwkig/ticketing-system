import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { BoardComponent } from './board.component';

describe('BoardComponent', () => {
  let fixture: ComponentFixture<BoardComponent>;
  let router: Router;
  const auth = { logout: vi.fn(), currentUserEmail: signal('me@example.com') };

  beforeEach(async () => {
    auth.logout.mockReset();
    await TestBed.configureTestingModule({
      imports: [BoardComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();
    fixture = TestBed.createComponent(BoardComponent);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('shows the signed-in user email', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('me@example.com');
  });

  it('logs out and redirects to /login', () => {
    const nav = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentInstance.logout();
    expect(auth.logout).toHaveBeenCalled();
    expect(nav).toHaveBeenCalledWith(['/login']);
  });
});
