import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Standalone component that displays a list of all customer orders.
 *
 * Fetches order data from the `/api/orders` endpoint on initialization and renders
 * them in a table showing the order ID, customer name, order date, status, and total amount.
 * Displays an empty-state message when no orders exist.
 *
 * Uses Angular's `date` and `currency` pipes for formatting.
 */
@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Orders</h2>
    <table *ngIf="orders.length">
      <thead><tr><th>ID</th><th>Customer</th><th>Date</th><th>Status</th><th>Total</th></tr></thead>
      <tbody>
        <tr *ngFor="let o of orders">
          <td>{{o.id}}</td><td>{{o.customer?.name}}</td><td>{{o.orderDate | date}}</td><td>{{o.status}}</td><td>{{o.totalAmount | currency}}</td>
        </tr>
      </tbody>
    </table>
    <p *ngIf="!orders.length">No orders yet.</p>
  `
})
export class OrderListComponent implements OnInit {
  /** Array of order objects fetched from the API. */
  orders: any[] = [];

  /** @param http - Angular HTTP client used to communicate with the backend API. */
  constructor(private http: HttpClient) {}

  /** Fetches all orders from the backend API when the component initializes. */
  ngOnInit() { this.http.get<any[]>('/api/orders').subscribe(data => this.orders = data); }
}
