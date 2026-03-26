export interface UserDto {
  id: number;
  username: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  isOnline: boolean;
  lastSeen: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
}

export interface ReelDto {
  id: number;
  title: string;
  description?: string;
  videoUrl: string;
  thumbnailUrl?: string;
  durationSeconds: number;
  viewCount: number;
  likeCount: number;
  tags?: string;
  isDownloadable: boolean;
  fileSizeBytes: number;
  createdAt: string;
  creator: UserDto;
  isLikedByCurrentUser: boolean;
}

export interface ReelFeedResponse {
  reels: ReelDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface CallLogDto {
  id: number;
  caller: UserDto;
  receiver: UserDto;
  startedAt: string;
  endedAt?: string;
  durationSeconds: number;
  status: string;
  callType: string;
}

export interface ContactDto {
  id: number;
  contactUserId: number;
  displayName?: string;
  username: string;
  avatarUrl?: string;
  isOnline: boolean;
  lastSeen: string;
}

export interface OfflineReel {
  id: number;
  title: string;
  description?: string;
  thumbnailUrl?: string;
  durationSeconds: number;
  creatorName: string;
  downloadedAt: string;
  blob: Blob;
}
