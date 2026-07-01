import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { MainLayoutComponent } from './main-layout.component';

describe('MainLayoutComponent', () => {
  let fixture: ComponentFixture<MainLayoutComponent>;
  let router: Router;
  const auth = { logout: vi.fn(), currentUserEmail: signal('me@example.com') };

  beforeEach(async () => {
    auth.logout.mockReset();
    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();
    fixture = TestBed.createComponent(MainLayoutComponent);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('renders nav links and the user email', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Board');
    expect(text).toContain('Teams');
    expect(text).toContain('me@example.com');
  });

  it('logs out and redirects to /login', () => {
    const nav = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentInstance.logout();
    expect(auth.logout).toHaveBeenCalled();
    expect(nav).toHaveBeenCalledWith(['/login']);
  });
});
