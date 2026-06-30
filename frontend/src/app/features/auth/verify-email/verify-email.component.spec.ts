import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { VerifyEmailComponent } from './verify-email.component';

function configure(token: string | null, verify: () => unknown) {
  const queryParamMap: ParamMap = convertToParamMap(token === null ? {} : { token });
  return TestBed.configureTestingModule({
    imports: [VerifyEmailComponent],
    providers: [
      provideRouter([]),
      { provide: AuthService, useValue: { verifyEmail: vi.fn(verify) } },
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap } } },
    ],
  }).compileComponents();
}

function textOf(fixture: ComponentFixture<VerifyEmailComponent>): string {
  return (fixture.nativeElement as HTMLElement).textContent ?? '';
}

describe('VerifyEmailComponent', () => {
  it('shows success when the token verifies', async () => {
    await configure('good-token', () => of(undefined));
    const fixture = TestBed.createComponent(VerifyEmailComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(textOf(fixture)).toContain('Email verified');
  });

  it('shows an error when verification fails', async () => {
    await configure('bad-token', () => throwError(() => new Error('nope')));
    const fixture = TestBed.createComponent(VerifyEmailComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(textOf(fixture)).toContain('Expired or invalid link');
  });

  it('shows an error when no token is present', async () => {
    await configure(null, () => of(undefined));
    const fixture = TestBed.createComponent(VerifyEmailComponent);
    fixture.detectChanges();
    expect(textOf(fixture)).toContain('Expired or invalid link');
  });
});
