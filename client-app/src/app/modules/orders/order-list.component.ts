import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DatePipe, CurrencyPipe } from '@angular/common';

@Component({
    selector: 'app-order-list',
    imports: [DatePipe, CurrencyPipe],
    template: `
    <h2>Orders</h2>
    @if (orders.length) {
      <table>
        <thead><tr><th>ID</th><th>Customer</th><th>Date</th><th>Status</th><th>Total</th></tr></thead>
        <tbody>
          @for (o of orders; track o.id) {
            <tr>
              <td>{{o.id}}</td><td>{{o.customer?.name}}</td><td>{{o.orderDate | date}}</td><td>{{o.status}}</td><td>{{o.totalAmount | currency}}</td>
            </tr>
          }
        </tbody>
      </table>
    } @else {
      <p>No orders yet.</p>
    }
  `
})
export class OrderListComponent implements OnInit {
  orders: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() { this.http.get<any[]>('/api/orders').subscribe(data => this.orders = data); }
}
