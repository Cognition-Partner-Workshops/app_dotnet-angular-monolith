import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { RfpApiService } from '../../shared/services/rfp-api.service';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatCheckboxModule, MatIconModule, MatProgressBarModule,
    MatSnackBarModule, MatChipsModule, MatListModule, MatDividerModule
  ],
  template: `
    <div class="container">
      <div class="page-header">
        <h1>Upload RFP Document</h1>
        <p>Upload an RFP/RFI document to generate a comprehensive AI-powered response</p>
      </div>

      <div class="upload-grid">
        <mat-card class="upload-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>cloud_upload</mat-icon>
            <mat-card-title>Document Upload</mat-card-title>
            <mat-card-subtitle>Supported formats: PDF, DOCX, TXT</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="drop-zone" 
                 [class.drag-over]="isDragOver"
                 (dragover)="onDragOver($event)" 
                 (dragleave)="onDragLeave($event)"
                 (drop)="onDrop($event)"
                 (click)="fileInput.click()">
              <mat-icon class="upload-icon">description</mat-icon>
              <p class="drop-text">Drag & drop your RFP document here</p>
              <p class="drop-subtext">or click to browse files</p>
              <input #fileInput type="file" hidden accept=".pdf,.docx,.txt" (change)="onFileSelected($event)">
            </div>

            <div *ngIf="selectedFile" class="selected-file">
              <mat-icon>insert_drive_file</mat-icon>
              <span>{{ selectedFile.name }} ({{ formatFileSize(selectedFile.size) }})</span>
              <button mat-icon-button (click)="removeFile()"><mat-icon>close</mat-icon></button>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metadata-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>info</mat-icon>
            <mat-card-title>RFP Metadata</mat-card-title>
            <mat-card-subtitle>Provide additional information about the RFP</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Client Name</mat-label>
              <input matInput [(ngModel)]="clientName" placeholder="Enter client name" required>
              <mat-icon matSuffix>business</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>CRM ID (Optional)</mat-label>
              <input matInput [(ngModel)]="crmId" placeholder="e.g., CRM-2025-001">
              <mat-icon matSuffix>badge</mat-icon>
              <mat-hint>Leave empty if not available. An email notification will be sent.</mat-hint>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Originator Email</mat-label>
              <input matInput [(ngModel)]="originatorEmail" placeholder="email@company.com" required>
              <mat-icon matSuffix>email</mat-icon>
            </mat-form-field>

            <div class="row">
              <mat-form-field appearance="outline" class="half-width">
                <mat-label>Due Date</mat-label>
                <input matInput type="date" [(ngModel)]="dueDate">
                <mat-icon matSuffix>calendar_today</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="half-width">
                <mat-label>Priority</mat-label>
                <mat-select [(ngModel)]="priority">
                  <mat-option value="Low">Low</mat-option>
                  <mat-option value="Medium">Medium</mat-option>
                  <mat-option value="High">High</mat-option>
                </mat-select>
              </mat-form-field>
            </div>

            <mat-divider></mat-divider>

            <div class="cloud-section">
              <mat-checkbox [(ngModel)]="isCloudMigrationInScope" color="primary">
                Cloud Migration in Scope
              </mat-checkbox>

              <mat-form-field *ngIf="isCloudMigrationInScope" appearance="outline" class="full-width cloud-provider">
                <mat-label>Preferred Cloud Provider</mat-label>
                <mat-select [(ngModel)]="preferredCloudProvider">
                  <mat-option value="Azure">Microsoft Azure</mat-option>
                  <mat-option value="AWS">Amazon Web Services (AWS)</mat-option>
                  <mat-option value="GCP">Google Cloud Platform (GCP)</mat-option>
                </mat-select>
              </mat-form-field>
            </div>
          </mat-card-content>

          <mat-card-actions align="end">
            <button mat-raised-button color="primary" (click)="submitRfp()" 
                    [disabled]="isUploading || !selectedFile || !clientName || !originatorEmail"
                    class="generate-btn">
              <mat-icon>{{ isUploading ? 'hourglass_top' : 'auto_awesome' }}</mat-icon>
              {{ isUploading ? 'Generating Response...' : 'Generate RFP Response' }}
            </button>
          </mat-card-actions>
        </mat-card>
      </div>

      <mat-progress-bar *ngIf="isUploading" mode="indeterminate" class="upload-progress"></mat-progress-bar>

      <mat-card *ngIf="processingStatus" class="status-card">
        <mat-card-header>
          <mat-icon mat-card-avatar>pending</mat-icon>
          <mat-card-title>Processing Status</mat-card-title>
          <mat-card-subtitle>RFP is being processed by AI agents</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <mat-list>
            <mat-list-item *ngFor="let agent of processingStatus.agentProgress">
              <mat-icon matListItemIcon [class]="agent.status.toLowerCase()">
                {{ agent.status === 'Completed' ? 'check_circle' : agent.status === 'InProgress' ? 'pending' : agent.status === 'Failed' ? 'error' : 'schedule' }}
              </mat-icon>
              <span matListItemTitle>{{ agent.agentName }}</span>
              <span matListItemLine>{{ agent.status }}</span>
            </mat-list-item>
          </mat-list>
          <div class="status-actions" *ngIf="processingStatus.status === 'Completed' || processingStatus.status === 'Draft - Pending CRM ID'">
            <button mat-raised-button color="accent" (click)="viewResponse()" class="view-response-btn">
              <mat-icon>visibility</mat-icon> View Generated RFP Response
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Recent uploads section -->
      <mat-card class="recent-card" *ngIf="recentUploads.length > 0">
        <mat-card-header>
          <mat-icon mat-card-avatar>history</mat-icon>
          <mat-card-title>Recent Uploads</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <mat-list>
            <mat-list-item *ngFor="let doc of recentUploads" (click)="navigateToResponse(doc.id)" class="clickable">
              <mat-icon matListItemIcon>description</mat-icon>
              <span matListItemTitle>{{ doc.clientName }} - {{ doc.fileName }}</span>
              <span matListItemLine>
                <span [class]="'status-chip ' + getStatusClass(doc.status)">{{ doc.status }}</span>
                &nbsp; {{ doc.priority }} priority &bull; Uploaded {{ doc.uploadedAt | date:'short' }}
              </span>
            </mat-list-item>
          </mat-list>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .upload-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px; }
    .upload-card, .metadata-card { height: fit-content; }
    .drop-zone {
      border: 2px dashed #ccc; border-radius: 12px; padding: 48px 24px;
      text-align: center; cursor: pointer; transition: all 0.3s;
      background: #fafafa; margin: 16px 0;
    }
    .drop-zone:hover, .drop-zone.drag-over { border-color: #3f51b5; background: #e8eaf6; }
    .upload-icon { font-size: 48px; height: 48px; width: 48px; color: #9e9e9e; }
    .drop-text { font-size: 16px; color: #666; margin: 8px 0 4px; }
    .drop-subtext { font-size: 13px; color: #999; }
    .selected-file {
      display: flex; align-items: center; gap: 8px; padding: 12px;
      background: #e8f5e9; border-radius: 8px; margin-top: 12px;
    }
    .full-width { width: 100%; }
    .half-width { width: 48%; }
    .row { display: flex; gap: 4%; }
    .cloud-section { margin-top: 16px; }
    .cloud-provider { margin-top: 12px; }
    .upload-progress { margin: 16px 0; }
    .status-card, .recent-card { margin-top: 24px; }
    .status-actions { margin-top: 16px; text-align: center; }
    .clickable { cursor: pointer; }
    .clickable:hover { background: #f5f5f5; }
    .generate-btn { font-size: 15px; padding: 8px 24px; }
    .view-response-btn { font-size: 16px; padding: 10px 32px; animation: pulse 1.5s infinite; }
    @keyframes pulse { 0%, 100% { transform: scale(1); } 50% { transform: scale(1.03); } }
    mat-icon.completed { color: #4caf50; }
    mat-icon.inprogress { color: #ff9800; }
    mat-icon.failed { color: #f44336; }
    mat-icon.pending { color: #9e9e9e; }
    @media (max-width: 768px) { .upload-grid { grid-template-columns: 1fr; } }
  `]
})
export class UploadComponent {
  selectedFile: File | null = null;
  clientName = '';
  crmId = '';
  originatorEmail = 'abc_xyz@gmail.com';
  dueDate = '';
  priority = 'Medium';
  isCloudMigrationInScope = false;
  preferredCloudProvider = 'Azure';
  isDragOver = false;
  isUploading = false;
  processingStatus: any = null;
  recentUploads: any[] = [];
  private pollingInterval: any;

  constructor(
    private rfpApi: RfpApiService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {
    this.loadRecentUploads();
  }

  loadRecentUploads(): void {
    this.rfpApi.getAll().subscribe({
      next: (docs) => this.recentUploads = docs.slice(0, 5),
      error: () => {}
    });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.selectedFile = files[0];
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
    }
  }

  removeFile(): void {
    this.selectedFile = null;
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / 1048576).toFixed(1) + ' MB';
  }

  submitRfp(): void {
    if (!this.selectedFile || !this.clientName || !this.originatorEmail) return;

    this.isUploading = true;
    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('clientName', this.clientName);
    formData.append('crmId', this.crmId || '');
    formData.append('originatorEmail', this.originatorEmail);
    formData.append('dueDate', this.dueDate || '');
    formData.append('priority', this.priority);
    formData.append('isCloudMigrationInScope', String(this.isCloudMigrationInScope));
    formData.append('preferredCloudProvider', this.preferredCloudProvider);

    this.rfpApi.upload(formData).subscribe({
      next: (doc) => {
        this.isUploading = false;
        this.snackBar.open('RFP uploaded successfully! Processing started.', 'OK', { duration: 5000 });
        this.startPolling(doc.id);
        this.loadRecentUploads();
      },
      error: (err) => {
        this.isUploading = false;
        this.snackBar.open('Upload failed: ' + (err.error || 'Unknown error'), 'Close', { duration: 5000 });
      }
    });
  }

  startPolling(rfpId: number): void {
    this.pollingInterval = setInterval(() => {
      this.rfpApi.getStatus(rfpId).subscribe({
        next: (status) => {
          this.processingStatus = status;
          if (status.status === 'Completed' || status.status === 'Draft - Pending CRM ID' || status.status === 'Partially Completed') {
            clearInterval(this.pollingInterval);
            this.loadRecentUploads();
          }
        }
      });
    }, 3000);
  }

  viewResponse(): void {
    if (this.processingStatus) {
      this.router.navigate(['/response', this.processingStatus.id]);
    }
  }

  navigateToResponse(id: number): void {
    this.router.navigate(['/response', id]);
  }

  getStatusClass(status: string): string {
    return status.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z-]/g, '');
  }

  ngOnDestroy(): void {
    if (this.pollingInterval) clearInterval(this.pollingInterval);
  }
}
