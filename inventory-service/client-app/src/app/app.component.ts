import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <nav>
      <h1>Inventory Service</h1>
      <a routerLink="/inventory">Inventory</a>
      <a routerLink="/low-stock">Low Stock</a>
    </nav>
    <router-outlet></router-outlet>
  `
})
export class AppComponent {
  title = 'Inventory Service';
}
