import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let router: Router;
  const auth = { login: vi.fn() };

  beforeEach(async () => {
    auth.login.mockReset();
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('does not call the service when the form is invalid', () => {
    component.submit();
    expect(auth.login).not.toHaveBeenCalled();
  });

  it('logs in and navigates to /board on success', async () => {
    const nav = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    auth.login.mockReturnValue(of({ token: 't' }));
    component.form.setValue({ email: 'a@b.com', password: 'secret1' });

    component.submit();
    await fixture.whenStable();

    expect(auth.login).toHaveBeenCalledWith({ email: 'a@b.com', password: 'secret1' });
    expect(nav).toHaveBeenCalledWith(['/board']);
  });

  it('shows a resend link when the account is not verified', () => {
    auth.login.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { error: 'Email address has not been verified.' },
          }),
      ),
    );
    component.form.setValue({ email: 'a@b.com', password: 'secret1' });

    component.submit();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Resend email');
    expect((fixture.nativeElement as HTMLElement).querySelector('.error-banner')).not.toBeNull();
  });
});
