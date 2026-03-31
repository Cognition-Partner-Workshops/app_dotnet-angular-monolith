import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './services/auth.service';
import { OfflineService } from './services/offline.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app-wrapper">
      <div *ngIf="!isOnline" class="offline-status-bar">
        No internet connection - Offline Mode
      </div>

      <router-outlet></router-outlet>

      <nav *ngIf="isAuthenticated" class="bottom-nav">
        <a [routerLink]="['/reels']" routerLinkActive="active" class="nav-item">
          <span class="nav-icon">&#127910;</span>
          <span class="nav-label">Reels</span>
        </a>
        <a [routerLink]="['/calls']" routerLinkActive="active" class="nav-item">
          <span class="nav-icon">&#128222;</span>
          <span class="nav-label">Calls</span>
        </a>
        <a [routerLink]="['/profile']" routerLinkActive="active" class="nav-item">
          <span class="nav-icon">&#128100;</span>
          <span class="nav-label">Profile</span>
        </a>
      </nav>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .app-wrapper { min-height: 100vh; }
    .offline-status-bar {
      position: fixed; top: 0; left: 0; right: 0; z-index: 1000;
      background: #ff6b35; color: white; text-align: center;
      padding: 6px; font-size: 0.8em; font-weight: 500;
    }
    .bottom-nav {
      position: fixed; bottom: 0; left: 0; right: 0; z-index: 100;
      display: flex; justify-content: center;
      background: rgba(10, 10, 26, 0.95); backdrop-filter: blur(20px);
      border-top: 1px solid rgba(255,255,255,0.08);
      padding: 8px 0; padding-bottom: max(8px, env(safe-area-inset-bottom));
    }
    .nav-item {
      flex: 1; max-width: 160px; display: flex; flex-direction: column; align-items: center;
      gap: 4px; text-decoration: none; color: rgba(255,255,255,0.4);
      padding: 6px 0; transition: color 0.2s;
    }
    .nav-item.active { color: #00d2ff; }
    .nav-icon { font-size: 1.4em; }
    .nav-label { font-size: 0.7em; }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'TrainConnect';
  isOnline = true;
  isAuthenticated = false;
  private subs: Subscription[] = [];

  constructor(
    private authService: AuthService,
    private offlineService: OfflineService
  ) {}

  ngOnInit(): void {
    this.subs.push(
      this.offlineService.isOnline$.subscribe(online => this.isOnline = online),
      this.authService.currentUser$.subscribe(user => this.isAuthenticated = !!user)
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }
}
