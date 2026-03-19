import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Displays a table of all orders fetched from the `/api/orders` endpoint.
 * Shows order ID, customer name, date, status, and total amount.
 * Falls back to "No orders yet." when the list is empty.
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
  orders: any[] = [];
  constructor(private http: HttpClient) {}

  /** Fetches all orders from the backend on component initialization. */
  ngOnInit() { this.http.get<any[]>('/api/orders').subscribe(data => this.orders = data); }
}
