import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatCheckboxModule, MatButtonModule, MatIconModule, MatSnackBarModule,
    MatDividerModule, MatListModule
  ],
  template: `
    <div class="container">
      <div class="page-header">
        <h1>Settings</h1>
        <p>Configure default settings for RFP processing</p>
      </div>

      <div class="settings-grid">
        <mat-card>
          <mat-card-header>
            <mat-icon mat-card-avatar>person</mat-icon>
            <mat-card-title>Default Configuration</mat-card-title>
            <mat-card-subtitle>Set default values for new RFP submissions</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Default Originator Email</mat-label>
              <input matInput [(ngModel)]="settings.defaultOriginatorEmail" placeholder="email@company.com">
              <mat-icon matSuffix>email</mat-icon>
            </mat-form-field>

            <mat-divider></mat-divider>

            <div class="section-title">Cloud Migration Defaults</div>

            <mat-checkbox [(ngModel)]="settings.defaultCloudMigrationInScope" color="primary">
              Cloud Migration in Scope by Default
            </mat-checkbox>

            <mat-form-field appearance="outline" class="full-width" style="margin-top: 16px;">
              <mat-label>Default Cloud Provider</mat-label>
              <mat-select [(ngModel)]="settings.defaultCloudProvider">
                <mat-option value="Azure">Microsoft Azure</mat-option>
                <mat-option value="AWS">Amazon Web Services (AWS)</mat-option>
                <mat-option value="GCP">Google Cloud Platform (GCP)</mat-option>
              </mat-select>
            </mat-form-field>
          </mat-card-content>
          <mat-card-actions align="end">
            <button mat-raised-button color="primary" (click)="saveSettings()">
              <mat-icon>save</mat-icon> Save Settings
            </button>
          </mat-card-actions>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-icon mat-card-avatar>smart_toy</mat-icon>
            <mat-card-title>AI Configuration</mat-card-title>
            <mat-card-subtitle>Configure AI service endpoints</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>AI Endpoint</mat-label>
              <input matInput [(ngModel)]="settings.aiEndpoint" placeholder="https://your-endpoint.openai.azure.com/">
              <mat-icon matSuffix>link</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Deployment Name</mat-label>
              <input matInput [(ngModel)]="settings.aiDeploymentName" placeholder="gpt-4o">
              <mat-icon matSuffix>memory</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>API Key</mat-label>
              <input matInput [(ngModel)]="settings.aiApiKey" type="password" placeholder="Enter API key">
              <mat-icon matSuffix>vpn_key</mat-icon>
            </mat-form-field>

            <div class="info-box">
              <mat-icon>info</mat-icon>
              <span>AI settings are configured in the backend appsettings.json. These fields are for display/reference only in the current version.</span>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-icon mat-card-avatar>email</mat-icon>
            <mat-card-title>Email Configuration</mat-card-title>
            <mat-card-subtitle>SMTP settings for automated notifications</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>SMTP Host</mat-label>
              <input matInput [(ngModel)]="settings.smtpHost" placeholder="smtp.gmail.com">
            </mat-form-field>

            <div class="row">
              <mat-form-field appearance="outline" class="half-width">
                <mat-label>SMTP Port</mat-label>
                <input matInput [(ngModel)]="settings.smtpPort" type="number" placeholder="587">
              </mat-form-field>

              <mat-form-field appearance="outline" class="half-width">
                <mat-label>From Address</mat-label>
                <input matInput [(ngModel)]="settings.fromAddress" placeholder="rfpcopilot@company.com">
              </mat-form-field>
            </div>

            <div class="info-box">
              <mat-icon>info</mat-icon>
              <span>Email settings are configured in the backend appsettings.json. In development mode, emails are logged to console instead of being sent.</span>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-icon mat-card-avatar>info</mat-icon>
            <mat-card-title>About RFP Copilot</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <mat-list>
              <mat-list-item>
                <mat-icon matListItemIcon>architecture</mat-icon>
                <span matListItemTitle>Architecture</span>
                <span matListItemLine>Multi-Agent Orchestration with Semantic Kernel</span>
              </mat-list-item>
              <mat-list-item>
                <mat-icon matListItemIcon>code</mat-icon>
                <span matListItemTitle>Backend</span>
                <span matListItemLine>.NET 8 ASP.NET Core Web API</span>
              </mat-list-item>
              <mat-list-item>
                <mat-icon matListItemIcon>web</mat-icon>
                <span matListItemTitle>Frontend</span>
                <span matListItemLine>Angular 17 with Angular Material</span>
              </mat-list-item>
              <mat-list-item>
                <mat-icon matListItemIcon>smart_toy</mat-icon>
                <span matListItemTitle>AI Agents</span>
                <span matListItemLine>8 specialized agents (Tracker, Solution, Estimation, Cloud, Integration, Testing/DevOps, Staffing, Risks)</span>
              </mat-list-item>
              <mat-list-item>
                <mat-icon matListItemIcon>storage</mat-icon>
                <span matListItemTitle>Database</span>
                <span matListItemLine>SQL Server / SQLite with Entity Framework Core</span>
              </mat-list-item>
            </mat-list>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .settings-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
    .full-width { width: 100%; }
    .half-width { width: 48%; }
    .row { display: flex; gap: 4%; }
    .section-title { font-size: 14px; font-weight: 500; color: #666; margin: 16px 0 12px; }
    .info-box {
      display: flex; align-items: flex-start; gap: 8px; padding: 12px;
      background: #fff3e0; border-radius: 8px; margin-top: 16px;
      font-size: 13px; color: #e65100;
    }
    .info-box mat-icon { font-size: 18px; height: 18px; width: 18px; flex-shrink: 0; margin-top: 2px; }
    @media (max-width: 768px) { .settings-grid { grid-template-columns: 1fr; } }
  `]
})
export class SettingsComponent {
  settings = {
    defaultOriginatorEmail: 'abc_xyz@gmail.com',
    defaultCloudMigrationInScope: false,
    defaultCloudProvider: 'Azure',
    aiEndpoint: '',
    aiDeploymentName: '',
    aiApiKey: '',
    smtpHost: 'localhost',
    smtpPort: 587,
    fromAddress: 'rfpcopilot@company.com'
  };

  constructor(private snackBar: MatSnackBar) {
    this.loadSettings();
  }

  loadSettings(): void {
    const saved = localStorage.getItem('rfp-copilot-settings');
    if (saved) {
      this.settings = { ...this.settings, ...JSON.parse(saved) };
    }
  }

  saveSettings(): void {
    localStorage.setItem('rfp-copilot-settings', JSON.stringify(this.settings));
    this.snackBar.open('Settings saved successfully', 'OK', { duration: 3000 });
  }
}
