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

      <!-- Global Active Call Overlay - shows on any page when call is active -->
      <div *ngIf="activeCallState !== 'idle' && !incomingCall" class="call-overlay active-call">
        <div class="call-screen">
          <video #globalRemoteVideo *ngIf="activeCallType === 'Video'" class="global-remote-video"
                 autoplay playsinline></video>
          <video #globalRelayVideo *ngIf="activeCallType === 'Video' && globalRemoteVideoUrl" class="global-remote-video global-relay-video"
                 autoplay playsinline></video>
          <video #globalLocalVideo *ngIf="activeCallType === 'Video' && globalLocalStreamActive" class="global-local-video"
                 autoplay playsinline muted></video>

          <div class="call-info-center" [class.video-mode]="activeCallType === 'Video'">
            <div *ngIf="activeCallType !== 'Video'" class="calling-avatar" [class.connected]="activeCallState === 'connected'">
              {{ getInitials(activeCallName) }}
            </div>
            <h2>{{ activeCallName }}</h2>
            <p class="call-state-text">{{ getActiveCallStateText() }}</p>
            <p *ngIf="activeCallState === 'connected'" class="call-timer-text">{{ callDurationDisplay }}</p>
          </div>

          <div class="call-controls">
            <button class="control-btn" [class.active]="isMuted" (click)="toggleMute()">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path *ngIf="!isMuted" d="M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm5-3c0 2.76-2.24 5-5 5s-5-2.24-5-5H5c0 3.53 2.61 6.43 6 6.92V21h2v-3.08c3.39-.49 6-3.39 6-6.92h-2z"/>
                <path *ngIf="isMuted" d="M19 11h-1.7c0 .74-.16 1.43-.43 2.05l1.23 1.23c.56-.98.9-2.09.9-3.28zm-4.02.17c0-.06.02-.11.02-.17V5c0-1.66-1.34-3-3-3S9 3.34 9 5v.18l5.98 5.99zM4.27 3L3 4.27l6.01 6.01V11c0 1.66 1.33 3 2.99 3 .22 0 .44-.03.65-.08l1.66 1.66c-.71.33-1.5.52-2.31.52-2.76 0-5.3-2.1-5.3-5.1H5c0 3.41 2.72 6.23 6 6.72V21h2v-3.28c.91-.13 1.77-.45 2.54-.9L19.73 21 21 19.73 4.27 3z"/>
              </svg>
            </button>
            <button class="control-btn end" (click)="hangUp()">
              <svg width="28" height="28" viewBox="0 0 24 24" fill="white"><path d="M12 9c-1.6 0-3.15.25-4.6.72v3.1c0 .39-.23.74-.56.9-.98.49-1.87 1.12-2.66 1.85-.18.18-.43.28-.7.28-.28 0-.53-.11-.71-.29L.29 13.08a.956.956 0 010-1.36C3.49 8.82 7.51 7 12 7s8.51 1.82 11.71 4.72c.18.18.29.44.29.71 0 .28-.11.53-.29.71l-2.48 2.48c-.18.18-.43.29-.71.29-.27 0-.52-.1-.7-.28-.79-.73-1.68-1.36-2.66-1.85a.994.994 0 01-.56-.9v-3.1C15.15 9.25 13.6 9 12 9z"/></svg>
            </button>
            <button *ngIf="activeCallType === 'Video'" class="control-btn" [class.active]="isVideoOff" (click)="toggleVideo()">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path *ngIf="!isVideoOff" d="M17 10.5V7a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h12a1 1 0 001-1v-3.5l4 4v-11l-4 4z"/>
                <path *ngIf="isVideoOff" d="M21 6.5l-4 4V7c0-.55-.45-1-1-1H9.82L21 17.18V6.5zM3.27 2L2 3.27 4.73 6H4c-.55 0-1 .45-1 1v10c0 .55.45 1 1 1h12c.21 0 .39-.08.54-.18L19.73 21 21 19.73 3.27 2z"/>
              </svg>
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
    .control-btn.end { background: #ff3b30; }
    .control-btn.active { background: rgba(255,100,100,0.4); }

    /* Active call overlay */
    .active-call .call-screen {
      width: 100%; height: 100%; position: relative;
      display: flex; flex-direction: column; align-items: center; justify-content: center;
    }
    .global-remote-video {
      position: absolute; top: 0; left: 0; width: 100%; height: 100%;
      object-fit: cover; background: #000;
    }
    .global-relay-video { z-index: 1; }
    .global-local-video {
      position: absolute; top: 20px; right: 20px; width: 120px; height: 160px;
      object-fit: cover; border-radius: 12px; border: 2px solid rgba(255,255,255,0.3);
      z-index: 102; background: #333;
    }
    .call-info-center { position: relative; z-index: 101; }
    .call-info-center.video-mode {
      position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);
      background: rgba(0,0,0,0.5); padding: 30px 50px; border-radius: 20px;
    }
    .calling-avatar.connected { border: 3px solid #00e676; }
    .call-timer-text { color: #00e676; font-size: 1.1em; font-weight: 500; margin: 4px 0 0; }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'TrainConnect';
  isOnline = true;
  isAuthenticated = false;
  incomingCall: IncomingCall | null = null;
  activeCallState: WebRTCCallState = 'idle';
  activeCallName = '';
  activeCallType = 'Audio';
  globalLocalStreamActive = false;
  globalRemoteVideoUrl: string | null = null;
  isMuted = false;
  isVideoOff = false;
  callDurationDisplay = '0:00';
  private callTimer: ReturnType<typeof setInterval> | null = null;
  private callStartTime = 0;
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
      }),
      this.webrtcService.activeCallName$.subscribe(name => {
        this.zone.run(() => { this.activeCallName = name; });
      }),
      this.webrtcService.activeCallType$.subscribe(type => {
        this.zone.run(() => { this.activeCallType = type; });
      }),
      this.webrtcService.callState$.subscribe(state => {
        this.zone.run(() => {
          this.activeCallState = state;
          if (state === 'connected') {
            this.startCallTimer();
          } else if (state === 'ended' || state === 'failed') {
            this.stopCallTimer();
            setTimeout(() => {
              this.zone.run(() => {
                this.activeCallState = 'idle';
                this.activeCallName = '';
                this.globalLocalStreamActive = false;
                this.globalRemoteVideoUrl = null;
                this.isMuted = false;
                this.isVideoOff = false;
              });
            }, 1500);
          }
        });
      }),
      this.webrtcService.localStream$.subscribe(stream => {
        this.zone.run(() => {
          this.globalLocalStreamActive = !!stream;
          this.attachGlobalStreams();
        });
      }),
      this.webrtcService.remoteStream$.subscribe(stream => {
        this.zone.run(() => {
          if (stream) this.attachGlobalStreams();
        });
      }),
      this.webrtcService.remoteVideoUrl$.subscribe(url => {
        this.zone.run(() => {
          this.globalRemoteVideoUrl = url;
          if (url) {
            setTimeout(() => this.attachGlobalStreams(), 50);
          }
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
    this.activeCallName = call.callerName;
    this.activeCallType = call.callType;
    this.incomingCall = null;
    // Accept call (global overlay will show the call UI)
    await this.webrtcService.acceptCall(call.callerId, call.offer, call.callerName);
  }

  async declineIncomingCall(): Promise<void> {
    if (!this.incomingCall) return;
    await this.webrtcService.declineCall(this.incomingCall.callerId);
    this.incomingCall = null;
  }

  async hangUp(): Promise<void> {
    await this.webrtcService.hangUp();
  }

  toggleMute(): void {
    this.isMuted = this.webrtcService.toggleMute();
  }

  toggleVideo(): void {
    this.isVideoOff = this.webrtcService.toggleVideo();
  }

  getActiveCallStateText(): string {
    switch (this.activeCallState) {
      case 'calling': return 'Calling...';
      case 'ringing': return 'Ringing...';
      case 'connected': return 'Connected';
      case 'ended': return 'Call Ended';
      case 'failed': return 'Call Failed';
      default: return '';
    }
  }

  private startCallTimer(): void {
    this.callStartTime = Date.now();
    this.callDurationDisplay = '0:00';
    this.callTimer = setInterval(() => {
      this.zone.run(() => {
        const elapsed = Math.floor((Date.now() - this.callStartTime) / 1000);
        const mins = Math.floor(elapsed / 60);
        const secs = elapsed % 60;
        this.callDurationDisplay = mins + ':' + secs.toString().padStart(2, '0');
      });
    }, 1000);
  }

  private stopCallTimer(): void {
    if (this.callTimer) {
      clearInterval(this.callTimer);
      this.callTimer = null;
    }
  }

  private attachGlobalStreams(): void {
    // Use setTimeout to wait for Angular to render the video elements
    setTimeout(() => {
      const remoteVideoEl = document.querySelector('.global-remote-video:not(.global-relay-video)') as HTMLVideoElement;
      const localVideoEl = document.querySelector('.global-local-video') as HTMLVideoElement;
      const relayVideoEl = document.querySelector('.global-relay-video') as HTMLVideoElement;

      const remoteStream = this.webrtcService.getRemoteStream();
      const localStream = this.webrtcService.getLocalStream();

      if (remoteVideoEl && remoteStream) {
        remoteVideoEl.srcObject = remoteStream;
        remoteVideoEl.play().catch(() => {});
      }
      if (localVideoEl && localStream) {
        localVideoEl.srcObject = localStream;
        localVideoEl.play().catch(() => {});
      }
      if (relayVideoEl && this.globalRemoteVideoUrl) {
        relayVideoEl.src = this.globalRemoteVideoUrl;
        relayVideoEl.play().catch(() => {});
      }
    }, 100);
  }
}
