import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import { AuthService } from './services/auth.service';
import { OfflineService } from './services/offline.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  template: `
    <!-- Offline Status Banner -->
    <div *ngIf="!isOnline" class="offline-status-bar">
      <span>No internet connection - Offline Mode</span>
    </div>

    <router-outlet></router-outlet>

    <!-- Bottom Navigation (only when authenticated) -->
    <nav *ngIf="isAuthenticated" class="bottom-nav">
      <a routerLink="/reels" class="nav-item" [class.active]="isActive('/reels')">
        <span class="nav-icon">&#127910;</span>
        <span class="nav-label">Reels</span>
      </a>
      <a routerLink="/calls" class="nav-item" [class.active]="isActive('/calls')">
        <span class="nav-icon">&#128222;</span>
        <span class="nav-label">Calls</span>
      </a>
      <a routerLink="/profile" class="nav-item" [class.active]="isActive('/profile')">
        <span class="nav-icon">&#128100;</span>
        <span class="nav-label">Profile</span>
      </a>
    </nav>
  `,
  styles: [`
    :host { display: block; }
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
export class AppComponent implements OnInit {
  title = 'TrainConnect';
  isOnline = true;
  isAuthenticated = false;

  constructor(
    private authService: AuthService,
    private offlineService: OfflineService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.offlineService.isOnline$.subscribe(online => this.isOnline = online);
    this.authService.currentUser$.subscribe(user => this.isAuthenticated = !!user);
  }

  isActive(path: string): boolean {
    return this.router.url.startsWith(path);
  }
}
