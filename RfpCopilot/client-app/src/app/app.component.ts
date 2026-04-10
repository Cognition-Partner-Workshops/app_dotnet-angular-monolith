import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatButtonModule, MatIconModule, MatSidenavModule, MatListModule
  ],
  template: `
    <div class="app-layout">
      <mat-toolbar color="primary" class="app-toolbar">
        <mat-icon class="logo-icon">smart_toy</mat-icon>
        <span class="app-title">RFP Copilot</span>
        <span class="spacer"></span>
        <nav class="nav-links">
          <a mat-button routerLink="/upload" routerLinkActive="active-link">
            <mat-icon>cloud_upload</mat-icon> Upload
          </a>
          <a mat-button routerLink="/tracker" routerLinkActive="active-link">
            <mat-icon>table_chart</mat-icon> Tracker
          </a>
          <a mat-button routerLink="/settings" routerLinkActive="active-link">
            <mat-icon>settings</mat-icon> Settings
          </a>
        </nav>
      </mat-toolbar>
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-layout { display: flex; flex-direction: column; height: 100vh; }
    .app-toolbar {
      position: sticky; top: 0; z-index: 100;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .logo-icon { margin-right: 8px; font-size: 28px; height: 28px; width: 28px; }
    .app-title { font-size: 20px; font-weight: 500; }
    .spacer { flex: 1 1 auto; }
    .nav-links a { color: white; margin-left: 8px; }
    .nav-links .active-link { background: rgba(255,255,255,0.15); border-radius: 4px; }
    .nav-links mat-icon { margin-right: 4px; font-size: 20px; height: 20px; width: 20px; vertical-align: middle; }
    .main-content { flex: 1; overflow-y: auto; background: #f5f5f5; }
  `]
})
export class AppComponent {
  title = 'RFP Copilot';
}
