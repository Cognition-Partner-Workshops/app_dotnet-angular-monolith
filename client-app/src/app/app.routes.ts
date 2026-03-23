import { Routes } from '@angular/router';

/**
 * Application route configuration for the OrderManager SPA.
 *
 * Uses lazy-loading (`loadComponent`) to defer the download of each feature module
 * until the user navigates to its route. This improves initial load performance by
 * splitting the application into smaller bundles.
 *
 * Routes:
 * - `/`          — Redirects to `/orders` (default landing page)
 * - `/orders`    — Lazy-loads the OrderListComponent (order management view)
 * - `/products`  — Lazy-loads the ProductListComponent (product catalog view)
 * - `/customers` — Lazy-loads the CustomerListComponent (customer directory view)
 * - `/inventory` — Lazy-loads the InventoryListComponent (warehouse stock view)
 */
export const routes: Routes = [
  { path: '', redirectTo: '/orders', pathMatch: 'full' },
  { path: 'orders', loadComponent: () => import('./modules/orders/order-list.component').then(m => m.OrderListComponent) },
  { path: 'products', loadComponent: () => import('./modules/products/product-list.component').then(m => m.ProductListComponent) },
  { path: 'customers', loadComponent: () => import('./modules/customers/customer-list.component').then(m => m.CustomerListComponent) },
  { path: 'inventory', loadComponent: () => import('./modules/inventory/inventory-list.component').then(m => m.InventoryListComponent) },
];
