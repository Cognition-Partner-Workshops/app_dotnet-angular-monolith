import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { OfflineReel } from '../models/interfaces';

@Injectable({ providedIn: 'root' })
export class OfflineService {
  private dbName = 'TrainConnectOffline';
  private dbVersion = 1;
  private db: IDBDatabase | null = null;

  private isOnlineSubject = new BehaviorSubject<boolean>(true);
  isOnline$ = this.isOnlineSubject.asObservable();

  private offlineReelsSubject = new BehaviorSubject<OfflineReel[]>([]);
  offlineReels$ = this.offlineReelsSubject.asObservable();

  private offlineDebounce: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // When browser says online, trust it immediately
    window.addEventListener('online', () => {
      if (this.offlineDebounce) { clearTimeout(this.offlineDebounce); this.offlineDebounce = null; }
      this.isOnlineSubject.next(true);
    });
    // When browser says offline, verify with a server ping before showing banner
    window.addEventListener('offline', () => {
      if (this.offlineDebounce) clearTimeout(this.offlineDebounce);
      this.offlineDebounce = setTimeout(() => this.verifyConnectivity(), 3000);
    });
    this.initDb();
  }

  private async verifyConnectivity(): Promise<void> {
    try {
      const resp = await fetch('/healthz', { method: 'GET', cache: 'no-store' });
      this.isOnlineSubject.next(resp.ok);
    } catch {
      this.isOnlineSubject.next(false);
    }
  }

  get isOnline(): boolean {
    return this.isOnlineSubject.value;
  }

  private initDb(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, this.dbVersion);

      request.onupgradeneeded = () => {
        const db = request.result;
        if (!db.objectStoreNames.contains('reels')) {
          db.createObjectStore('reels', { keyPath: 'id' });
        }
        if (!db.objectStoreNames.contains('callQueue')) {
          db.createObjectStore('callQueue', { keyPath: 'id', autoIncrement: true });
        }
      };

      request.onsuccess = () => {
        this.db = request.result;
        this.loadOfflineReels();
        resolve();
      };

      request.onerror = () => reject(request.error);
    });
  }

  async saveReelOffline(reelId: number, title: string, description: string | undefined, thumbnailUrl: string | undefined, durationSeconds: number, creatorName: string, videoBlob: Blob): Promise<void> {
    if (!this.db) await this.initDb();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction('reels', 'readwrite');
      const store = transaction.objectStore('reels');

      const offlineReel: OfflineReel = {
        id: reelId,
        title,
        description,
        thumbnailUrl,
        durationSeconds,
        creatorName,
        downloadedAt: new Date().toISOString(),
        blob: videoBlob
      };

      const request = store.put(offlineReel);
      request.onsuccess = () => {
        this.loadOfflineReels();
        resolve();
      };
      request.onerror = () => reject(request.error);
    });
  }

  async removeOfflineReel(reelId: number): Promise<void> {
    if (!this.db) await this.initDb();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction('reels', 'readwrite');
      const store = transaction.objectStore('reels');
      const request = store.delete(reelId);
      request.onsuccess = () => {
        this.loadOfflineReels();
        resolve();
      };
      request.onerror = () => reject(request.error);
    });
  }

  async getOfflineReel(reelId: number): Promise<OfflineReel | undefined> {
    if (!this.db) await this.initDb();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction('reels', 'readonly');
      const store = transaction.objectStore('reels');
      const request = store.get(reelId);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  }

  async isReelDownloaded(reelId: number): Promise<boolean> {
    const reel = await this.getOfflineReel(reelId);
    return !!reel;
  }

  private loadOfflineReels(): void {
    if (!this.db) return;

    const transaction = this.db.transaction('reels', 'readonly');
    const store = transaction.objectStore('reels');
    const request = store.getAll();

    request.onsuccess = () => {
      this.offlineReelsSubject.next(request.result);
    };
  }

  async queueCall(targetUserId: number, callType: string): Promise<void> {
    if (!this.db) await this.initDb();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction('callQueue', 'readwrite');
      const store = transaction.objectStore('callQueue');
      const request = store.add({
        targetUserId,
        callType,
        queuedAt: new Date().toISOString()
      });
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  async getQueuedCalls(): Promise<Array<{ id: number; targetUserId: number; callType: string; queuedAt: string }>> {
    if (!this.db) await this.initDb();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction('callQueue', 'readonly');
      const store = transaction.objectStore('callQueue');
      const request = store.getAll();
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  }

  async clearQueuedCall(id: number): Promise<void> {
    if (!this.db) await this.initDb();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction('callQueue', 'readwrite');
      const store = transaction.objectStore('callQueue');
      const request = store.delete(id);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }
}
