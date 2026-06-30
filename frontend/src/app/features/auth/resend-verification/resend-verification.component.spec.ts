import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { ResendVerificationComponent } from './resend-verification.component';

function configure(email: string | null) {
  const queryParamMap = convertToParamMap(email === null ? {} : { email });
  const auth = { resendVerification: vi.fn().mockReturnValue(of(undefined)) };
  TestBed.configureTestingModule({
    imports: [ResendVerificationComponent],
    providers: [
      provideRouter([]),
      { provide: AuthService, useValue: auth },
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap } } },
    ],
  });
  return auth;
}

describe('ResendVerificationComponent', () => {
  let fixture: ComponentFixture<ResendVerificationComponent>;
  let component: ResendVerificationComponent;

  it('prefills the email from the query string', () => {
    configure('user@example.com');
    fixture = TestBed.createComponent(ResendVerificationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    expect(component.form.controls.email.value).toBe('user@example.com');
  });

  it('does not call the service when the email is invalid', () => {
    const auth = configure(null);
    fixture = TestBed.createComponent(ResendVerificationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.submit();
    expect(auth.resendVerification).not.toHaveBeenCalled();
  });

  it('sends and shows a confirmation on success', () => {
    const auth = configure('user@example.com');
    fixture = TestBed.createComponent(ResendVerificationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.submit();
    fixture.detectChanges();

    expect(auth.resendVerification).toHaveBeenCalledWith('user@example.com');
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('a new verification link is on its way');
  });
});
