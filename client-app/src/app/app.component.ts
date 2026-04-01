import { Component, OnInit, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from './services/auth.service';
import { OfflineService } from './services/offline.service';
import { WebRTCService, IncomingCall, WebRTCCallState } from './services/webrtc.service';
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

      <!-- Global Incoming Call Overlay - shows on any page -->
      <div *ngIf="incomingCall" class="call-overlay incoming">
        <div class="call-screen">
          <div class="calling-avatar pulse-ring">
            {{ getInitials(incomingCall.callerName) }}
          </div>
          <h2>{{ incomingCall.callerName }}</h2>
          <p class="call-state-text">Incoming {{ incomingCall.callType }} Call...</p>
          <div class="call-controls">
            <button class="control-btn decline" (click)="declineIncomingCall()">
              <svg width="28" height="28" viewBox="0 0 24 24" fill="white"><path d="M12 9c-1.6 0-3.15.25-4.6.72v3.1c0 .39-.23.74-.56.9-.98.49-1.87 1.12-2.66 1.85-.18.18-.43.28-.7.28-.28 0-.53-.11-.71-.29L.29 13.08a.956.956 0 010-1.36C3.49 8.82 7.51 7 12 7s8.51 1.82 11.71 4.72c.18.18.29.44.29.71 0 .28-.11.53-.29.71l-2.48 2.48c-.18.18-.43.29-.71.29-.27 0-.52-.1-.7-.28-.79-.73-1.68-1.36-2.66-1.85a.994.994 0 01-.56-.9v-3.1C15.15 9.25 13.6 9 12 9z"/></svg>
            </button>
            <button class="control-btn accept" (click)="acceptIncomingCall()">
              <svg width="28" height="28" viewBox="0 0 24 24" fill="white"><path d="M6.62 10.79a15.053 15.053 0 006.59 6.59l2.2-2.2a1 1 0 011.01-.24c1.12.37 2.33.57 3.58.57a1 1 0 011 1V20a1 1 0 01-1 1A17 17 0 013 4a1 1 0 011-1h3.5a1 1 0 011 1c0 1.25.2 2.46.57 3.58a1 1 0 01-.24 1.01l-2.21 2.2z"/></svg>
            </button>
          </div>
        </div>
      </div>

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

    /* Global incoming call overlay */
    .call-overlay {
      position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 2000;
      background: linear-gradient(180deg, #1a1a3e 0%, #0a0a1a 100%);
      display: flex; align-items: center; justify-content: center;
    }
    .call-screen { text-align: center; color: white; }
    .calling-avatar {
      width: 100px; height: 100px; border-radius: 50%;
      background: linear-gradient(135deg, #00d2ff, #3a7bd5);
      display: flex; align-items: center; justify-content: center;
      font-size: 2em; font-weight: 600; margin: 0 auto 20px;
    }
    .pulse-ring {
      animation: pulse 1.5s ease-in-out infinite;
      box-shadow: 0 0 0 0 rgba(0,210,255,0.4);
    }
    @keyframes pulse {
      0% { box-shadow: 0 0 0 0 rgba(0,210,255,0.4); }
      70% { box-shadow: 0 0 0 20px rgba(0,210,255,0); }
      100% { box-shadow: 0 0 0 0 rgba(0,210,255,0); }
    }
    .call-state-text { color: rgba(255,255,255,0.6); margin: 8px 0 30px; }
    .call-controls { display: flex; gap: 40px; justify-content: center; }
    .control-btn {
      width: 64px; height: 64px; border-radius: 50%; border: none;
      display: flex; align-items: center; justify-content: center; cursor: pointer;
    }
    .control-btn.decline { background: #ff4444; }
    .control-btn.accept { background: #00c853; }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'TrainConnect';
  isOnline = true;
  isAuthenticated = false;
  incomingCall: IncomingCall | null = null;
  private subs: Subscription[] = [];

  constructor(
    private authService: AuthService,
    private offlineService: OfflineService,
    private webrtcService: WebRTCService,
    private router: Router,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.subs.push(
      this.offlineService.isOnline$.subscribe(online => this.isOnline = online),
      this.authService.currentUser$.subscribe(user => {
        this.isAuthenticated = !!user;
        if (user) {
          this.webrtcService.connect();
        } else {
          this.webrtcService.disconnect();
        }
      }),
      this.webrtcService.incomingCall$.subscribe(call => {
        this.zone.run(() => {
          this.incomingCall = call;
        });
      }),
      this.webrtcService.callEnded$.subscribe(() => {
        this.zone.run(() => {
          this.incomingCall = null;
        });
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.forEach(s => s.unsubscribe());
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  }

  async acceptIncomingCall(): Promise<void> {
    if (!this.incomingCall) return;
    const call = this.incomingCall;
    this.incomingCall = null;
    // Navigate to calls page then accept
    await this.router.navigate(['/calls']);
    await this.webrtcService.acceptCall(call.callerId, call.offer);
  }

  async declineIncomingCall(): Promise<void> {
    if (!this.incomingCall) return;
    await this.webrtcService.declineCall(this.incomingCall.callerId);
    this.incomingCall = null;
  }
}
