import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

/**
 * Standalone component that displays a directory of all customers.
 *
 * Fetches customer data from the `/api/customers` endpoint on initialization and renders
 * them in a table showing the name, email, phone number, and city/state location.
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
  /** Array of customer objects fetched from the API. */
  customers: any[] = [];

  /** @param http - Angular HTTP client used to communicate with the backend API. */
  constructor(private http: HttpClient) {}

  /** Fetches all customers from the backend API when the component initializes. */
  ngOnInit() { this.http.get<any[]>('/api/customers').subscribe(data => this.customers = data); }
}
