import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/notification/notification.service';
import { SignupComponent } from './signup.component';

describe('SignupComponent', () => {
  let fixture: ComponentFixture<SignupComponent>;
  let component: SignupComponent;
  let router: Router;
  const auth = { signUp: jest.fn() };
  const notify = { success: jest.fn(), error: jest.fn() };

  beforeEach(async () => {
    auth.signUp.mockReset();
    notify.success.mockReset();
    await TestBed.configureTestingModule({
      imports: [SignupComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notify },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SignupComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('rejects a short password', () => {
    component.form.setValue({ email: 'a@b.com', password: 'short', confirmPassword: 'short' });
    expect(component.form.controls.password.hasError('minlength')).toBe(true);
  });

  it('rejects mismatched passwords', () => {
    component.form.setValue({ email: 'a@b.com', password: 'password1', confirmPassword: 'password2' });
    expect(component.form.hasError('mismatch')).toBe(true);
  });

  it('does not call the service when invalid', () => {
    component.submit();
    expect(auth.signUp).not.toHaveBeenCalled();
  });

  it('signs up, notifies, and routes to login on success', async () => {
    const nav = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    auth.signUp.mockReturnValue(of(undefined));
    component.form.setValue({ email: 'a@b.com', password: 'password1', confirmPassword: 'password1' });

    component.submit();
    await fixture.whenStable();

    expect(auth.signUp).toHaveBeenCalledWith({ email: 'a@b.com', password: 'password1' });
    expect(notify.success).toHaveBeenCalled();
    expect(nav).toHaveBeenCalledWith(['/login']);
  });
});
