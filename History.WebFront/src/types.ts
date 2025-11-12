export interface UserResponseDto {
  userId: string;
  handle: string;
  nickname: string;
  profileThumbnailMediaId: string | null;
  backgroundThumbnailMediaId?: string | null;
  description?: string | null;
  friends?: UserResponseDto[];
}

// --------------------
// 콘텐츠 타입 정의
// --------------------
export interface TextContent {
  $type: 'text';
  text: string;
  Text?: string;
}

export interface MediaContent {
  $type: 'media';
  mediaId: string;
  thumbnailMediaId: string;
  mimeType: string;
  description: string | null;
}

export interface ProfileContent {
  $type: 'profile';
  userId: string;
  nickname: string;
}

export interface UploadContent {
  $type: 'upload' | 'UploadContent';
  FileName: string;
  Description?: string;
}

export interface ExternalUrlContent {
  $type: 'external' | 'externalUrl';
  url?: string;
  Url?: string;
  sourceUrl?: string;
  SourceUrl?: string;

  title?: string;
  Title?: string;

  description?: string;
  Description?: string;

  thumbnailUrl?: string;
  thumbnailImageUrl?: string;
  ThumbnailImageUrl?: string;

  image?: string;
  Image?: string;
}

export interface MediaGroupContent {
  $type: 'mediaGroup';
  media: MediaContent[];
}

export type AnyContent =
  | TextContent
  | MediaContent
  | ProfileContent
  | UploadContent
  | ExternalUrlContent
  | MediaGroupContent
  | any;

// --------------------
// 게시글 / 댓글 / 알림
// --------------------
export interface PostReaction {
  type: 'like' | 'awesome' | 'happy' | 'sad' | 'support';
  reactionType?: string; // ✅ 코드에서 reactionType도 쓰고 있어서 추가
  user: UserResponseDto;
}

export interface PostResponseDto {
  id: string;
  user: any; // 또는 UserResponseDto
  discoveryOption: string;
  contents: AnyContent[];
  comments: any[];
  commentsCount: number;
  postReactions: PostReaction[];
  commentPermission: string | null;
  disallowShare: boolean;
  isRepost: boolean;
  createdAt: string;
  modifiedAt: string | null;
  parentPost?: PostResponseDto;
  hashtags?: string[];
  discoveryOptionSelectedUserIds?: string[];
  sharedAndRepostedUsers?: {
    user: any;
    postId: string;
    isRepost: boolean;
    sharedAt: string;
  }[];
}

export interface NotificationResponseDto {
  id: string;
  type: string;
  user: UserResponseDto;
  title: string;
  body: string;
  imageUrl: string;
  data: {
    [key: string]: any;
  };
  createdAt: string;
}

export interface CommentResponseDto {
  id: string;
  user: UserResponseDto;
  contents: AnyContent[];
  createdAt: string;
  likedUsers?: UserResponseDto[];
}

export interface UserDto {
  userId: string;
  handle: string;
  nickname: string;
  profileThumbnailMediaId: string | null;
  backgroundThumbnailMediaId?: string | null;
  description?: string | null;
  friends?: UserDto[];
}
