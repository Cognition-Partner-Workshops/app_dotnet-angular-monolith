import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ServerConfigService {
  private readonly STORAGE_KEY = 'tc_server_url';
  private serverUrlSubject = new BehaviorSubject<string>(this.loadServerUrl());
  serverUrl$ = this.serverUrlSubject.asObservable();

  /** Returns the configured server base URL, or '' for same-origin (online mode). */
  get serverUrl(): string {
    return this.serverUrlSubject.value;
  }

  /** True when a custom local server URL is configured (offline/LAN mode). */
  get isLocalMode(): boolean {
    return this.serverUrl.length > 0;
  }

  /** Set a local server URL (e.g. 'http://192.168.1.5:5000'). Pass '' to reset to online mode. */
  setServerUrl(url: string): void {
    const cleaned = url.replace(/\/+$/, ''); // strip trailing slashes
    localStorage.setItem(this.STORAGE_KEY, cleaned);
    this.serverUrlSubject.next(cleaned);
  }

  /** Resolve an API path against the configured server. */
  resolveUrl(path: string): string {
    if (!this.serverUrl) return path; // same-origin
    return this.serverUrl + path;
  }

  /** Resolve a WebSocket URL (ws:// or wss://) for the configured server. */
  resolveWsUrl(path: string): string {
    if (!this.serverUrl) {
      const wsProtocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
      return `${wsProtocol}//${location.host}${path}`;
    }
    const wsProtocol = this.serverUrl.startsWith('https') ? 'wss:' : 'ws:';
    const host = this.serverUrl.replace(/^https?:\/\//, '');
    return `${wsProtocol}//${host}${path}`;
  }

  /** Test connectivity to the configured server. */
  async testConnection(url?: string): Promise<boolean> {
    const base = url ?? this.serverUrl;
    const target = base ? `${base}/healthz` : '/healthz';
    try {
      const resp = await fetch(target, { method: 'GET', cache: 'no-store', mode: 'cors' });
      return resp.ok;
    } catch {
      return false;
    }
  }

  private loadServerUrl(): string {
    return localStorage.getItem(this.STORAGE_KEY) || '';
  }
}
