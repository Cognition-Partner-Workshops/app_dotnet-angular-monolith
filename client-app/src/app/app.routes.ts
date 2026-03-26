import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/reels', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./modules/auth/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./modules/auth/register.component').then(m => m.RegisterComponent) },
  { path: 'reels', loadComponent: () => import('./modules/reels/reel-feed.component').then(m => m.ReelFeedComponent), canActivate: [authGuard] },
  { path: 'calls', loadComponent: () => import('./modules/calls/call-list.component').then(m => m.CallListComponent), canActivate: [authGuard] },
  { path: 'profile', loadComponent: () => import('./modules/calls/profile.component').then(m => m.ProfileComponent), canActivate: [authGuard] },
  // Legacy OrderManager routes
  { path: 'orders', loadComponent: () => import('./modules/orders/order-list.component').then(m => m.OrderListComponent) },
  { path: 'products', loadComponent: () => import('./modules/products/product-list.component').then(m => m.ProductListComponent) },
  { path: 'customers', loadComponent: () => import('./modules/customers/customer-list.component').then(m => m.CustomerListComponent) },
  { path: 'inventory', loadComponent: () => import('./modules/inventory/inventory-list.component').then(m => m.InventoryListComponent) },
  { path: '**', redirectTo: '/reels' },
];
