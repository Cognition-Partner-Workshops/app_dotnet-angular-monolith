import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TrackerApiService } from '../../shared/services/tracker-api.service';
import { TrackerEntry } from '../../shared/models/tracker-entry.model';
import { saveAs } from 'file-saver';

@Component({
  selector: 'app-tracker',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatTableModule, MatSortModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatChipsModule,
    MatSnackBarModule, MatTooltipModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="container">
      <div class="page-header">
        <h1>RFP Tracker Dashboard</h1>
        <p>Track and manage all RFP submissions</p>
      </div>

      <mat-card class="filter-card">
        <mat-card-content>
          <div class="filters">
            <mat-form-field appearance="outline">
              <mat-label>Search</mat-label>
              <input matInput [(ngModel)]="searchText" (ngModelChange)="applyFilter()" placeholder="Search by title, client, or RFP ID">
              <mat-icon matSuffix>search</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Status</mat-label>
              <mat-select [(ngModel)]="statusFilter" (ngModelChange)="applyFilter()">
                <mat-option value="">All</mat-option>
                <mat-option value="New">New</mat-option>
                <mat-option value="In Progress">In Progress</mat-option>
                <mat-option value="Pending CRM">Pending CRM</mat-option>
                <mat-option value="Submitted">Submitted</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Priority</mat-label>
              <mat-select [(ngModel)]="priorityFilter" (ngModelChange)="applyFilter()">
                <mat-option value="">All</mat-option>
                <mat-option value="High">High</mat-option>
                <mat-option value="Medium">Medium</mat-option>
                <mat-option value="Low">Low</mat-option>
              </mat-select>
            </mat-form-field>

            <button mat-raised-button color="accent" (click)="exportExcel()">
              <mat-icon>download</mat-icon> Export Excel
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="table-card">
        <div class="table-container" *ngIf="!loading">
          <table mat-table [dataSource]="filteredEntries" matSort (matSortChange)="sortData($event)">

            <ng-container matColumnDef="rfpId">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>RFP ID</th>
              <td mat-cell *matCellDef="let entry">
                <strong>{{ entry.rfpId }}</strong>
              </td>
            </ng-container>

            <ng-container matColumnDef="rfpTitle">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Title</th>
              <td mat-cell *matCellDef="let entry">{{ entry.rfpTitle }}</td>
            </ng-container>

            <ng-container matColumnDef="clientName">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Client</th>
              <td mat-cell *matCellDef="let entry">{{ entry.clientName }}</td>
            </ng-container>

            <ng-container matColumnDef="crmId">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>CRM ID</th>
              <td mat-cell *matCellDef="let entry">
                <span *ngIf="editingRfpId !== entry.rfpId">
                  <span *ngIf="entry.crmId" class="crm-present">{{ entry.crmId }}</span>
                  <span *ngIf="!entry.crmId" class="crm-missing">
                    Missing
                    <button mat-icon-button matTooltip="Add CRM ID" (click)="startEdit(entry)">
                      <mat-icon>edit</mat-icon>
                    </button>
                  </span>
                </span>
                <span *ngIf="editingRfpId === entry.rfpId" class="inline-edit">
                  <input [(ngModel)]="editCrmId" placeholder="CRM-2025-XXX" class="crm-input">
                  <button mat-icon-button color="primary" (click)="saveCrmId(entry)" matTooltip="Save">
                    <mat-icon>check</mat-icon>
                  </button>
                  <button mat-icon-button (click)="cancelEdit()" matTooltip="Cancel">
                    <mat-icon>close</mat-icon>
                  </button>
                </span>
              </td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
              <td mat-cell *matCellDef="let entry">
                <span [class]="'status-chip ' + getStatusClass(entry.status)">{{ entry.status }}</span>
              </td>
            </ng-container>

            <ng-container matColumnDef="priority">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Priority</th>
              <td mat-cell *matCellDef="let entry">{{ entry.priority }}</td>
            </ng-container>

            <ng-container matColumnDef="dueDate">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Due Date</th>
              <td mat-cell *matCellDef="let entry">{{ entry.dueDate ? (entry.dueDate | date:'mediumDate') : '-' }}</td>
            </ng-container>

            <ng-container matColumnDef="assignedTo">
              <th mat-header-cell *matHeaderCellDef>Assigned To</th>
              <td mat-cell *matCellDef="let entry">{{ entry.assignedTo || '-' }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"
                [class.missing-crm]="!row.crmId"
                [class.has-crm]="row.crmId"></tr>
          </table>
        </div>

        <div *ngIf="loading" class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
          <p>Loading tracker data...</p>
        </div>

        <div *ngIf="!loading && filteredEntries.length === 0" class="empty-state">
          <mat-icon>inbox</mat-icon>
          <p>No tracker entries found</p>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .filter-card { margin-bottom: 16px; }
    .filters { display: flex; gap: 16px; align-items: center; flex-wrap: wrap; }
    .filters mat-form-field { flex: 1; min-width: 200px; }
    .table-card { overflow: hidden; }
    .table-container { overflow-x: auto; }
    table { width: 100%; }
    .missing-crm { background-color: #fff3f0 !important; }
    .has-crm { background-color: #f0fff4 !important; }
    .crm-present { color: #2e7d32; font-weight: 500; }
    .crm-missing { color: #c62828; display: flex; align-items: center; gap: 4px; }
    .inline-edit { display: flex; align-items: center; gap: 4px; }
    .crm-input { border: 1px solid #ccc; border-radius: 4px; padding: 4px 8px; width: 140px; }
    .loading-container { display: flex; flex-direction: column; align-items: center; padding: 48px; }
    .empty-state { text-align: center; padding: 48px; color: #999; }
    .empty-state mat-icon { font-size: 48px; height: 48px; width: 48px; }
    th.mat-header-cell { font-weight: 600; color: #333; }
  `]
})
export class TrackerComponent implements OnInit {
  entries: TrackerEntry[] = [];
  filteredEntries: TrackerEntry[] = [];
  displayedColumns = ['rfpId', 'rfpTitle', 'clientName', 'crmId', 'status', 'priority', 'dueDate', 'assignedTo'];
  searchText = '';
  statusFilter = '';
  priorityFilter = '';
  loading = true;
  editingRfpId: string | null = null;
  editCrmId = '';

  constructor(
    private trackerApi: TrackerApiService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadEntries();
  }

  loadEntries(): void {
    this.loading = true;
    this.trackerApi.getAll().subscribe({
      next: (entries) => {
        this.entries = entries;
        this.applyFilter();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackBar.open('Failed to load tracker data', 'Close', { duration: 5000 });
      }
    });
  }

  applyFilter(): void {
    let filtered = this.entries;
    if (this.searchText) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(e =>
        e.rfpId.toLowerCase().includes(search) ||
        e.rfpTitle.toLowerCase().includes(search) ||
        e.clientName.toLowerCase().includes(search)
      );
    }
    if (this.statusFilter) {
      filtered = filtered.filter(e => e.status === this.statusFilter);
    }
    if (this.priorityFilter) {
      filtered = filtered.filter(e => e.priority === this.priorityFilter);
    }
    this.filteredEntries = filtered;
  }

  sortData(sort: Sort): void {
    if (!sort.active || sort.direction === '') {
      this.applyFilter();
      return;
    }
    this.filteredEntries = [...this.filteredEntries].sort((a, b) => {
      const isAsc = sort.direction === 'asc';
      const key = sort.active as keyof TrackerEntry;
      const aVal = a[key] ?? '';
      const bVal = b[key] ?? '';
      return (aVal < bVal ? -1 : aVal > bVal ? 1 : 0) * (isAsc ? 1 : -1);
    });
  }

  startEdit(entry: TrackerEntry): void {
    this.editingRfpId = entry.rfpId;
    this.editCrmId = '';
  }

  cancelEdit(): void {
    this.editingRfpId = null;
    this.editCrmId = '';
  }

  saveCrmId(entry: TrackerEntry): void {
    if (!this.editCrmId) return;
    this.trackerApi.update(entry.rfpId, { ...entry, crmId: this.editCrmId }).subscribe({
      next: (updated) => {
        const idx = this.entries.findIndex(e => e.rfpId === entry.rfpId);
        if (idx >= 0) this.entries[idx] = updated;
        this.applyFilter();
        this.editingRfpId = null;
        this.snackBar.open('CRM ID updated successfully', 'OK', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to update CRM ID', 'Close', { duration: 5000 });
      }
    });
  }

  exportExcel(): void {
    this.trackerApi.exportToExcel().subscribe({
      next: (blob) => saveAs(blob, 'rfp-tracker.xlsx'),
      error: () => this.snackBar.open('Export failed', 'Close', { duration: 5000 })
    });
  }

  getStatusClass(status: string): string {
    return status.toLowerCase().replace(/\s+/g, '-');
  }
}
