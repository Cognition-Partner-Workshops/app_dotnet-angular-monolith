import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

/**
 * Root application shell component.
 * Renders the top navigation bar with links to each domain module
 * and a `<router-outlet>` for lazy-loaded feature components.
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
