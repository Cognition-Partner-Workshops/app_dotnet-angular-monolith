import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/upload', pathMatch: 'full' },
  { path: 'upload', loadComponent: () => import('./features/upload/upload.component').then(m => m.UploadComponent) },
  { path: 'tracker', loadComponent: () => import('./features/tracker/tracker.component').then(m => m.TrackerComponent) },
  { path: 'response/:rfpId', loadComponent: () => import('./features/response/response.component').then(m => m.ResponseComponent) },
  { path: 'settings', loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent) }
];
