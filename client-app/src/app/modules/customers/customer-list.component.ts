import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Displays a table of all customers fetched from the `/api/customers` endpoint.
 * Shows name, email, phone, and city/state.
 * Lazy-loaded when the user navigates to the `/customers` route.
 */
@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Customers</h2>
    <table *ngIf="customers.length">
      <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>City</th></tr></thead>
      <tbody>
        <tr *ngFor="let c of customers">
          <td>{{c.name}}</td><td>{{c.email}}</td><td>{{c.phone}}</td><td>{{c.city}}, {{c.state}}</td>
        </tr>
      </tbody>
    </table>
  `
})
export class CustomerListComponent implements OnInit {
  /** The list of customers retrieved from the backend API. */
  customers: any[] = [];

  constructor(private http: HttpClient) {}

  /** Fetches all customers from the API on component initialization. */
  ngOnInit() { this.http.get<any[]>('/api/customers').subscribe(data => this.customers = data); }
}
