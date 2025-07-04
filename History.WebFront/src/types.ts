export interface UserResponseDto {
  userId: string;
  handle: string;
  nickname: string;
  profileThumbnailMediaId: string | null;
  backgroundThumbnailMediaId?: string | null;
  description?: string | null;
  friends?: UserResponseDto[]; 
}

/**
 * 텍스트 콘텐츠를 나타내는 타입
 * 
 * 게시글이나 댓글의 텍스트 부분을 표현할 때 사용됩니다.
 * 
 * @interface TextContent
 */
// 각 컨텐츠 타입을 구별하기 위한 속성들을 추가합니다.
export interface TextContent {
  /** 콘텐츠 타입 식별자 */
  $type: 'text';
  /** 텍스트 내용 (클라이언트용) */
  text: string;
  /** 텍스트 내용 (API 응답용, 대문자 T) */
  Text?: string; // API 응답에서 사용하는 경우
}

/**
 * 미디어 콘텐츠(이미지, 비디오 등)를 나타내는 타입
 * 
 * 게시글에 첨부된 이미지나 비디오 파일 정보를 표현합니다.
 * 
 * @interface MediaContent
 */
export interface MediaContent {
  /** 콘텐츠 타입 식별자 */
  $type: 'media';
  /** 원본 미디어 파일의 미디어 ID */
  mediaId: string;
  /** 썸네일 이미지의 미디어 ID */
  thumbnailMediaId: string;
  /** 미디어 파일의 MIME 타입 (예: image/jpeg, video/mp4) */
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

export type AnyContent = TextContent | MediaContent | ProfileContent | UploadContent | any;

export interface PostResponseDto {
  id: string;
  user: any; // 또는 UserResponseDto
  discoveryOption: string;
  contents: any[];
  comments: any[];
  commentsCount: number;
  postReactions: any[];
  commentPermission: string | null;
  disallowShare: boolean;
  isRepost: boolean;
  createdAt: string;
  modifiedAt: string | null;
  parentPost?: PostResponseDto;

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