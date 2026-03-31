import { Injectable, NgZone } from '@angular/core';
import { AuthService } from './auth.service';
import { BehaviorSubject, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

export interface IncomingCall {
  callerId: number;
  callerName: string;
  callType: string;
  offer: string;
}

export type WebRTCCallState = 'idle' | 'calling' | 'ringing' | 'connected' | 'ended' | 'failed';

@Injectable({ providedIn: 'root' })
export class WebRTCService {
  private hubConnection: signalR.HubConnection | null = null;
  private peerConnection: RTCPeerConnection | null = null;
  private localStream: MediaStream | null = null;
  private remoteStream: MediaStream | null = null;

  private callStateSubject = new BehaviorSubject<WebRTCCallState>('idle');
  callState$ = this.callStateSubject.asObservable();

  private remoteStreamSubject = new BehaviorSubject<MediaStream | null>(null);
  remoteStream$ = this.remoteStreamSubject.asObservable();

  private localStreamSubject = new BehaviorSubject<MediaStream | null>(null);
  localStream$ = this.localStreamSubject.asObservable();

  private incomingCallSubject = new Subject<IncomingCall>();
  incomingCall$ = this.incomingCallSubject.asObservable();

  private callEndedSubject = new Subject<void>();
  callEnded$ = this.callEndedSubject.asObservable();

  private currentTargetUserId: number | null = null;
  private currentCallType: string = 'Audio';
  private pendingIceCandidates: RTCIceCandidateInit[] = [];

  private readonly rtcConfig: RTCConfiguration = {
    iceServers: [
      { urls: 'stun:stun.l.google.com:19302' },
      { urls: 'stun:stun1.l.google.com:19302' },
      { urls: 'stun:stun2.l.google.com:19302' },
      { urls: 'stun:stun3.l.google.com:19302' },
      { urls: 'stun:stun4.l.google.com:19302' },
      {
        urls: 'turn:openrelay.metered.ca:80',
        username: 'openrelayproject',
        credential: 'openrelayproject'
      },
      {
        urls: 'turn:openrelay.metered.ca:443',
        username: 'openrelayproject',
        credential: 'openrelayproject'
      },
      {
        urls: 'turn:openrelay.metered.ca:443?transport=tcp',
        username: 'openrelayproject',
        credential: 'openrelayproject'
      }
    ],
    iceCandidatePoolSize: 10
  };

  constructor(
    private authService: AuthService,
    private ngZone: NgZone
  ) {}

  async connect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;

    const token = this.authService.getAccessToken();
    if (!token) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/call', {
        accessTokenFactory: () => this.authService.getAccessToken() || ''
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.setupSignalRHandlers();

    try {
      await this.hubConnection.start();
      console.log('SignalR connected for WebRTC signaling');
    } catch (err) {
      console.error('SignalR connection failed:', err);
    }
  }

  disconnect(): void {
    this.hangUp();
    this.hubConnection?.stop();
    this.hubConnection = null;
  }

  private setupSignalRHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveOffer', (callerId: number, offerJson: string) => {
      this.ngZone.run(() => {
        const offer = JSON.parse(offerJson);
        this.incomingCallSubject.next({
          callerId,
          callerName: `User #${callerId}`,
          callType: offer.callType || 'Audio',
          offer: offerJson
        });
      });
    });

    this.hubConnection.on('ReceiveAnswer', async (_answererId: number, answerJson: string) => {
      this.ngZone.run(async () => {
        try {
          const answer = JSON.parse(answerJson);
          if (this.peerConnection && this.peerConnection.signalingState === 'have-local-offer') {
            await this.peerConnection.setRemoteDescription(new RTCSessionDescription(answer.sdp));
            this.callStateSubject.next('connected');
            // Apply any pending ICE candidates
            for (const candidate of this.pendingIceCandidates) {
              await this.peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
            }
            this.pendingIceCandidates = [];
          }
        } catch (err) {
          console.error('Error handling answer:', err);
        }
      });
    });

    this.hubConnection.on('ReceiveIceCandidate', async (_senderId: number, candidateJson: string) => {
      try {
        const candidate = JSON.parse(candidateJson);
        if (this.peerConnection) {
          if (this.peerConnection.remoteDescription) {
            await this.peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
          } else {
            this.pendingIceCandidates.push(candidate);
          }
        }
      } catch (err) {
        console.error('Error handling ICE candidate:', err);
      }
    });

    this.hubConnection.on('CallDeclined', (_declinerId: number) => {
      this.ngZone.run(() => {
        this.callStateSubject.next('ended');
        this.cleanupCall();
        this.callEndedSubject.next();
      });
    });

    this.hubConnection.on('CallEnded', (_enderId: number) => {
      this.ngZone.run(() => {
        this.callStateSubject.next('ended');
        this.cleanupCall();
        this.callEndedSubject.next();
      });
    });

    this.hubConnection.on('CallFailed', (_targetUserId: number, reason: string) => {
      this.ngZone.run(() => {
        console.warn('Call failed:', reason);
        this.callStateSubject.next('failed');
        this.cleanupCall();
      });
    });
  }

  async startCall(targetUserId: number, callType: string): Promise<void> {
    this.currentTargetUserId = targetUserId;
    this.currentCallType = callType;
    this.callStateSubject.next('calling');
    this.pendingIceCandidates = [];

    try {
      await this.acquireMedia(callType);
      this.createPeerConnection();

      // Add local tracks to peer connection
      if (this.localStream && this.peerConnection) {
        this.localStream.getTracks().forEach(track => {
          this.peerConnection!.addTrack(track, this.localStream!);
        });
      }

      // Create and send offer
      const offer = await this.peerConnection!.createOffer();
      await this.peerConnection!.setLocalDescription(offer);

      const offerPayload = JSON.stringify({
        sdp: { type: offer.type, sdp: offer.sdp },
        callType
      });

      await this.hubConnection!.invoke('SendOffer', targetUserId, offerPayload);
    } catch (err) {
      console.error('Error starting call:', err);
      this.callStateSubject.next('failed');
      this.cleanupCall();
    }
  }

  async acceptCall(callerId: number, offerJson: string): Promise<void> {
    this.currentTargetUserId = callerId;
    this.pendingIceCandidates = [];

    try {
      const offer = JSON.parse(offerJson);
      this.currentCallType = offer.callType || 'Audio';

      await this.acquireMedia(this.currentCallType);
      this.createPeerConnection();

      // Add local tracks
      if (this.localStream && this.peerConnection) {
        this.localStream.getTracks().forEach(track => {
          this.peerConnection!.addTrack(track, this.localStream!);
        });
      }

      // Set remote description (the offer)
      await this.peerConnection!.setRemoteDescription(new RTCSessionDescription(offer.sdp));

      // Apply any pending ICE candidates
      for (const candidate of this.pendingIceCandidates) {
        await this.peerConnection!.addIceCandidate(new RTCIceCandidate(candidate));
      }
      this.pendingIceCandidates = [];

      // Create and send answer
      const answer = await this.peerConnection!.createAnswer();
      await this.peerConnection!.setLocalDescription(answer);

      const answerPayload = JSON.stringify({
        sdp: { type: answer.type, sdp: answer.sdp }
      });

      await this.hubConnection!.invoke('SendAnswer', callerId, answerPayload);
      this.callStateSubject.next('connected');
    } catch (err) {
      console.error('Error accepting call:', err);
      this.callStateSubject.next('failed');
      this.cleanupCall();
    }
  }

  async declineCall(callerId: number): Promise<void> {
    await this.hubConnection?.invoke('DeclineCall', callerId);
  }

  async hangUp(): Promise<void> {
    if (this.currentTargetUserId && this.hubConnection) {
      try {
        await this.hubConnection.invoke('EndCall', this.currentTargetUserId);
      } catch { /* ignore if already disconnected */ }
    }
    this.callStateSubject.next('ended');
    this.cleanupCall();
    this.callEndedSubject.next();
  }

  toggleMute(): boolean {
    if (this.localStream) {
      const audioTrack = this.localStream.getAudioTracks()[0];
      if (audioTrack) {
        audioTrack.enabled = !audioTrack.enabled;
        return !audioTrack.enabled; // return true if muted
      }
    }
    return false;
  }

  toggleVideo(): boolean {
    if (this.localStream) {
      const videoTrack = this.localStream.getVideoTracks()[0];
      if (videoTrack) {
        videoTrack.enabled = !videoTrack.enabled;
        return !videoTrack.enabled; // return true if video off
      }
    }
    return false;
  }

  private async acquireMedia(callType: string): Promise<void> {
    const constraints: MediaStreamConstraints = {
      audio: true,
      video: callType === 'Video' ? { width: { ideal: 640 }, height: { ideal: 480 }, facingMode: 'user' } : false
    };

    try {
      this.localStream = await navigator.mediaDevices.getUserMedia(constraints);
      this.localStreamSubject.next(this.localStream);
    } catch (err) {
      console.error('Failed to acquire media:', err);
      // Fallback: try audio only if video fails
      if (callType === 'Video') {
        try {
          this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
          this.localStreamSubject.next(this.localStream);
        } catch (audioErr) {
          throw audioErr;
        }
      } else {
        throw err;
      }
    }
  }

  private createPeerConnection(): void {
    this.peerConnection = new RTCPeerConnection(this.rtcConfig);

    // Handle ICE candidates
    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate && this.currentTargetUserId && this.hubConnection) {
        this.hubConnection.invoke(
          'SendIceCandidate',
          this.currentTargetUserId,
          JSON.stringify(event.candidate.toJSON())
        ).catch(err => console.error('Error sending ICE candidate:', err));
      }
    };

    // Handle remote stream
    this.peerConnection.ontrack = (event) => {
      this.ngZone.run(() => {
        if (event.streams && event.streams[0]) {
          this.remoteStream = event.streams[0];
          this.remoteStreamSubject.next(this.remoteStream);
        }
      });
    };

    // Monitor connection state
    this.peerConnection.onconnectionstatechange = () => {
      this.ngZone.run(() => {
        const state = this.peerConnection?.connectionState;
        if (state === 'connected') {
          this.callStateSubject.next('connected');
        } else if (state === 'disconnected' || state === 'failed' || state === 'closed') {
          this.callStateSubject.next('ended');
          this.cleanupCall();
          this.callEndedSubject.next();
        }
      });
    };

    this.peerConnection.oniceconnectionstatechange = () => {
      console.log('ICE connection state:', this.peerConnection?.iceConnectionState);
    };
  }

  private cleanupCall(): void {
    // Stop local media tracks
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
      this.localStreamSubject.next(null);
    }

    // Close peer connection
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }

    this.remoteStream = null;
    this.remoteStreamSubject.next(null);
    this.currentTargetUserId = null;
    this.pendingIceCandidates = [];
  }
}
