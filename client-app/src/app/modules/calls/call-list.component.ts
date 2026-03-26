import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CallService } from '../../services/call.service';
import { OfflineService } from '../../services/offline.service';
import { AuthService } from '../../services/auth.service';
import { ContactDto, CallLogDto } from '../../models/interfaces';

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
              &#128222;
            </button>
            <button class="call-btn video" (click)="makeCall(contact, 'Video')" title="Video Call">
              &#128249;
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

      <!-- Active Call Overlay -->
      <div *ngIf="activeCall" class="call-overlay">
        <div class="call-screen">
          <div class="calling-avatar">
            {{ getInitials(activeCallContact?.displayName || activeCallContact?.username || '') }}
          </div>
          <h2>{{ activeCallContact?.displayName || activeCallContact?.username }}</h2>
          <p class="call-state">{{ callState }}</p>
          <div class="call-controls">
            <button class="control-btn mute" (click)="toggleMute()">
              {{ isMuted ? '&#128263;' : '&#128266;' }}
            </button>
            <button class="control-btn end" (click)="endActiveCall()">
              &#128308;
            </button>
            <button class="control-btn speaker" (click)="toggleSpeaker()">
              {{ isSpeaker ? '&#128264;' : '&#128266;' }}
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
      width: 40px; height: 40px; border-radius: 50%; border: none; cursor: pointer;
      display: flex; align-items: center; justify-content: center; font-size: 1.2em;
    }
    .call-btn.audio { background: rgba(0,210,255,0.15); }
    .call-btn.video { background: rgba(0,230,118,0.15); }
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
    .call-screen { text-align: center; color: white; }
    .calling-avatar {
      width: 120px; height: 120px; border-radius: 50%; margin: 0 auto 20px;
      background: linear-gradient(135deg, #00d2ff, #3a7bd5);
      display: flex; align-items: center; justify-content: center;
      font-size: 2.5em; font-weight: 600;
    }
    .call-screen h2 { margin: 0 0 8px; }
    .call-state { color: rgba(255,255,255,0.6); font-size: 0.9em; }
    .call-controls { display: flex; justify-content: center; gap: 24px; margin-top: 40px; }
    .control-btn {
      width: 60px; height: 60px; border-radius: 50%; border: none; cursor: pointer;
      display: flex; align-items: center; justify-content: center; font-size: 1.5em;
    }
    .control-btn.mute { background: rgba(255,255,255,0.15); }
    .control-btn.end { background: #ff3b30; }
    .control-btn.speaker { background: rgba(255,255,255,0.15); }
  `]
})
export class CallListComponent implements OnInit {
  contacts: ContactDto[] = [];
  callHistory: CallLogDto[] = [];
  activeTab: 'contacts' | 'history' = 'contacts';
  newContactUsername = '';
  addContactError = '';
  isOnline = true;
  activeCall: CallLogDto | null = null;
  activeCallContact: ContactDto | null = null;
  callState = 'Calling...';
  isMuted = false;
  isSpeaker = false;

  constructor(
    private callService: CallService,
    private offlineService: OfflineService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.offlineService.isOnline$.subscribe(online => this.isOnline = online);
    this.loadContacts();
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
      this.callState = 'Call queued - will connect when online';
      this.activeCallContact = contact;
      this.activeCall = {} as CallLogDto;
      setTimeout(() => this.activeCall = null, 3000);
      return;
    }

    this.activeCallContact = contact;
    this.callState = 'Calling...';

    this.callService.initiateCall(contact.contactUserId, callType).subscribe({
      next: (call) => {
        this.activeCall = call;
        if (call.status === 'Queued') {
          this.callState = 'User is offline - call queued';
          setTimeout(() => this.endActiveCall(), 5000);
        } else {
          this.callState = 'Connected';
        }
      },
      error: () => {
        this.callState = 'Call failed';
        setTimeout(() => this.activeCall = null, 2000);
      }
    });
  }

  endActiveCall(): void {
    if (this.activeCall?.id) {
      this.callService.endCall(this.activeCall.id).subscribe();
    }
    this.activeCall = null;
    this.activeCallContact = null;
    this.callState = 'Calling...';
    this.isMuted = false;
    this.isSpeaker = false;
  }

  toggleMute(): void { this.isMuted = !this.isMuted; }
  toggleSpeaker(): void { this.isSpeaker = !this.isSpeaker; }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    return date.toLocaleDateString();
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
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
