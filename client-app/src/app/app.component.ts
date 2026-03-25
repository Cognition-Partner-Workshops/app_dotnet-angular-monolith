import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

/**
 * Root component of the OrderManager SPA.
 * Renders the top-level navigation bar and a <router-outlet> where
 * feature components (Orders, Products, Customers, Inventory) are
 * lazy-loaded based on the current route.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <nav>
      <h1>OrderManager</h1>
      <a routerLink="/orders">Orders</a>
      <a routerLink="/products">Products</a>
      <a routerLink="/customers">Customers</a>
      <a routerLink="/inventory">Inventory</a>
    </nav>
    <router-outlet></router-outlet>
  `
})
export class AppComponent {
  title = 'OrderManager';
}
