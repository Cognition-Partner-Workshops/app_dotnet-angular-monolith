import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { OfflineService } from '../../services/offline.service';
import { UserDto, OfflineReel } from '../../models/interfaces';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="profile-container">
      <div class="profile-header">
        <div class="profile-avatar">
          {{ getInitials(user?.displayName || '') }}
        </div>
        <h2>{{ user?.displayName }}</h2>
        <p class="username">&#64;{{ user?.username }}</p>
        <p class="email">{{ user?.email }}</p>
      </div>

      <div class="profile-stats">
        <div class="stat">
          <span class="stat-value">{{ offlineReels.length }}</span>
          <span class="stat-label">Downloaded Reels</span>
        </div>
        <div class="stat">
          <span class="stat-value" [class.online]="isOnline">{{ isOnline ? 'Online' : 'Offline' }}</span>
          <span class="stat-label">Connection</span>
        </div>
      </div>

      <div class="profile-section">
        <h3>Offline Storage</h3>
        <div class="storage-info">
          <div class="storage-bar">
            <div class="storage-used" [style.width.%]="storagePercent"></div>
          </div>
          <p class="storage-text">{{ formatBytes(totalStorageUsed) }} used</p>
        </div>
      </div>

      <div class="profile-section">
        <h3>Calling</h3>
        <div class="calling-info">
          <div class="calling-feature">
            <span class="feature-icon">&#128222;</span>
            <div class="feature-text">
              <strong>Works on WiFi</strong>
              <p>When you and your friend are on the same WiFi (like train WiFi),
                calls connect directly between your phones - no internet needed!</p>
            </div>
          </div>
          <div class="calling-feature">
            <span class="feature-icon">&#128274;</span>
            <div class="feature-text">
              <strong>Peer-to-Peer</strong>
              <p>Audio and video flow directly between devices for the best quality
                and privacy. No data goes through external servers.</p>
            </div>
          </div>
          <div class="calling-steps">
            <h4>How to call:</h4>
            <ol>
              <li>Both users connect to the same WiFi</li>
              <li>Both open the app and sign in</li>
              <li>Go to Calls tab and tap the call button</li>
              <li>Accept the call on the other phone - done!</li>
            </ol>
          </div>
        </div>
      </div>

      <div class="profile-section">
        <h3>Security</h3>
        <div class="security-info">
          <div class="security-item">
            <span>End-to-end encryption</span>
            <span class="badge active">Active</span>
          </div>
          <div class="security-item">
            <span>JWT Authentication</span>
            <span class="badge active">Active</span>
          </div>
          <div class="security-item">
            <span>Rate Limiting</span>
            <span class="badge active">Active</span>
          </div>
          <div class="security-item">
            <span>CSP Headers</span>
            <span class="badge active">Active</span>
          </div>
        </div>
      </div>

      <button class="btn-logout" (click)="logout()">Sign Out</button>
    </div>
  `,
  styles: [`
    .profile-container { max-width: 480px; margin: 0 auto; min-height: 100vh; background: #0a0a1a; color: white; padding: 20px; padding-bottom: 100px; }
    .profile-header { text-align: center; padding: 30px 0; }
    .profile-avatar {
      width: 100px; height: 100px; border-radius: 50%; margin: 0 auto 16px;
      background: linear-gradient(135deg, #00d2ff, #3a7bd5);
      display: flex; align-items: center; justify-content: center;
      font-size: 2.2em; font-weight: 600;
    }
    .profile-header h2 { margin: 0 0 4px; font-size: 1.5em; }
    .username { margin: 0 0 4px; color: rgba(255,255,255,0.5); font-size: 0.9em; }
    .email { margin: 0; color: rgba(255,255,255,0.4); font-size: 0.85em; }
    .profile-stats {
      display: flex; gap: 16px; padding: 20px 0;
      border-top: 1px solid rgba(255,255,255,0.08);
      border-bottom: 1px solid rgba(255,255,255,0.08);
    }
    .stat {
      flex: 1; text-align: center; padding: 12px;
      background: rgba(255,255,255,0.04); border-radius: 12px;
    }
    .stat-value { display: block; font-size: 1.4em; font-weight: 600; margin-bottom: 4px; }
    .stat-value.online { color: #00e676; }
    .stat-label { font-size: 0.75em; color: rgba(255,255,255,0.4); }
    .profile-section { padding: 20px 0; border-bottom: 1px solid rgba(255,255,255,0.08); }
    .profile-section h3 { margin: 0 0 12px; font-size: 1em; color: rgba(255,255,255,0.7); }
    .storage-bar {
      height: 6px; background: rgba(255,255,255,0.1); border-radius: 3px; overflow: hidden;
    }
    .storage-used {
      height: 100%; background: linear-gradient(90deg, #00d2ff, #3a7bd5); border-radius: 3px;
      transition: width 0.3s;
    }
    .storage-text { margin: 8px 0 0; font-size: 0.8em; color: rgba(255,255,255,0.4); }
    .security-info { display: flex; flex-direction: column; gap: 10px; }
    .security-item {
      display: flex; justify-content: space-between; align-items: center;
      padding: 10px 14px; background: rgba(255,255,255,0.04); border-radius: 10px;
      font-size: 0.9em;
    }
    .badge {
      padding: 3px 10px; border-radius: 12px; font-size: 0.75em; font-weight: 500;
    }
    .badge.active { background: rgba(0,230,118,0.15); color: #00e676; }
    .btn-logout {
      width: 100%; padding: 14px; border: 1px solid #ff3b30; border-radius: 10px;
      background: transparent; color: #ff3b30; font-size: 1em; cursor: pointer;
      margin-top: 20px; transition: background 0.3s;
    }
    .btn-logout:hover { background: rgba(255,59,48,0.1); }
    .calling-info { display: flex; flex-direction: column; gap: 14px; }
    .calling-feature {
      display: flex; gap: 12px; padding: 12px; background: rgba(255,255,255,0.04);
      border-radius: 10px; border: 1px solid rgba(255,255,255,0.08);
    }
    .feature-icon { font-size: 1.5em; flex-shrink: 0; }
    .feature-text strong { font-size: 0.9em; display: block; margin-bottom: 4px; }
    .feature-text p { font-size: 0.8em; color: rgba(255,255,255,0.5); margin: 0; line-height: 1.4; }
    .calling-steps {
      padding: 12px; background: rgba(0,210,255,0.06); border-radius: 10px;
      border: 1px solid rgba(0,210,255,0.15);
    }
    .calling-steps h4 { font-size: 0.85em; margin: 0 0 8px; color: #00d2ff; }
    .calling-steps ol { margin: 0; padding-left: 20px; font-size: 0.8em; color: rgba(255,255,255,0.6); line-height: 1.6; }
  `]
})
export class ProfileComponent implements OnInit {
  user: UserDto | null = null;
  offlineReels: OfflineReel[] = [];
  isOnline = true;
  totalStorageUsed = 0;
  storagePercent = 0;

  constructor(
    private authService: AuthService,
    private offlineService: OfflineService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => this.user = user);
    this.offlineService.isOnline$.subscribe(online => this.isOnline = online);
    this.offlineService.offlineReels$.subscribe(reels => {
      this.offlineReels = reels;
      this.totalStorageUsed = reels.reduce((acc, r) => acc + (r.blob?.size || 0), 0);
      this.storagePercent = Math.min(100, (this.totalStorageUsed / (500 * 1024 * 1024)) * 100);
    });
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
