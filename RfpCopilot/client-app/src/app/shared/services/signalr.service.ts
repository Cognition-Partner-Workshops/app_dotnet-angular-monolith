import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

export interface ProgressUpdate {
  rfpDocumentId: number;
  status: string;
  message: string;
}

export interface AgentProgressUpdate {
  rfpDocumentId: number;
  agentName: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  progressUpdate$ = new Subject<ProgressUpdate>();
  agentProgress$ = new Subject<AgentProgressUpdate>();

  startConnection(): void {
    if (this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/rfp-progress')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ProgressUpdate', (rfpDocumentId: number, status: string, message: string) => {
      this.progressUpdate$.next({ rfpDocumentId, status, message });
    });

    this.hubConnection.on('AgentProgress', (rfpDocumentId: number, agentName: string, status: string) => {
      this.agentProgress$.next({ rfpDocumentId, agentName, status });
    });

    this.hubConnection.start().catch(err => console.error('SignalR connection error:', err));
  }

  stopConnection(): void {
    this.hubConnection?.stop();
    this.hubConnection = null;
  }
}
