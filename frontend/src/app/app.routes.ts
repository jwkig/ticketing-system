import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'board' },
  {
    // Public auth screens: /login, /signup, /verify-email, /resend-verification
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.authRoutes),
  },
  {
    path: 'board',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/board/board.component').then((m) => m.BoardComponent),
  },
  { path: '**', redirectTo: 'login' },
];
