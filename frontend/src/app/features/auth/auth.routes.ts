import { Routes } from '@angular/router';

/** Public authentication screens (mapped to Wireframe 2). */
export const authRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'signup',
    loadComponent: () => import('./signup/signup.component').then((m) => m.SignupComponent),
  },
  {
    path: 'verify-email',
    loadComponent: () =>
      import('./verify-email/verify-email.component').then((m) => m.VerifyEmailComponent),
  },
  {
    path: 'resend-verification',
    loadComponent: () =>
      import('./resend-verification/resend-verification.component').then(
        (m) => m.ResendVerificationComponent,
      ),
  },
];
