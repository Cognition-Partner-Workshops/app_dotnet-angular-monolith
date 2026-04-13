import { Routes } from '@angular/router';

/**
 * Top-level route configuration for the OrderManager SPA.
 * Each feature module is lazy-loaded via `loadComponent` for optimal bundle splitting.
 * The default route redirects to the orders view.
 */
export const routes: Routes = [
  { path: '', redirectTo: '/orders', pathMatch: 'full' },
  { path: 'orders', loadComponent: () => import('./modules/orders/order-list.component').then(m => m.OrderListComponent) },
  { path: 'products', loadComponent: () => import('./modules/products/product-list.component').then(m => m.ProductListComponent) },
  { path: 'customers', loadComponent: () => import('./modules/customers/customer-list.component').then(m => m.CustomerListComponent) },
  { path: 'inventory', loadComponent: () => import('./modules/inventory/inventory-list.component').then(m => m.InventoryListComponent) },
];
