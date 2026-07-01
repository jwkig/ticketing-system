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
    // Authenticated shell hosting the guarded feature routes.
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      {
        path: 'board',
        loadComponent: () => import('./features/board/board.component').then((m) => m.BoardComponent),
      },
      {
        path: 'teams',
        loadComponent: () => import('./features/teams/teams.component').then((m) => m.TeamsComponent),
      },
      {
        path: 'epics',
        loadComponent: () => import('./features/epics/epics.component').then((m) => m.EpicsComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
