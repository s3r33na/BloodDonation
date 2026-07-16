import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from './services/api.service';

// Guards
export const authGuard = () => {
  const api = inject(ApiService);
  const router = inject(Router);
  if (api.token() || api.isDemoMode()) return true;
  router.navigate(['/login']);
  return false;
};

export const adminGuard = () => {
  const api = inject(ApiService);
  const router = inject(Router);
  if (api.token() && api.currentUser()?.role === 'Admin') return true;
  router.navigate(['/dashboard']);
  return false;
};

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./components/login').then(m => m.Login)
  },
  {
    path: 'register',
    loadComponent: () => import('./components/register').then(m => m.Register)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/dashboard').then(m => m.Dashboard),
    canActivate: [authGuard]
  },
  {
    path: 'screening',
    loadComponent: () => import('./components/donation-form').then(m => m.DonationForm),
    canActivate: [authGuard]
  },
  {
    path: 'feed',
    loadComponent: () => import('./components/feed').then(m => m.Feed),
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadComponent: () => import('./components/admin-dashboard').then(m => m.AdminDashboard),
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'admin/posts',
    loadComponent: () => import('./components/admin-posts').then(m => m.AdminPosts),
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'admin/users',
    loadComponent: () => import('./components/admin-users').then(m => m.AdminUsers),
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'admin/appointments',
    loadComponent: () => import('./components/admin-appointments').then(m => m.AdminAppointments),
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'admin/reports',
    loadComponent: () => import('./components/admin-reports').then(m => m.AdminReports),
    canActivate: [authGuard, adminGuard]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
