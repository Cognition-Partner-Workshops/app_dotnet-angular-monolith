import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReelService } from '../../services/reel.service';
import { OfflineService } from '../../services/offline.service';
import { AuthService } from '../../services/auth.service';
import { ReelDto, OfflineReel } from '../../models/interfaces';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-reel-feed',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="reels-container">
      <div class="reels-header">
        <h2>Reels</h2>
        <button class="btn-offline-lib" (click)="showOffline = !showOffline">
          {{ showOffline ? 'Online Feed' : 'Offline Library (' + offlineReels.length + ')' }}
        </button>
      </div>

      <!-- Offline Banner -->
      <div *ngIf="!isOnline" class="offline-banner">
        You're offline. Showing downloaded reels only.
      </div>

      <!-- Online Feed -->
      <div *ngIf="!showOffline && isOnline" class="reels-feed">
        <div *ngFor="let reel of reels; let i = index" class="reel-card"
             [class.active]="currentIndex === i" (click)="currentIndex = i">
          <div class="reel-video-container">
            <video #videoPlayer [src]="reel.videoUrl" [poster]="reel.thumbnailUrl || ''"
                   playsinline loop [muted]="currentIndex !== i"
                   (click)="togglePlay($event)" class="reel-video">
            </video>
            <div class="reel-overlay">
              <div class="reel-info">
                <h3>{{ reel.title }}</h3>
                <p class="creator">{{ reel.creator.displayName }}</p>
                <p *ngIf="reel.description" class="description">{{ reel.description }}</p>
              </div>
              <div class="reel-actions">
                <button class="action-btn" [class.liked]="reel.isLikedByCurrentUser" (click)="toggleLike(reel, $event)">
                  <span class="icon">{{ reel.isLikedByCurrentUser ? '&#10084;' : '&#9825;' }}</span>
                  <span class="count">{{ reel.likeCount }}</span>
                </button>
                <button class="action-btn" (click)="viewReel(reel, $event)">
                  <span class="icon">&#128065;</span>
                  <span class="count">{{ reel.viewCount }}</span>
                </button>
                <button *ngIf="reel.isDownloadable" class="action-btn download-btn"
                        [class.downloaded]="isDownloaded(reel.id)"
                        [disabled]="downloadingId === reel.id"
                        (click)="downloadReel(reel, $event)">
                  <span class="icon">{{ isDownloaded(reel.id) ? '&#10003;' : '&#8615;' }}</span>
                  <span class="count">{{ downloadingId === reel.id ? 'Saving...' : (isDownloaded(reel.id) ? 'Saved' : 'Save') }}</span>
                </button>
              </div>
            </div>
          </div>
          <div *ngIf="reel.tags" class="reel-tags">
            <span *ngFor="let tag of reel.tags.split(',')" class="tag">#{{ tag.trim() }}</span>
          </div>
        </div>
        <div *ngIf="hasMore" class="load-more">
          <button (click)="loadMore()" [disabled]="isLoading" class="btn-load-more">
            {{ isLoading ? 'Loading...' : 'Load More' }}
          </button>
        </div>
      </div>

      <!-- Offline Library -->
      <div *ngIf="showOffline || !isOnline" class="offline-library">
        <div *ngIf="offlineReels.length === 0" class="empty-state">
          <p>No downloaded reels yet.</p>
          <p>Download reels while online to watch them during your journey!</p>
        </div>
        <div *ngFor="let reel of offlineReels" class="offline-reel-card">
          <div class="reel-video-container">
            <video [src]="getObjectUrl(reel)" playsinline loop class="reel-video"
                   (click)="togglePlay($event)">
            </video>
            <div class="reel-overlay">
              <div class="reel-info">
                <h3>{{ reel.title }}</h3>
                <p class="creator">{{ reel.creatorName }}</p>
              </div>
              <div class="reel-actions">
                <button class="action-btn delete-btn" (click)="removeOfflineReel(reel.id, $event)">
                  <span class="icon">&#128465;</span>
                  <span class="count">Remove</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .reels-container { max-width: 480px; margin: 0 auto; padding: 0; min-height: 100vh; background: #000; }
    .reels-header {
      display: flex; justify-content: space-between; align-items: center;
      padding: 16px 20px; background: rgba(0,0,0,0.8); position: sticky; top: 0; z-index: 10;
    }
    .reels-header h2 { margin: 0; color: white; font-size: 1.3em; }
    .btn-offline-lib {
      padding: 8px 16px; border: 1px solid rgba(0,210,255,0.5); border-radius: 20px;
      background: transparent; color: #00d2ff; font-size: 0.8em; cursor: pointer;
    }
    .offline-banner {
      padding: 10px 20px; background: #ff6b35; color: white; text-align: center;
      font-size: 0.85em; font-weight: 500;
    }
    .reel-card, .offline-reel-card {
      position: relative; margin-bottom: 2px; background: #111;
    }
    .reel-video-container { position: relative; width: 100%; aspect-ratio: 9/16; overflow: hidden; }
    .reel-video { width: 100%; height: 100%; object-fit: cover; cursor: pointer; }
    .reel-overlay {
      position: absolute; bottom: 0; left: 0; right: 0;
      background: linear-gradient(transparent, rgba(0,0,0,0.8));
      padding: 60px 16px 16px; display: flex; justify-content: space-between; align-items: flex-end;
    }
    .reel-info { flex: 1; color: white; }
    .reel-info h3 { margin: 0 0 4px; font-size: 1em; }
    .reel-info .creator { margin: 0 0 4px; font-size: 0.85em; color: rgba(255,255,255,0.7); }
    .reel-info .description { margin: 0; font-size: 0.8em; color: rgba(255,255,255,0.5); max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .reel-actions { display: flex; flex-direction: column; gap: 16px; align-items: center; margin-left: 12px; }
    .action-btn {
      display: flex; flex-direction: column; align-items: center; gap: 2px;
      background: none; border: none; color: white; cursor: pointer; padding: 4px;
    }
    .action-btn .icon { font-size: 1.6em; }
    .action-btn .count { font-size: 0.7em; }
    .action-btn.liked .icon { color: #ff2d55; }
    .action-btn.downloaded .icon { color: #00d2ff; }
    .action-btn.delete-btn .icon { color: #ff6b6b; }
    .reel-tags { padding: 8px 16px; display: flex; gap: 8px; flex-wrap: wrap; }
    .tag { color: #00d2ff; font-size: 0.8em; }
    .load-more { padding: 20px; text-align: center; }
    .btn-load-more {
      padding: 12px 32px; border: 1px solid rgba(255,255,255,0.2); border-radius: 20px;
      background: transparent; color: white; cursor: pointer; font-size: 0.9em;
    }
    .btn-load-more:disabled { opacity: 0.5; }
    .empty-state { text-align: center; padding: 60px 20px; color: rgba(255,255,255,0.5); }
    .empty-state p { margin: 8px 0; }
    .offline-library { padding-bottom: 80px; }
    .reels-feed { padding-bottom: 80px; }
  `]
})
export class ReelFeedComponent implements OnInit, OnDestroy {
  reels: ReelDto[] = [];
  offlineReels: OfflineReel[] = [];
  currentIndex = 0;
  page = 1;
  hasMore = true;
  isLoading = false;
  showOffline = false;
  isOnline = true;
  downloadingId: number | null = null;
  private downloadedIds = new Set<number>();
  private objectUrls = new Map<number, string>();
  private subscriptions: Subscription[] = [];

  constructor(
    private reelService: ReelService,
    private offlineService: OfflineService,
    private authService: AuthService,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.subscriptions.push(
      this.offlineService.isOnline$.subscribe(online => {
        this.isOnline = online;
        if (!online) this.showOffline = true;
      }),
      this.offlineService.offlineReels$.subscribe(reels => {
        this.offlineReels = reels;
        this.downloadedIds = new Set(reels.map(r => r.id));
      })
    );

    if (this.isOnline) {
      this.loadReels();
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    this.objectUrls.forEach(url => URL.revokeObjectURL(url));
  }

  loadReels(): void {
    this.isLoading = true;
    this.reelService.getFeed(this.page, 10).subscribe({
      next: (response) => {
        this.reels = [...this.reels, ...response.reels];
        this.hasMore = response.hasMore;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  loadMore(): void {
    this.page++;
    this.loadReels();
  }

  togglePlay(event: Event): void {
    event.stopPropagation();
    const video = event.target as HTMLVideoElement;
    if (video.paused) {
      video.play();
    } else {
      video.pause();
    }
  }

  toggleLike(reel: ReelDto, event: Event): void {
    event.stopPropagation();
    if (!this.authService.isAuthenticated) return;

    this.reelService.toggleLike(reel.id).subscribe({
      next: (result) => {
        reel.isLikedByCurrentUser = result.isLiked;
        reel.likeCount += result.isLiked ? 1 : -1;
      }
    });
  }

  viewReel(reel: ReelDto, event: Event): void {
    event.stopPropagation();
  }

  downloadReel(reel: ReelDto, event: Event): void {
    event.stopPropagation();
    if (this.downloadingId || this.isDownloaded(reel.id)) return;

    this.downloadingId = reel.id;
    this.reelService.getDownloadInfo(reel.id).subscribe({
      next: (info) => {
        this.http.get(info.downloadUrl, { responseType: 'blob' }).subscribe({
          next: async (blob) => {
            await this.offlineService.saveReelOffline(
              reel.id, reel.title, reel.description, reel.thumbnailUrl,
              reel.durationSeconds, reel.creator.displayName, blob
            );
            this.downloadingId = null;
          },
          error: () => { this.downloadingId = null; }
        });
      },
      error: () => { this.downloadingId = null; }
    });
  }

  isDownloaded(reelId: number): boolean {
    return this.downloadedIds.has(reelId);
  }

  getObjectUrl(reel: OfflineReel): string {
    if (!this.objectUrls.has(reel.id)) {
      this.objectUrls.set(reel.id, URL.createObjectURL(reel.blob));
    }
    return this.objectUrls.get(reel.id)!;
  }

  async removeOfflineReel(reelId: number, event: Event): Promise<void> {
    event.stopPropagation();
    const url = this.objectUrls.get(reelId);
    if (url) {
      URL.revokeObjectURL(url);
      this.objectUrls.delete(reelId);
    }
    await this.offlineService.removeOfflineReel(reelId);
  }
}
