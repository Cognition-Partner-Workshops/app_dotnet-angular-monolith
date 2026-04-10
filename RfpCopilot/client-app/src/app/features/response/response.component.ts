import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { RfpApiService } from '../../shared/services/rfp-api.service';
import { RfpResponse, RfpResponseSection } from '../../shared/models/rfp-response.model';
import { saveAs } from 'file-saver';
import { marked } from 'marked';

@Component({
  selector: 'app-response',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule, MatTabsModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSnackBarModule, MatTooltipModule, MatChipsModule
  ],
  template: `
    <div class="container">
      <div class="page-header">
        <h1>RFP Response</h1>
        <p *ngIf="response">{{ response.clientName }} - {{ response.fileName }}</p>
        <div class="header-actions" *ngIf="response">
          <span [class]="'status-chip ' + getStatusClass(response.status)">{{ response.status }}</span>
          <button mat-raised-button color="primary" (click)="downloadDocx()" matTooltip="Download as Word document">
            <mat-icon>description</mat-icon> Download DOCX
          </button>
          <button mat-raised-button color="accent" (click)="downloadPdf()" matTooltip="Download as PDF">
            <mat-icon>picture_as_pdf</mat-icon> Download PDF
          </button>
        </div>
      </div>

      <div *ngIf="loading" class="loading-container">
        <mat-spinner diameter="40"></mat-spinner>
        <p>Loading response...</p>
      </div>

      <mat-card *ngIf="!loading && response">
        <mat-tab-group animationDuration="200ms" [dynamicHeight]="true">
          <mat-tab *ngFor="let section of response.sections">
            <ng-template mat-tab-label>
              <mat-icon class="tab-icon">{{ getSectionIcon(section.sectionNumber) }}</mat-icon>
              {{ section.sectionTitle }}
              <span [class]="'tab-status ' + section.status.toLowerCase()">
                {{ section.status === 'Completed' ? '' : ' (' + section.status + ')' }}
              </span>
            </ng-template>

            <div class="section-content">
              <div class="section-toolbar">
                <div class="section-info">
                  <span class="generated-at">Generated: {{ section.generatedAt | date:'medium' }}</span>
                  <span *ngIf="section.regeneratedAt" class="regenerated-at">
                    Last regenerated: {{ section.regeneratedAt | date:'medium' }}
                  </span>
                </div>
                <button mat-stroked-button color="primary" (click)="regenerateSection(section)"
                        [disabled]="section.status === 'InProgress'"
                        matTooltip="Re-generate this section using AI">
                  <mat-icon>refresh</mat-icon> Regenerate Section
                </button>
              </div>

              <div class="markdown-content" [innerHTML]="renderMarkdown(section.content)"></div>
            </div>
          </mat-tab>
        </mat-tab-group>
      </mat-card>

      <div *ngIf="!loading && !response" class="empty-state">
        <mat-icon>find_in_page</mat-icon>
        <p>No response found for this RFP</p>
      </div>
    </div>
  `,
  styles: [`
    .header-actions { display: flex; align-items: center; gap: 12px; margin-top: 12px; }
    .loading-container { display: flex; flex-direction: column; align-items: center; padding: 48px; }
    .section-content { padding: 24px; }
    .section-toolbar {
      display: flex; justify-content: space-between; align-items: center;
      margin-bottom: 16px; padding-bottom: 12px; border-bottom: 1px solid #e0e0e0;
    }
    .section-info { display: flex; flex-direction: column; gap: 4px; }
    .generated-at, .regenerated-at { font-size: 12px; color: #999; }
    .tab-icon { margin-right: 6px; font-size: 18px; height: 18px; width: 18px; }
    .tab-status.failed { color: #f44336; }
    .tab-status.pending { color: #ff9800; }
    .markdown-content {
      line-height: 1.7; font-size: 14px;
      :host ::ng-deep {
        h1 { font-size: 24px; color: #1a237e; border-bottom: 2px solid #e8eaf6; padding-bottom: 8px; }
        h2 { font-size: 20px; color: #283593; margin-top: 24px; }
        h3 { font-size: 16px; color: #3949ab; }
        table { width: 100%; border-collapse: collapse; margin: 16px 0; }
        th { background: #e8eaf6; padding: 10px; text-align: left; border: 1px solid #c5cae9; font-weight: 600; }
        td { padding: 8px 10px; border: 1px solid #e0e0e0; }
        tr:nth-child(even) { background: #fafafa; }
        code { background: #f5f5f5; padding: 2px 6px; border-radius: 4px; font-size: 13px; }
        pre { background: #263238; color: #e0e0e0; padding: 16px; border-radius: 8px; overflow-x: auto; }
        pre code { background: none; color: inherit; }
        blockquote { border-left: 4px solid #3f51b5; margin: 16px 0; padding: 12px 16px; background: #e8eaf6; }
        ul, ol { padding-left: 24px; }
        li { margin-bottom: 4px; }
      }
    }
    .empty-state { text-align: center; padding: 48px; color: #999; }
    .empty-state mat-icon { font-size: 64px; height: 64px; width: 64px; }
  `]
})
export class ResponseComponent implements OnInit {
  rfpId!: number;
  response: RfpResponse | null = null;
  loading = true;

  constructor(
    private route: ActivatedRoute,
    private rfpApi: RfpApiService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.rfpId = Number(this.route.snapshot.paramMap.get('rfpId'));
    this.loadResponse();
  }

  loadResponse(): void {
    this.loading = true;
    this.rfpApi.getResponse(this.rfpId).subscribe({
      next: (resp) => {
        this.response = resp;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackBar.open('Failed to load response', 'Close', { duration: 5000 });
      }
    });
  }

  renderMarkdown(content: string): string {
    try {
      return marked.parse(content, { async: false }) as string;
    } catch {
      return content;
    }
  }

  getSectionIcon(sectionNumber: number): string {
    const icons: Record<number, string> = {
      0: 'assignment', 1: 'lightbulb', 2: 'calculate', 3: 'cloud',
      4: 'hub', 5: 'build', 6: 'groups', 7: 'warning'
    };
    return icons[sectionNumber] || 'article';
  }

  regenerateSection(section: RfpResponseSection): void {
    this.rfpApi.regenerateSection(this.rfpId, section.sectionNumber).subscribe({
      next: () => {
        this.snackBar.open(`Regenerating "${section.sectionTitle}"...`, 'OK', { duration: 3000 });
        setTimeout(() => this.loadResponse(), 5000);
      },
      error: () => this.snackBar.open('Regeneration failed', 'Close', { duration: 5000 })
    });
  }

  downloadDocx(): void {
    this.rfpApi.downloadDocx(this.rfpId).subscribe({
      next: (blob) => saveAs(blob, `rfp-response-${this.rfpId}.docx`),
      error: () => this.snackBar.open('Download failed', 'Close', { duration: 5000 })
    });
  }

  downloadPdf(): void {
    this.rfpApi.downloadPdf(this.rfpId).subscribe({
      next: (blob) => saveAs(blob, `rfp-response-${this.rfpId}.pdf`),
      error: () => this.snackBar.open('Download failed', 'Close', { duration: 5000 })
    });
  }

  getStatusClass(status: string): string {
    return status.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z-]/g, '');
  }
}
