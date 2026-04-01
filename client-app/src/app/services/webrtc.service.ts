import { Injectable, NgZone } from '@angular/core';
import { AuthService } from './auth.service';
import { ServerConfigService } from './server-config.service';
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

  // Active call metadata (exposed for global UI)
  private activeCallNameSubject = new BehaviorSubject<string>('');
  activeCallName$ = this.activeCallNameSubject.asObservable();
  private activeCallTypeSubject = new BehaviorSubject<string>('Audio');
  activeCallType$ = this.activeCallTypeSubject.asObservable();
  private pendingIceCandidates: RTCIceCandidateInit[] = [];

  // Audio relay properties
  private relayWs: WebSocket | null = null;
  private currentCallId: string = '';
  private captureAudioCtx: AudioContext | null = null;
  private captureProcessor: ScriptProcessorNode | null = null;
  private playbackAudioCtx: AudioContext | null = null;
  private nextPlayTime: number = 0;
  private readonly RELAY_SAMPLE_RATE = 16000;
  private readonly RELAY_BUFFER_SIZE = 2048;

  // Video relay properties (server-relayed video via MediaRecorder/MediaSource)
  private videoRelayWs: WebSocket | null = null;
  private mediaRecorder: MediaRecorder | null = null;
  private remoteMediaSource: MediaSource | null = null;
  private remoteSourceBuffer: SourceBuffer | null = null;
  private videoBufferQueue: ArrayBuffer[] = [];
  private isAppendingVideo = false;

  private remoteVideoUrlSubject = new BehaviorSubject<string | null>(null);
  remoteVideoUrl$ = this.remoteVideoUrlSubject.asObservable();

  // Track whether WebRTC P2P connected (so we can stop relay)
  private peerConnectionConnected = false;

  // RTC config - always includes STUN for NAT traversal
  private getRtcConfig(): RTCConfiguration {
    return {
      iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun1.l.google.com:19302' },
        { urls: 'stun:stun2.l.google.com:19302' }
      ],
      iceCandidatePoolSize: 10
    };
  }

  constructor(
    private authService: AuthService,
    private serverConfig: ServerConfigService,
    private ngZone: NgZone
  ) {}

  getRemoteStream(): MediaStream | null {
    return this.remoteStream;
  }

  getLocalStream(): MediaStream | null {
    return this.localStream;
  }

  async connect(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;

    const token = this.authService.getAccessToken();
    if (!token) {
      console.log('No valid token, skipping SignalR connect');
      return;
    }

    // Clean up any existing connection before creating a new one
    if (this.hubConnection) {
      try { await this.hubConnection.stop(); } catch { /* ignore */ }
      this.hubConnection = null;
    }

    const hubUrl = this.serverConfig.resolveUrl('/hubs/call');
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.authService.getAccessToken() || ''
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000, 15000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Increase timeouts for connections through proxy
    this.hubConnection.serverTimeoutInMilliseconds = 120000; // 2 minutes
    this.hubConnection.keepAliveIntervalInMilliseconds = 30000; // 30 seconds

    this.setupSignalRHandlers();
    this.setupReconnectionHandlers();

    try {
      await this.hubConnection.start();
      console.log('SignalR connected for WebRTC signaling');
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : String(err);
      console.error('SignalR connection failed:', errMsg);
      // If 401/Unauthorized, the stored token is stale - clear it
      if (errMsg.includes('401') || errMsg.includes('Unauthorized')) {
        console.warn('Clearing stale auth - please log in again');
        this.authService.logout();
      }
      this.hubConnection = null;
    }
  }

  disconnect(): void {
    this.hangUp();
    this.hubConnection?.stop();
    this.hubConnection = null;
  }

  private setupReconnectionHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.onreconnecting((error) => {
      console.warn('SignalR reconnecting...', error?.message);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('SignalR reconnected with id:', connectionId);
    });

    this.hubConnection.onclose(async (error) => {
      console.warn('SignalR connection closed:', error?.message);
      // Try to reconnect after a delay if we still have a valid token
      const token = this.authService.getAccessToken();
      if (token) {
        setTimeout(() => this.connect(), 5000);
      }
    });
  }

  private setupSignalRHandlers(): void {
    if (!this.hubConnection) return;

    // Handle user presence events from CallHub
    this.hubConnection.on('UserOnline', (userId: number) => {
      console.log('User online:', userId);
    });

    this.hubConnection.on('UserOffline', (userId: number) => {
      console.log('User offline:', userId);
    });

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

          // Start audio relay with the callId from the answer
          if (answer.callId && !this.relayWs) {
            this.currentCallId = answer.callId;
            this.startAudioRelay();
          }

          if (this.peerConnection && this.peerConnection.signalingState === 'have-local-offer') {
            await this.peerConnection.setRemoteDescription(new RTCSessionDescription(answer.sdp));
            this.callStateSubject.next('connected');
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

  async startCall(targetUserId: number, callType: string, contactName?: string): Promise<void> {
    this.currentTargetUserId = targetUserId;
    this.currentCallType = callType;
    this.activeCallTypeSubject.next(callType);
    if (contactName) this.activeCallNameSubject.next(contactName);
    this.callStateSubject.next('calling');
    this.pendingIceCandidates = [];

    // Generate unique call ID for the audio relay
    this.currentCallId = this.generateCallId();

    try {
      // Prime audio context during user gesture to avoid autoplay blocking
      this.primeAudioPlayback();

      await this.acquireMedia(callType);
      this.createPeerConnection();

      // Add local tracks to peer connection (for video best-effort)
      if (this.localStream && this.peerConnection) {
        this.localStream.getTracks().forEach(track => {
          this.peerConnection!.addTrack(track, this.localStream!);
        });
      }

      // Create and send offer with callId
      const offer = await this.peerConnection!.createOffer({
        offerToReceiveAudio: true,
        offerToReceiveVideo: callType === 'Video'
      });
      await this.peerConnection!.setLocalDescription(offer);

      const offerPayload = JSON.stringify({
        sdp: { type: offer.type, sdp: offer.sdp },
        callType,
        callId: this.currentCallId
      });

      await this.hubConnection!.invoke('SendOffer', targetUserId, offerPayload);

      // Start audio relay immediately (caller side)
      this.startAudioRelay();

      // Start video relay for video calls
      if (callType === 'Video') {
        this.startVideoRelay();
      }
    } catch (err) {
      console.error('Error starting call:', err);
      this.callStateSubject.next('failed');
      this.cleanupCall();
    }
  }

  async acceptCall(callerId: number, offerJson: string, callerName?: string): Promise<void> {
    this.currentTargetUserId = callerId;
    this.pendingIceCandidates = [];
    if (callerName) this.activeCallNameSubject.next(callerName);

    try {
      const offer = JSON.parse(offerJson);
      this.currentCallType = offer.callType || 'Audio';
      this.activeCallTypeSubject.next(this.currentCallType);
      this.currentCallId = offer.callId || this.generateCallId();

      // Prime audio context during user gesture to avoid autoplay blocking
      this.primeAudioPlayback();

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

      // Create and send answer with callId
      const answer = await this.peerConnection!.createAnswer();
      await this.peerConnection!.setLocalDescription(answer);

      const answerPayload = JSON.stringify({
        sdp: { type: answer.type, sdp: answer.sdp },
        callId: this.currentCallId
      });

      await this.hubConnection!.invoke('SendAnswer', callerId, answerPayload);
      this.callStateSubject.next('connected');

      // Start audio relay (callee side)
      this.startAudioRelay();

      // Start video relay for video calls
      if (this.currentCallType === 'Video') {
        this.startVideoRelay();
      }
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
        return !audioTrack.enabled;
      }
    }
    return false;
  }

  toggleVideo(): boolean {
    if (this.localStream) {
      const videoTrack = this.localStream.getVideoTracks()[0];
      if (videoTrack) {
        videoTrack.enabled = !videoTrack.enabled;
        return !videoTrack.enabled;
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
      console.warn('Failed to acquire media:', err);
      if (callType === 'Video') {
        try {
          this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
          this.localStreamSubject.next(this.localStream);
          return;
        } catch (audioErr) {
          console.warn('Audio-only fallback also failed:', audioErr);
        }
      }
      // Proceed without local media - can still receive remote audio via relay
      console.warn('Proceeding without local media devices');
      this.localStream = null;
      this.localStreamSubject.next(null);
    }
  }

  private generateCallId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substring(2, 8);
  }

  private primeAudioPlayback(): void {
    try {
      if (!this.playbackAudioCtx) {
        this.playbackAudioCtx = new AudioContext({ sampleRate: this.RELAY_SAMPLE_RATE });
      }
      this.playbackAudioCtx.resume();

      const buf = this.playbackAudioCtx.createBuffer(1, 1, this.RELAY_SAMPLE_RATE);
      const src = this.playbackAudioCtx.createBufferSource();
      src.buffer = buf;
      src.connect(this.playbackAudioCtx.destination);
      src.start(0);
    } catch (e) {
      console.warn('Failed to prime audio playback:', e);
    }
  }

  // ---- Audio Relay (WebSocket-based, replaces TURN for audio) ----

  private startAudioRelay(): void {
    if (this.relayWs) return;
    if (!this.currentCallId) return;

    const wsUrl = this.serverConfig.resolveWsUrl(`/ws/relay?callId=${encodeURIComponent(this.currentCallId)}`);
    console.log('Connecting to audio relay:', wsUrl);

    this.relayWs = new WebSocket(wsUrl);
    this.relayWs.binaryType = 'arraybuffer';

    this.relayWs.onopen = () => {
      console.log('Audio relay connected for call', this.currentCallId);
      this.startAudioCapture();
    };

    this.relayWs.onmessage = (event: MessageEvent) => {
      if (event.data instanceof ArrayBuffer) {
        this.playReceivedAudio(event.data);
      }
    };

    this.relayWs.onerror = (err) => {
      console.error('Audio relay WebSocket error:', err);
    };

    this.relayWs.onclose = () => {
      console.log('Audio relay disconnected');
    };
  }

  private startAudioCapture(): void {
    if (!this.localStream) return;

    try {
      this.captureAudioCtx = new AudioContext({ sampleRate: this.RELAY_SAMPLE_RATE });
      const source = this.captureAudioCtx.createMediaStreamSource(this.localStream);

      this.captureProcessor = this.captureAudioCtx.createScriptProcessor(
        this.RELAY_BUFFER_SIZE, 1, 1
      );

      this.captureProcessor.onaudioprocess = (e: AudioProcessingEvent) => {
        if (!this.relayWs || this.relayWs.readyState !== WebSocket.OPEN) return;

        const audioTrack = this.localStream?.getAudioTracks()[0];
        if (!audioTrack || !audioTrack.enabled) return;

        const pcmFloat = e.inputBuffer.getChannelData(0);

        const int16 = new Int16Array(pcmFloat.length);
        for (let i = 0; i < pcmFloat.length; i++) {
          const s = Math.max(-1, Math.min(1, pcmFloat[i]));
          int16[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
        }

        this.relayWs.send(int16.buffer);
      };

      source.connect(this.captureProcessor);
      this.captureProcessor.connect(this.captureAudioCtx.destination);
      console.log('Audio capture started at', this.RELAY_SAMPLE_RATE, 'Hz');
    } catch (err) {
      console.error('Failed to start audio capture:', err);
    }
  }

  private playReceivedAudio(data: ArrayBuffer): void {
    if (!this.playbackAudioCtx) {
      this.playbackAudioCtx = new AudioContext({ sampleRate: this.RELAY_SAMPLE_RATE });
    }

    if (this.playbackAudioCtx.state === 'suspended') {
      this.playbackAudioCtx.resume();
    }

    const int16 = new Int16Array(data);
    const float32 = new Float32Array(int16.length);
    for (let i = 0; i < int16.length; i++) {
      float32[i] = int16[i] / 0x8000;
    }

    const buffer = this.playbackAudioCtx.createBuffer(1, float32.length, this.RELAY_SAMPLE_RATE);
    buffer.getChannelData(0).set(float32);

    const source = this.playbackAudioCtx.createBufferSource();
    source.buffer = buffer;
    source.connect(this.playbackAudioCtx.destination);

    const now = this.playbackAudioCtx.currentTime;
    if (this.nextPlayTime <= now) {
      this.nextPlayTime = now + 0.05;
    }
    source.start(this.nextPlayTime);
    this.nextPlayTime += buffer.duration;
  }

  private stopAudioRelay(): void {
    if (this.captureProcessor) {
      this.captureProcessor.disconnect();
      this.captureProcessor = null;
    }
    if (this.captureAudioCtx) {
      this.captureAudioCtx.close().catch(() => {});
      this.captureAudioCtx = null;
    }
    if (this.playbackAudioCtx) {
      this.playbackAudioCtx.close().catch(() => {});
      this.playbackAudioCtx = null;
    }
    if (this.relayWs) {
      this.relayWs.close();
      this.relayWs = null;
    }
    this.nextPlayTime = 0;
  }

  // ---- Video Relay (server-relayed compressed video via WebSocket) ----

  private startVideoRelay(): void {
    if (!this.currentCallId) return;
    const wsUrl = this.serverConfig.resolveWsUrl(`/ws/relay?callId=${this.currentCallId}-video`);

    this.videoRelayWs = new WebSocket(wsUrl);
    this.videoRelayWs.binaryType = 'arraybuffer';

    this.videoRelayWs.onopen = () => {
      console.log('Video relay connected for call', this.currentCallId);
      this.startVideoCapture();
      this.setupRemoteVideoPlayback();
    };

    this.videoRelayWs.onmessage = (event) => {
      if (event.data instanceof ArrayBuffer && event.data.byteLength > 0) {
        this.appendVideoBuffer(event.data);
      }
    };

    this.videoRelayWs.onerror = (err) => console.error('Video relay error:', err);
    this.videoRelayWs.onclose = () => console.log('Video relay disconnected');
  }

  private startVideoCapture(): void {
    if (!this.localStream) return;
    const videoTrack = this.localStream.getVideoTracks()[0];
    if (!videoTrack) return;

    const videoStream = new MediaStream([videoTrack]);

    const mimeType = MediaRecorder.isTypeSupported('video/webm;codecs=vp8')
      ? 'video/webm;codecs=vp8'
      : 'video/webm';

    try {
      this.mediaRecorder = new MediaRecorder(videoStream, {
        mimeType,
        videoBitsPerSecond: 200000 // 200kbps low bandwidth
      });
    } catch {
      console.warn('MediaRecorder not supported, video relay unavailable');
      return;
    }

    this.mediaRecorder.ondataavailable = (e: BlobEvent) => {
      if (e.data.size > 0 && this.videoRelayWs?.readyState === WebSocket.OPEN) {
        e.data.arrayBuffer().then(buf => {
          this.videoRelayWs?.send(buf);
        });
      }
    };

    this.mediaRecorder.start(300); // produce chunks every 300ms
    console.log('Video capture started via MediaRecorder');
  }

  private setupRemoteVideoPlayback(): void {
    this.remoteMediaSource = new MediaSource();
    const url = URL.createObjectURL(this.remoteMediaSource);

    this.remoteMediaSource.addEventListener('sourceopen', () => {
      const mimeType = 'video/webm;codecs=vp8';
      try {
        this.remoteSourceBuffer = this.remoteMediaSource!.addSourceBuffer(mimeType);
        this.remoteSourceBuffer.mode = 'sequence';
        this.remoteSourceBuffer.addEventListener('updateend', () => {
          this.isAppendingVideo = false;
          this.flushVideoBufferQueue();
        });
        console.log('Remote video MediaSource ready');
      } catch (e) {
        console.error('Failed to create SourceBuffer:', e);
      }
    });

    this.ngZone.run(() => {
      this.remoteVideoUrlSubject.next(url);
    });
  }

  private appendVideoBuffer(data: ArrayBuffer): void {
    this.videoBufferQueue.push(data);
    this.flushVideoBufferQueue();
  }

  private flushVideoBufferQueue(): void {
    if (this.isAppendingVideo || !this.remoteSourceBuffer || this.videoBufferQueue.length === 0) return;
    if (this.remoteMediaSource?.readyState !== 'open') return;

    this.isAppendingVideo = true;
    const buffer = this.videoBufferQueue.shift()!;
    try {
      this.remoteSourceBuffer.appendBuffer(buffer);
    } catch (e) {
      console.error('appendBuffer failed:', e);
      this.isAppendingVideo = false;
    }
  }

  private stopVideoRelay(): void {
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      try { this.mediaRecorder.stop(); } catch { /* ignore */ }
    }
    this.mediaRecorder = null;

    if (this.videoRelayWs) {
      this.videoRelayWs.close();
      this.videoRelayWs = null;
    }

    if (this.remoteMediaSource && this.remoteMediaSource.readyState === 'open') {
      try { this.remoteMediaSource.endOfStream(); } catch { /* ignore */ }
    }
    this.remoteMediaSource = null;
    this.remoteSourceBuffer = null;
    this.videoBufferQueue = [];
    this.isAppendingVideo = false;

    const url = this.remoteVideoUrlSubject.value;
    if (url) URL.revokeObjectURL(url);
    this.remoteVideoUrlSubject.next(null);
  }

  // ---- WebRTC PeerConnection (for video best-effort via STUN) ----

  private createPeerConnection(): void {
    const rtcCfg = this.getRtcConfig();
    console.log('Creating PeerConnection', this.serverConfig.isLocalMode ? '(local network, no STUN)' : '(STUN for NAT traversal)');
    this.peerConnection = new RTCPeerConnection(rtcCfg);

    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate && this.currentTargetUserId && this.hubConnection) {
        this.hubConnection.invoke(
          'SendIceCandidate',
          this.currentTargetUserId,
          JSON.stringify(event.candidate.toJSON())
        ).catch(err => console.error('Error sending ICE candidate:', err));
      }
    };

    this.peerConnection.ontrack = (event) => {
      console.log('WebRTC ontrack:', event.track.kind);
      this.ngZone.run(() => {
        if (event.streams && event.streams[0]) {
          this.remoteStream = event.streams[0];
        } else {
          if (!this.remoteStream) {
            this.remoteStream = new MediaStream();
          }
          this.remoteStream.addTrack(event.track);
        }
        this.remoteStreamSubject.next(this.remoteStream);
      });
    };

    this.peerConnection.onconnectionstatechange = () => {
      const state = this.peerConnection?.connectionState;
      console.log('PeerConnection state:', state);
      if (state === 'connected') {
        // WebRTC P2P connected! Audio/video flows directly between phones.
        // Stop relay since P2P is working (on same WiFi, this is direct).
        this.peerConnectionConnected = true;
        console.log('WebRTC P2P connected - audio/video flows directly between devices');
        this.stopAudioRelay();
        this.stopVideoRelay();

        // Play remote audio from the WebRTC peer connection
        this.playRemoteStreamAudio();
      }
    };

    this.peerConnection.oniceconnectionstatechange = () => {
      console.log('ICE connection state:', this.peerConnection?.iceConnectionState);
    };
  }

  private playRemoteStreamAudio(): void {
    if (!this.remoteStream) return;
    // Create an audio element to play remote audio from WebRTC P2P
    const audioEl = document.createElement('audio');
    audioEl.srcObject = this.remoteStream;
    audioEl.autoplay = true;
    audioEl.setAttribute('playsinline', '');
    audioEl.play().catch(err => console.warn('Remote audio autoplay blocked:', err));
    // Store reference for cleanup
    (this as any)._remoteAudioEl = audioEl;
  }

  private cleanupCall(): void {
    this.stopAudioRelay();
    this.stopVideoRelay();
    this.peerConnectionConnected = false;
    // Clean up remote audio element
    const audioEl = (this as any)._remoteAudioEl as HTMLAudioElement | undefined;
    if (audioEl) {
      audioEl.srcObject = null;
      (this as any)._remoteAudioEl = null;
    }

    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
      this.localStreamSubject.next(null);
    }

    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }

    this.remoteStream = null;
    this.remoteStreamSubject.next(null);
    this.currentTargetUserId = null;
    this.currentCallId = '';
    this.pendingIceCandidates = [];
  }
}
