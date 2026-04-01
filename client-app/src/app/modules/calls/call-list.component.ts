import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CallService } from '../../services/call.service';
import { OfflineService } from '../../services/offline.service';
import { AuthService } from '../../services/auth.service';
import { WebRTCService, IncomingCall, WebRTCCallState } from '../../services/webrtc.service';
import { ContactDto, CallLogDto } from '../../models/interfaces';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-call-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="calls-container">
      <!-- Offline Banner -->
      <div *ngIf="!isOnline" class="offline-banner">
        You're offline. Calls will be queued and placed when you're back online.
      </div>

      <!-- Tab Navigation -->
      <div class="tab-nav">
        <button [class.active]="activeTab === 'contacts'" (click)="activeTab = 'contacts'">Contacts</button>
        <button [class.active]="activeTab === 'history'" (click)="activeTab = 'history'; loadHistory()">History</button>
      </div>

      <!-- Contacts Tab -->
      <div *ngIf="activeTab === 'contacts'" class="tab-content">
        <div class="add-contact">
          <input type="text" [(ngModel)]="newContactUsername" placeholder="Add contact by username"
                 (keyup.enter)="addContact()">
          <button (click)="addContact()" [disabled]="!newContactUsername">+</button>
        </div>
        <div *ngIf="addContactError" class="error-msg">{{ addContactError }}</div>

        <div *ngIf="contacts.length === 0" class="empty-state">
          <p>No contacts yet. Add friends to start calling!</p>
        </div>

        <div *ngFor="let contact of contacts" class="contact-card">
          <div class="contact-avatar">
            <div class="avatar-circle" [class.online]="contact.isOnline">
              {{ getInitials(contact.displayName || contact.username) }}
            </div>
          </div>
          <div class="contact-info">
            <h3>{{ contact.displayName || contact.username }}</h3>
            <p class="status" [class.online]="contact.isOnline">
              {{ contact.isOnline ? 'Online' : 'Last seen ' + formatTime(contact.lastSeen) }}
            </p>
          </div>
          <div class="contact-actions">
            <button class="call-btn audio" (click)="makeCall(contact, 'Audio')" title="Audio Call">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d="M6.62 10.79a15.053 15.053 0 006.59 6.59l2.2-2.2a1 1 0 011.01-.24c1.12.37 2.33.57 3.58.57a1 1 0 011 1V20a1 1 0 01-1 1A17 17 0 013 4a1 1 0 011-1h3.5a1 1 0 011 1c0 1.25.2 2.46.57 3.58a1 1 0 01-.24 1.01l-2.21 2.2z"/></svg>
            </button>
            <button class="call-btn video" (click)="makeCall(contact, 'Video')" title="Video Call">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d="M17 10.5V7a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h12a1 1 0 001-1v-3.5l4 4v-11l-4 4z"/></svg>
            </button>
          </div>
        </div>
      </div>

      <!-- History Tab -->
      <div *ngIf="activeTab === 'history'" class="tab-content">
        <div *ngIf="callHistory.length === 0" class="empty-state">
          <p>No call history yet.</p>
        </div>

        <div *ngFor="let call of callHistory" class="history-card">
          <div class="call-icon" [ngClass]="getCallStatusClass(call)">
            {{ call.callType === 'Video' ? '&#128249;' : '&#128222;' }}
          </div>
          <div class="call-details">
            <h3>{{ getCallPartyName(call) }}</h3>
            <p class="call-meta">
              <span class="call-status" [ngClass]="getCallStatusClass(call)">{{ call.status }}</span>
              &middot; {{ formatTime(call.startedAt) }}
            </p>
          </div>
          <div class="call-duration" *ngIf="call.durationSeconds > 0">
            {{ formatDuration(call.durationSeconds) }}
          </div>
        </div>
      </div>

      <!-- Incoming Call Overlay -->
      <div *ngIf="incomingCall" class="call-overlay incoming">
        <div class="call-screen">
          <div class="calling-avatar pulse-ring">
            {{ getInitials(incomingCall.callerName) }}
          </div>
          <h2>{{ incomingCall.callerName }}</h2>
          <p class="call-state">Incoming {{ incomingCall.callType }} Call...</p>
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

      <!-- Active Call Overlay -->
      <div *ngIf="webrtcCallState !== 'idle' && !incomingCall" class="call-overlay">
        <div class="call-screen">
          <video #remoteVideo *ngIf="currentCallType === 'Video'" class="remote-video"
                 autoplay playsinline></video>
          <video #localVideo *ngIf="currentCallType === 'Video' && localStreamActive" class="local-video"
                 autoplay playsinline muted></video>

          <div class="call-info-overlay" [class.video-mode]="currentCallType === 'Video'">
            <div *ngIf="currentCallType !== 'Video'" class="calling-avatar" [class.connected]="webrtcCallState === 'connected'">
              {{ getInitials(activeCallContact?.displayName || activeCallContact?.username || '') }}
            </div>
            <h2>{{ activeCallContact?.displayName || activeCallContact?.username }}</h2>
            <p class="call-state">{{ getCallStateText() }}</p>
            <p *ngIf="webrtcCallState === 'connected'" class="call-timer">{{ callDurationDisplay }}</p>
          </div>

          <div class="call-controls">
            <button class="control-btn" [class.active]="isMuted" (click)="toggleMute()" title="Mute">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path *ngIf="!isMuted" d="M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm5-3c0 2.76-2.24 5-5 5s-5-2.24-5-5H5c0 3.53 2.61 6.43 6 6.92V21h2v-3.08c3.39-.49 6-3.39 6-6.92h-2z"/>
                <path *ngIf="isMuted" d="M19 11h-1.7c0 .74-.16 1.43-.43 2.05l1.23 1.23c.56-.98.9-2.09.9-3.28zm-4.02.17c0-.06.02-.11.02-.17V5c0-1.66-1.34-3-3-3S9 3.34 9 5v.18l5.98 5.99zM4.27 3L3 4.27l6.01 6.01V11c0 1.66 1.33 3 2.99 3 .22 0 .44-.03.65-.08l1.66 1.66c-.71.33-1.5.52-2.31.52-2.76 0-5.3-2.1-5.3-5.1H5c0 3.41 2.72 6.23 6 6.72V21h2v-3.28c.91-.13 1.77-.45 2.54-.9L19.73 21 21 19.73 4.27 3z"/>
              </svg>
            </button>
            <button class="control-btn end" (click)="hangUp()">
              <svg width="28" height="28" viewBox="0 0 24 24" fill="white"><path d="M12 9c-1.6 0-3.15.25-4.6.72v3.1c0 .39-.23.74-.56.9-.98.49-1.87 1.12-2.66 1.85-.18.18-.43.28-.7.28-.28 0-.53-.11-.71-.29L.29 13.08a.956.956 0 010-1.36C3.49 8.82 7.51 7 12 7s8.51 1.82 11.71 4.72c.18.18.29.44.29.71 0 .28-.11.53-.29.71l-2.48 2.48c-.18.18-.43.29-.71.29-.27 0-.52-.1-.7-.28-.79-.73-1.68-1.36-2.66-1.85a.994.994 0 01-.56-.9v-3.1C15.15 9.25 13.6 9 12 9z"/></svg>
            </button>
            <button *ngIf="currentCallType === 'Video'" class="control-btn" [class.active]="isVideoOff" (click)="toggleVideo()" title="Camera">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                <path *ngIf="!isVideoOff" d="M17 10.5V7a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h12a1 1 0 001-1v-3.5l4 4v-11l-4 4z"/>
                <path *ngIf="isVideoOff" d="M21 6.5l-4 4V7c0-.55-.45-1-1-1H9.82L21 17.18V6.5zM3.27 2L2 3.27 4.73 6H4c-.55 0-1 .45-1 1v10c0 .55.45 1 1 1h12c.21 0 .39-.08.54-.18L19.73 21 21 19.73 3.27 2z"/>
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .calls-container { max-width: 480px; margin: 0 auto; min-height: 100vh; background: #0a0a1a; color: white; }
    .offline-banner { padding: 10px 20px; background: #ff6b35; text-align: center; font-size: 0.85em; }
    .tab-nav {
      display: flex; padding: 0; border-bottom: 1px solid rgba(255,255,255,0.1);
      position: sticky; top: 0; background: #0a0a1a; z-index: 10;
    }
    .tab-nav button {
      flex: 1; padding: 16px; border: none; background: none; color: rgba(255,255,255,0.5);
      font-size: 1em; cursor: pointer; border-bottom: 2px solid transparent;
    }
    .tab-nav button.active { color: #00d2ff; border-bottom-color: #00d2ff; }
    .tab-content { padding: 16px; padding-bottom: 80px; }
    .add-contact {
      display: flex; gap: 8px; margin-bottom: 16px;
    }
    .add-contact input {
      flex: 1; padding: 12px 16px; border: 1px solid rgba(255,255,255,0.15);
      border-radius: 10px; background: rgba(255,255,255,0.08); color: white; font-size: 0.9em; outline: none;
    }
    .add-contact input::placeholder { color: rgba(255,255,255,0.3); }
    .add-contact button {
      width: 44px; border: none; border-radius: 10px;
      background: linear-gradient(90deg, #00d2ff, #3a7bd5); color: white;
      font-size: 1.4em; cursor: pointer;
    }
    .add-contact button:disabled { opacity: 0.3; }
    .error-msg { color: #ff6b6b; font-size: 0.85em; margin-bottom: 12px; }
    .contact-card {
      display: flex; align-items: center; padding: 14px 0;
      border-bottom: 1px solid rgba(255,255,255,0.05);
    }
    .avatar-circle {
      width: 48px; height: 48px; border-radius: 50%;
      background: linear-gradient(135deg, #302b63, #24243e);
      display: flex; align-items: center; justify-content: center;
      font-size: 1em; font-weight: 600; margin-right: 14px;
      border: 2px solid transparent;
    }
    .avatar-circle.online { border-color: #00e676; }
    .contact-info { flex: 1; }
    .contact-info h3 { margin: 0 0 4px; font-size: 1em; }
    .contact-info .status { margin: 0; font-size: 0.8em; color: rgba(255,255,255,0.4); }
    .contact-info .status.online { color: #00e676; }
    .contact-actions { display: flex; gap: 12px; }
    .call-btn {
      width: 44px; height: 44px; border-radius: 50%; border: 2px solid transparent; cursor: pointer;
      display: flex; align-items: center; justify-content: center; font-size: 1.2em;
      transition: all 0.2s;
    }
    .call-btn:hover { transform: scale(1.1); }
    .call-btn:active { transform: scale(0.95); }
    .call-btn.audio { background: rgba(0,210,255,0.2); color: #00d2ff; border-color: rgba(0,210,255,0.4); }
    .call-btn.video { background: rgba(0,230,118,0.2); color: #00e676; border-color: rgba(0,230,118,0.4); }
    .history-card {
      display: flex; align-items: center; padding: 14px 0;
      border-bottom: 1px solid rgba(255,255,255,0.05);
    }
    .call-icon { font-size: 1.4em; margin-right: 14px; width: 40px; text-align: center; }
    .call-icon.missed, .call-icon.declined { filter: hue-rotate(330deg); }
    .call-details { flex: 1; }
    .call-details h3 { margin: 0 0 4px; font-size: 1em; }
    .call-meta { margin: 0; font-size: 0.8em; color: rgba(255,255,255,0.4); }
    .call-status.missed, .call-status.declined { color: #ff6b6b; }
    .call-status.answered { color: #00e676; }
    .call-status.queued { color: #ffc107; }
    .call-duration { font-size: 0.85em; color: rgba(255,255,255,0.5); }
    .empty-state { text-align: center; padding: 40px 20px; color: rgba(255,255,255,0.4); }
    .call-overlay {
      position: fixed; top: 0; left: 0; right: 0; bottom: 0;
      background: linear-gradient(135deg, #0f0c29, #302b63); z-index: 100;
      display: flex; align-items: center; justify-content: center;
    }
    .call-overlay.incoming { background: linear-gradient(135deg, #1a1a2e, #16213e); }
    .call-screen {
      text-align: center; color: white; width: 100%; height: 100%; position: relative;
      display: flex; flex-direction: column; align-items: center; justify-content: center;
    }
    .remote-video {
      position: absolute; top: 0; left: 0; width: 100%; height: 100%;
      object-fit: cover; background: #000;
    }
    .local-video {
      position: absolute; top: 20px; right: 20px; width: 120px; height: 160px;
      object-fit: cover; border-radius: 12px; border: 2px solid rgba(255,255,255,0.3);
      z-index: 102; background: #333;
    }
    .call-info-overlay { position: relative; z-index: 101; }
    .call-info-overlay.video-mode {
      position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);
      background: rgba(0,0,0,0.5); padding: 30px 50px; border-radius: 20px;
    }
    .calling-avatar {
      width: 120px; height: 120px; border-radius: 50%; margin: 0 auto 20px;
      background: linear-gradient(135deg, #00d2ff, #3a7bd5);
      display: flex; align-items: center; justify-content: center;
      font-size: 2.5em; font-weight: 600;
    }
    .calling-avatar.connected { border: 3px solid #00e676; }
    .call-screen h2 { margin: 0 0 8px; }
    .call-state { color: rgba(255,255,255,0.6); font-size: 0.9em; margin: 0 0 4px; }
    .call-timer { color: #00e676; font-size: 1.1em; font-weight: 500; margin: 4px 0 0; }
    .pulse-ring { animation: pulse 2s infinite; }
    @keyframes pulse {
      0% { box-shadow: 0 0 0 0 rgba(0,210,255,0.5); }
      70% { box-shadow: 0 0 0 20px rgba(0,210,255,0); }
      100% { box-shadow: 0 0 0 0 rgba(0,210,255,0); }
    }
    .call-controls {
      display: flex; justify-content: center; gap: 24px; margin-top: 40px;
      position: relative; z-index: 102;
    }
    .control-btn {
      width: 60px; height: 60px; border-radius: 50%; border: none; cursor: pointer;
      display: flex; align-items: center; justify-content: center; font-size: 1.5em;
      background: rgba(255,255,255,0.15); color: white; transition: all 0.2s;
    }
    .control-btn:hover { transform: scale(1.05); }
    .control-btn.active { background: rgba(255,100,100,0.4); }
    .control-btn.end { background: #ff3b30; }
    .control-btn.end:hover { background: #ff1a1a; }
    .control-btn.decline { background: #ff3b30; }
    .control-btn.accept { background: #00c853; }
  `]
})
export class CallListComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('remoteVideo') remoteVideoRef!: ElementRef<HTMLVideoElement>;
  @ViewChild('localVideo') localVideoRef!: ElementRef<HTMLVideoElement>;

  contacts: ContactDto[] = [];
  callHistory: CallLogDto[] = [];
  activeTab: 'contacts' | 'history' = 'contacts';
  newContactUsername = '';
  addContactError = '';
  isOnline = true;
  activeCallContact: ContactDto | null = null;
  isMuted = false;
  isVideoOff = false;
  currentCallType = 'Audio';
  localStreamActive = false;

  webrtcCallState: WebRTCCallState = 'idle';
  incomingCall: IncomingCall | null = null;
  callDurationDisplay = '0:00';

  private callTimer: ReturnType<typeof setInterval> | null = null;
  private callStartTime: number = 0;
  private subscriptions: Subscription[] = [];
  private needsVideoAttach = false;

  constructor(
    private callService: CallService,
    private offlineService: OfflineService,
    private authService: AuthService,
    private webrtcService: WebRTCService,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.subscriptions.push(
      this.offlineService.isOnline$.subscribe(online => this.isOnline = online)
    );
    this.loadContacts();
    // SignalR is connected globally in AppComponent, no need to connect here

    this.subscriptions.push(
      this.webrtcService.callState$.subscribe(state => {
        this.webrtcCallState = state;
        if (state === 'connected') {
          this.startCallTimer();
        } else if (state === 'ended' || state === 'failed') {
          this.stopCallTimer();
          setTimeout(() => {
            this.zone.run(() => {
              this.webrtcCallState = 'idle';
              this.activeCallContact = null;
              this.isMuted = false;
              this.isVideoOff = false;
              this.localStreamActive = false;
            });
          }, 1500);
        }
      })
    );

    this.subscriptions.push(
      this.webrtcService.incomingCall$.subscribe(call => {
        const callerContact = this.contacts.find(c => c.contactUserId === call.callerId);
        if (callerContact) {
          call.callerName = callerContact.displayName || callerContact.username;
        }
        this.incomingCall = call;
      })
    );

    this.subscriptions.push(
      this.webrtcService.callEnded$.subscribe(() => {
        this.incomingCall = null;
      })
    );

    this.subscriptions.push(
      this.webrtcService.remoteStream$.subscribe(stream => {
        if (stream) { this.needsVideoAttach = true; }
      })
    );

    this.subscriptions.push(
      this.webrtcService.localStream$.subscribe(stream => {
        this.localStreamActive = !!stream;
        if (stream) { this.needsVideoAttach = true; }
      })
    );
  }

  ngAfterViewChecked(): void {
    if (this.needsVideoAttach) {
      this.attachStreams();
      this.needsVideoAttach = false;
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    this.stopCallTimer();
    // Don't disconnect WebRTC here - it's managed globally in AppComponent
  }

  private attachStreams(): void {
    if (this.remoteVideoRef?.nativeElement) {
      const sub = this.webrtcService.remoteStream$.subscribe(stream => {
        if (stream && this.remoteVideoRef?.nativeElement) {
          this.remoteVideoRef.nativeElement.srcObject = stream;
          this.remoteVideoRef.nativeElement.play().catch(() => {});
        }
      });
      sub.unsubscribe();
    }
    if (this.localVideoRef?.nativeElement) {
      const sub = this.webrtcService.localStream$.subscribe(stream => {
        if (stream && this.localVideoRef?.nativeElement) {
          this.localVideoRef.nativeElement.srcObject = stream;
          this.localVideoRef.nativeElement.play().catch(() => {});
        }
      });
      sub.unsubscribe();
    }
  }

  loadContacts(): void {
    this.callService.getContacts().subscribe({
      next: (contacts) => this.contacts = contacts,
      error: () => {}
    });
  }

  loadHistory(): void {
    this.callService.getHistory().subscribe({
      next: (history) => this.callHistory = history,
      error: () => {}
    });
  }

  addContact(): void {
    if (!this.newContactUsername) return;
    this.addContactError = '';
    this.callService.addContact(this.newContactUsername).subscribe({
      next: (contact) => {
        this.contacts.push(contact);
        this.newContactUsername = '';
      },
      error: (err) => {
        this.addContactError = err.error?.error || 'Failed to add contact';
      }
    });
  }

  async makeCall(contact: ContactDto, callType: string): Promise<void> {
    if (!this.isOnline) {
      await this.offlineService.queueCall(contact.contactUserId, callType);
      return;
    }
    this.activeCallContact = contact;
    this.currentCallType = callType;
    this.isMuted = false;
    this.isVideoOff = false;
    this.callService.initiateCall(contact.contactUserId, callType).subscribe({ error: () => {} });
    await this.webrtcService.startCall(contact.contactUserId, callType);
  }

  async acceptIncomingCall(): Promise<void> {
    if (!this.incomingCall) return;
    const call = this.incomingCall;
    this.currentCallType = call.callType;
    const callerContact = this.contacts.find(c => c.contactUserId === call.callerId);
    this.activeCallContact = callerContact || {
      id: 0, contactUserId: call.callerId, displayName: call.callerName,
      username: call.callerName, isOnline: true, lastSeen: new Date().toISOString()
    };
    this.incomingCall = null;
    await this.webrtcService.acceptCall(call.callerId, call.offer);
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

  getCallStateText(): string {
    switch (this.webrtcCallState) {
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

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return diffMins + 'm ago';
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return diffHours + 'h ago';
    return date.toLocaleDateString();
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return mins + ':' + secs.toString().padStart(2, '0');
  }

  getCallPartyName(call: CallLogDto): string {
    const currentUser = this.authService.currentUser;
    if (!currentUser) return '';
    return call.caller.id === currentUser.id ? call.receiver.displayName : call.caller.displayName;
  }

  getCallStatusClass(call: CallLogDto): string {
    return call.status.toLowerCase();
  }
}
