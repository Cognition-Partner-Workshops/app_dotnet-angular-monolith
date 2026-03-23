import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

/**
 * Root component of the OrderManager Angular application.
 * Renders the global navigation bar with links to the four main feature modules
 * (Orders, Products, Customers, Inventory) and a `<router-outlet>` where
 * lazy-loaded feature components are displayed.
 *
 * This is a standalone component that directly imports Angular Router directives.
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
  /** The application title displayed in the browser tab and navigation header. */
  title = 'OrderManager';
}
