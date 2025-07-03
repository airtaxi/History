<script setup lang="ts">
/**
 * @fileoverview PostCard 컴포넌트 - 게시글 표시 및 상호작용을 담당하는 메인 컴포넌트
 *  * 주요 기능:
 * - 게시글 내용 표시 (텍스트, 미디어, 프로필 링크 등)
 * - 반응 시스템 (Like, Awesome, Happy, Sad, Support)
 * - hover 툴팁으로 반응한 사용자 정보 표시
 * - 리포스트 및 원본 게시글 표시
 * - 게시글 수정/삭제 (권한 기반)
 * - 신고 기능
 */

import { defineProps, computed, ref, onMounted, onUnmounted } from 'vue';
import type { PostResponseDto } from '@/types';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import apiClient from '@/api';
import "./PostCard.css"

/**
 * 컴포넌트 Props 정의
 * @typedef {Object} PostCardProps
 * @property {PostResponseDto} post - 표시할 게시글 데이터
 * @property {Record<string, string>} [profileImageMap] - 사용자별 프로필 이미지 URL 맵
 * @property {boolean} [showActions] - 수정/삭제 버튼 표시 여부 (작성자에게만)
 */
const props = defineProps<{
  post: PostResponseDto;
  profileImageMap?: Record<string, string>;
  showActions?: boolean;
}>();

const emit = defineEmits(['open-detail']);

const goToUserProfile = (userId: string) => {
  router.push(`/user/${userId}`);
};

// === Store 및 Router 인스턴스 ===
const authStore = useAuthStore();  // 인증 정보 관리
const router = useRouter();        // 페이지 라우팅
const uiStore = useUiStore();      // UI 상태 관리
const totalReactions = computed(() => {
  return Object.values(reactionMap.value).reduce((sum, count) => sum + count, 0)
})
const showImageModal = ref(false);
const selectedImageUrl = ref('');
const showAccessDeniedModal = ref(false);
const deniedUserId = ref('');
const deniedUserNickname = ref('');

const openImageModal = (url: string) => {
  console.log('[모달] 이미지 URL:', url)
  selectedImageUrl.value = url;
  showImageModal.value = true;
};

const closeImageModal = () => {
  showImageModal.value = false;
  selectedImageUrl.value = '';
};

// === 컴포넌트 상태 관리 ===
/** @description 더보기 메뉴 열림/닫힘 상태 */
const isMenuOpen = ref(false);

/** @description 미디어 파일의 Blob URL 저장 (메모리 관리를 위한 캐싱) */
const mediaUrlMap = ref<Record<string, string>>({});

/** @description 반응 타입별 카운트 (Like: 3, Awesome: 1, ...) */
const reactionMap = ref<Record<string, number>>({});

/** @description 현재 사용자가 누른 반응 타입 ('Like' | 'Awesome' | null) */
const myReaction = ref<string | null>(null);

/**
 * @description 툴팁 표시용 반응별 사용자 정보
 * @example { Like: [{ userId: '123', nickname: '김철수', profileImageUrl: '...' }] }
 */
const reactionUsersMap = ref<Record<string, Array<{userId: string, nickname: string, profileImageUrl: string}>>>({});

/** @description 현재 마우스가 올려진 반응 타입 */
const hoveredReaction = ref<string | null>(null);

/** @description 툴팁의 화면 좌표 위치 */
const tooltipPosition = ref({ top: 0, left: 0 });

/** @description 반응 선택 팝업 표시 여부 */
const showReactionPopup = ref(false);

/** @description 반응 팝업의 화면 좌표 위치 */
const reactionPopupPosition = ref({ top: 0, left: 0 });

/** @description Long press 타이머 */
let longPressTimer: number | null = null;

/** @description Long press 진행 중 여부 */
const isLongPressing = ref(false);

/** @description 팝업 표시 직후 클릭 방지를 위한 플래그 */
const isClickDisabled = ref(false);

/**
 * 툴팁 표시 및 위치 계산
 *  * 마우스가 반응 버튼에 올려졌을 때 해당 버튼 위에 툴팁을 표시합니다.
 * getBoundingClientRect()를 사용해 버튼의 정확한 화면 위치를 계산하고,
 * Fixed positioning으로 viewport 기준 절대 좌표를 설정합니다.
 *  * @param {MouseEvent} event - 마우스 이벤트 객체 (target에서 버튼 위치 추출)
 * @param {string} reactionType - 반응 타입 ('Like' | 'Awesome' | 'Happy' | 'Sad' | 'Support')
 * @example
 * ```vue
 * <button @mouseenter="showTooltip($event, 'Like')">👍</button>
 * ```
 */
const showTooltip = (event: MouseEvent, reactionType: string) => {
  const rect = (event.target as HTMLElement).getBoundingClientRect();
  tooltipPosition.value = {
    top: rect.top - 10, // 버튼 위쪽에 표시 (10px 여백)
    left: rect.left + rect.width / 2 // 버튼 중앙 정렬
  };
  hoveredReaction.value = reactionType;
};

/**
 * 툴팁 숨기기
 *  * 마우스가 반응 버튼에서 벗어났을 때 툴팁을 숨깁니다.
 * hoveredReaction을 null로 설정하여 v-if 조건을 false로 만듭니다.
 */
const hideTooltip = () => {
  hoveredReaction.value = null;
};

/**
 * 반응 버튼 클릭 시 처리
 *  * 일반 클릭 시 바로 좋아요 반응을 추가합니다.
 *  * @param {MouseEvent} event - 클릭 이벤트 객체
 */
const handleReactionClick = (event: MouseEvent) => {
  event.stopPropagation();
  
  // Long press가 아닌 일반 클릭인 경우에만 좋아요 추가
  if (!isLongPressing.value) {
    postReaction('Like');
  }
};

/**
 * 반응 버튼 Long Press 시작
 *  * 마우스/터치 다운 시 타이머를 시작하여 500ms 후 팝업을 표시합니다.
 *  * @param {MouseEvent | TouchEvent} event - 마우스/터치 이벤트 객체
 */
const startLongPress = (event: MouseEvent | TouchEvent) => {
  event.stopPropagation();
  event.preventDefault(); // 기본 동작 방지
  
  isLongPressing.value = false;
  
  const target = event.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  
  longPressTimer = window.setTimeout(() => {
    isLongPressing.value = true;
    reactionPopupPosition.value = {
      top: rect.top - 80, // 버튼 위쪽에 더 높게 표시 (60 -> 80)
      left: rect.left + rect.width / 2
    };
    showReactionPopup.value = true;
    
    // 팝업 표시 후 300ms 동안 클릭 비활성화
    isClickDisabled.value = true;
    setTimeout(() => {
      isClickDisabled.value = false;
    }, 300);
  }, 500); // 500ms 후 팝업 표시
};

/**
 * 반응 버튼 Long Press 종료
 *  * 마우스/터치 업 시 타이머를 취소합니다.
 *  * @param {MouseEvent | TouchEvent} event - 마우스/터치 이벤트 객체
 */
const endLongPress = (event: MouseEvent | TouchEvent) => {
  event.stopPropagation();
  
  if (longPressTimer) {
    clearTimeout(longPressTimer);
    longPressTimer = null;
  }
  
  // 팝업이 열리지 않은 경우에만 플래그 리셋
  if (!showReactionPopup.value) {
    setTimeout(() => {
      isLongPressing.value = false;
    }, 100);
  }
};

/**
 * 반응 선택 팝업에서 반응 선택
 *  * 팝업에서 반응을 선택했을 때 호출됩니다.
 *  * @param {string} reactionType - 선택한 반응 타입
 */
const selectReaction = async (reactionType: string) => {
  // 클릭이 비활성화된 상태면 무시
  if (isClickDisabled.value) return;
  
  // 이모지 플로팅 애니메이션 생성
  createFloatingEmoji(reactionType);
  
  await postReaction(reactionType);
  showReactionPopup.value = false;
  isLongPressing.value = false;
};

/**
 * 플로팅 이모지 애니메이션 생성
 *  * @param {string} reactionType - 반응 타입
 */
const createFloatingEmoji = (reactionType: string) => {
  const emojiMap: Record<string, string> = {
    'Like': '❤️',
    'Awesome': '🔥',
    'Happy': '😄',
    'Sad': '😢',
    'Support': '💪'
  };
  
  const emoji = document.createElement('div');
  emoji.className = 'floating-emoji';
  emoji.textContent = emojiMap[reactionType] || '❤️';
  emoji.style.left = reactionPopupPosition.value.left + 'px';
  emoji.style.top = (reactionPopupPosition.value.top + 40) + 'px';
  
  document.body.appendChild(emoji);
  
  // 애니메이션 종료 후 제거
  setTimeout(() => {
    emoji.remove();
  }, 800);
};

/**
 * 반응 타입의 한글 레이블 반환
 *  * @param {string} reactionType - 반응 타입
 * @returns {string} 한글 레이블
 */
const getReactionLabel = (reactionType: string): string => {
  const labelMap: Record<string, string> = {
    'Like': '좋아요',
    'Awesome': '멋져요',
    'Happy': '기뻐요',
    'Sad': '슬퍼요',
    'Support': '힘내요'
  };
  
  return labelMap[reactionType] || '반응';
};

/**
 * 게시글 즉시 신고 (레거시 함수)
 *  * 사용자 확인 후 즉시 '스팸' 사유로 게시글을 신고합니다.
 * 신고 사유를 선택할 수 없는 간단한 버전으로, 현재는 더 발전된 
 * openReportDialog() 함수를 사용하는 것을 권장합니다.
 *  * @deprecated 대신 openReportDialog() → submitReport() 플로우 사용 권장
 * @async
 *  * @example
 * ```vue
 *  * <button @click="reportPost">신고</button>
 * ```
 */
const reportPost = () => {
  if (confirm('이 게시물을 신고하시겠습니까?')) {
    const payload = {
      type: 'Spam',               // ReportType enum 값 (예: Spam, Hate, Nudity 등)
      target: 'Post',             // ReportTarget enum 값 (Post 또는 Comment)
      associatedId: props.post.id // 신고 대상 ID (게시글 ID)
    };

    console.log('[신고 요청 데이터]', payload);

    apiClient.post('/api/Report', payload)
      .then(() => {
        alert('신고가 접수되었습니다.');
      })
      .catch((err) => {
        console.log('[신고 실패 응답]', err.response?.data?.errors);
        alert('신고 처리 중 오류가 발생했습니다.');
      });
  }
};

// 시간 포맷 함수 추가

function formatRelativeTime(dateString: string): string {
  const created = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - created.getTime();
  const diffMinutes = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMinutes / 60);

  if (diffMinutes < 1) return '방금 전';
  if (diffMinutes < 60) return `${diffMinutes}분 전`;
  if (diffHours < 12) return `${diffHours}시간 전`;

  // 12시간 이상이면 날짜와 시간만 출력
  return `${created.getFullYear()}-${(created.getMonth() + 1).toString().padStart(2, '0')}-${created.getDate().toString().padStart(2, '0')} ${created.getHours().toString().padStart(2, '0')}:${created.getMinutes().toString().padStart(2, '0')}`;
}

/**
 * 게시글 공유 (리포스트)
 *  * UI Store의 리포스트 에디터를 열어서 사용자가 이 게시글을 리포스트할 수 있도록 합니다.
 * 원본 게시글 정보가 에디터에 전달되어 미리보기로 표시됩니다.
 *  * @example
 * ```vue
 * <button @click="sharePost">🔗 공유</button>
 * ```
 */
 const openShareEditor = () => {
  // `uiStore`에 `openShareEditor` 같은 명확한 이름의 함수를 만들어 사용하는 것이 좋습니다.
  // 여기서는 기존 `openRepostEditor`를 그대로 사용한다고 가정합니다.
  uiStore.openShareEditor(props.post);
};

/**
 * 리포스트 (즉시 실행)
 * - 에디터를 열지 않고, 확인창 후 바로 리포스트 API를 호출합니다.
 */
const handleInstantRepost = async (event: MouseEvent) => {
  if (confirm('이 게시물을 리포스트하시겠습니까?')) {
    const button = event.currentTarget as HTMLElement;
    const icon = button.querySelector('.repost-icon');
    
    try {
      await apiClient.post(`/api/Post/${props.post.id}/repost`);
      
      if (icon) {
        icon.classList.add('repost-success');
        setTimeout(() => icon.classList.remove('repost-success'), 600);
      }
      
      alert('리포스트되었습니다.');
      // emit('post-action-complete');

    } catch (error: any) {
      console.error("리포스트 실패:", error);
      alert(`리포스트에 실패했습니다: ${error.response?.data || error.message}`);
    }
  }
};


/**
 * 미디어 파일의 Blob URL 생성
 *  * 서버에서 미디어 파일을 다운로드하여 브라우저에서 표시 가능한 Blob URL로 변환합니다.
 * 메모리 효율성을 위해 각 미디어 ID마다 한 번만 로드하여 캐싱합니다.
 *  * @param {string} mediaId - 서버에 저장된 미디어 파일의 고유 ID
 * @returns {Promise<string>} Blob URL 문자열 (실패 시 빈 문자열)
 *  * @async
 * @example
 * ```typescript
 * const imageUrl = await getMediaBlobUrl('abc123');
 * // 반환값: "blob:http://localhost:3000/abc123-def456"
 * ```
 */
const getMediaBlobUrl = async (mediaId: string) => {
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, {
      responseType: 'blob',
    });
    const blob = response.data;
    return URL.createObjectURL(blob);
  } catch (error) {
    console.warn('미디어 로딩 실패:', mediaId);
    return '';
  }
};

/**
 * 컴포넌트 마운트 시 초기화 작업
 *  * 1. 게시글의 모든 미디어 파일을 Blob URL로 변환하여 캐싱
 * 2. 서버에서 반응 정보를 로드하여 UI 상태 초기화
 *  * 이 과정은 사용자가 게시글을 볼 때 즉시 이미지/동영상이 표시되고
 * 정확한 반응 상태가 표시되도록 보장합니다.
 */
/**
 * 반응 팝업 외부 클릭 처리
 *  * 팝업이 열려있을 때 외부를 클릭하면 팝업을 닫습니다.
 */
const handleClickOutside = (event: MouseEvent) => {
  const target = event.target as HTMLElement;
  if (!target.closest('.reaction-popup') && !target.closest('.footer-btn')) {
    showReactionPopup.value = false;
  }
};

onMounted(async () => {
  // 디버깅: 게시글 콘텐츠 구조 확인
  console.log('[📌 게시글 전체 데이터]', props.post);
  console.log('🔍 게시글 콘텐츠 구조:', props.post.contents);
  props.post.contents.forEach((content, index) => {
    console.log(`📝 콘텐츠 ${index}:`, {
      type: content.$type,
      fullContent: content,
      // externalUrl 타입의 경우 모든 속성 확인
      ...(content.$type === 'externalUrl' && {
        url: content.url || content.Url,
        title: content.title || content.Title,
        description: content.description || content.Description,
        image: content.image || content.Image || content.imageUrl || content.ImageUrl || content.thumbnail || content.Thumbnail
      })
    });
  });
  
  // 미디어 URL 로드 (이미지, 동영상 등)
  for (const content of props.post.contents) {
    if (content.$type === 'media' && (content.mediaId || content.thumbnailMediaId)) {
      const id = content.mediaId || content.thumbnailMediaId;
      mediaUrlMap.value[id] = await getMediaBlobUrl(id);
    }
  }
  
  // 리포스트인 경우 원본 게시글의 미디어도 로드
  if ((props.post as any).parentPost) {
    console.log('🔍 리포스트 원본 게시글 미디어 로드 중...');
    const parentPost = (props.post as any).parentPost;
    if (parentPost.contents && Array.isArray(parentPost.contents)) {
      for (const content of parentPost.contents) {
        if ((content as any).$type === 'media' && ((content as any).mediaId || (content as any).thumbnailMediaId)) {
          const id = (content as any).mediaId || (content as any).thumbnailMediaId;
          console.log('📷 원본 게시글 미디어 ID:', id);
          mediaUrlMap.value[id] = await getMediaBlobUrl(id);
        }
      }
    }
  }
  
  // 반응 정보 로드 (카운트, 내 반응, 툴팁용 사용자 정보)
  await loadReactionData();
  
  // 외부 클릭 리스너 추가
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  // 외부 클릭 리스너 제거
  document.removeEventListener('click', handleClickOutside);
});

/**
 * 게시글 편집 권한 확인
 *  * 현재 사용자가 이 게시글을 수정/삭제할 수 있는지 확인합니다.
 * showActions prop이 true이고, 로그인한 사용자이며, 게시글 작성자인 경우에만 true를 반환합니다.
 *  * @returns {boolean} 편집 권한 여부
 */
const canEdit = computed(() => {
  return props.showActions === true && authStore.user && authStore.user.userId === props.post.user.userId;
});

/**
 * 더보기 메뉴 토글
 *  * 게시글 우측 상단의 "..." 버튼 클릭 시 수정/삭제 메뉴를 표시/숨김 처리합니다.
 * stopPropagation()으로 게시글 클릭 이벤트와 충돌을 방지합니다.
 *  * @param {Event} e - 클릭 이벤트 객체
 */
const toggleMenu = (e: Event) => {
  e.stopPropagation();
  isMenuOpen.value = !isMenuOpen.value;
};

/**
 * 게시글 수정 페이지로 이동
 *  * 현재 게시글의 ID를 사용하여 EditPostView로 라우팅합니다.
 */
const goToEditPage = () => {
  router.push(`/post/edit/${props.post.id}`);
};

/**
 * 내 게시글 삭제
 *  * 사용자 확인 후 서버에서 게시글을 삭제하고 이전 페이지로 돌아갑니다.
 * 삭제는 되돌릴 수 없는 작업이므로 confirm 대화상자로 한 번 더 확인합니다.
 *  * @async
 */
const deleteMyPost = () => {
  if (confirm('정말 삭제하시겠습니까?')) {
    apiClient.delete(`/api/Post/${props.post.id}`)
      .then(() => {
        alert('삭제되었습니다.');
        router.back();
      })
      .catch((err) => {
        console.error('삭제 실패:', err);
        alert('삭제에 실패했습니다.');
      });
  }
};

function splitTextWithLinks(text: string): Array<{ text: string; isLink: boolean }> {
  const urlRegex = /(https?:\/\/[^\s]+)/g;
  const result: Array<{ text: string; isLink: boolean }> = [];

  let lastIndex = 0;
  let match;

  while ((match = urlRegex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      result.push({ text: text.slice(lastIndex, match.index), isLink: false });
    }
    result.push({ text: match[0], isLink: true });
    lastIndex = match.index + match[0].length;
  }

  if (lastIndex < text.length) {
    result.push({ text: text.slice(lastIndex), isLink: false });
  }

  return result;
}

/**
 * 텍스트에서 @멘션과 링크를 감지하여 분리
 *  * @handle 형식의 멘션과 URL을 찾아서 각각 다른 타입으로 분리합니다.
 *  * @param {string} text - 원본 텍스트
 * @returns {Array} 분리된 텍스트 청크 배열
 */
function splitTextWithLinksAndMentions(text: string): Array<{ text: string; type: 'text' | 'link' | 'mention' }> {
  // 향상된 URL 감지 정규식: http(s)://, www., 도메인 패턴 등을 감지
  const urlRegex = /(?:https?:\/\/[^\s]+)|(?:www\.[^\s]+)|(?:[a-zA-Z0-9][a-zA-Z0-9-]*(?:\.[a-zA-Z0-9][a-zA-Z0-9-]*)+(?:\/[^\s]*)?)/g;
  // 공백을 포함한 닉네임 지원 (@닉네임 형태)
  const mentionRegex = /@[a-zA-Z0-9_가-힣\s]+/g;
  
  // 모든 매치를 찾아서 위치와 함께 저장
  const matches: Array<{ text: string; type: 'link' | 'mention'; index: number; length: number }> = [];
  
  let match;
  while ((match = urlRegex.exec(text)) !== null) {
    let url = match[0];
    // www.로 시작하는 경우 https:// 추가
    if (url.startsWith('www.')) {
      url = url;
    }
    matches.push({ text: url, type: 'link', index: match.index, length: match[0].length });
  }
  
  while ((match = mentionRegex.exec(text)) !== null) {
    matches.push({ text: match[0], type: 'mention', index: match.index, length: match[0].length });
  }
  
  // 위치순으로 정렬
  matches.sort((a, b) => a.index - b.index);
  
  const result: Array<{ text: string; type: 'text' | 'link' | 'mention' }> = [];

  let lastIndex = 0;
  
  // 매치를 순서대로 처리
  for (const match of matches) {
    // 매치 이전의 일반 텍스트 추가
    if (match.index > lastIndex) {
      result.push({ text: text.slice(lastIndex, match.index), type: 'text' });
    }
    
    // 매치된 항목 추가
    result.push({ text: match.text, type: match.type });
    
    lastIndex = match.index + match.length;
  }

  // 마지막 매치 이후의 텍스트 추가
  if (lastIndex < text.length) {
    result.push({ text: text.slice(lastIndex), type: 'text' });
  }

  return result;
}

/**
 * 서버에서 최신 반응 데이터를 로드하여 UI 상태를 동기화
 *  * 게시글의 모든 반응 정보를 서버에서 가져와서 다음을 업데이트합니다:
 * - 반응별 카운트 (reactionMap)
 * - 현재 사용자의 반응 (myReaction)  
 * - 툴팁용 사용자 정보 (reactionUsersMap)
 *  * 서버 응답의 postReactions 배열을 파싱하여 클라이언트 상태로 변환합니다.
 *  * @async
 * @throws {Error} API 호출 실패 시 콘솔에 경고 로그 출력 (UI는 기존 상태 유지)
 *  * @example
 * ```typescript
 * // 컴포넌트 마운트 시 또는 반응 처리 후 호출
 * await loadReactionData();
 * ```
 */
const loadReactionData = async () => {
  try {
    const response = await apiClient.get(`/api/Post/${props.post.id}`);
    console.log('[반응 정보 로드]', response.data);
    
    // 서버 응답 구조 확인
    const postData = response.data;
    console.log('[postReactions 데이터]', postData.postReactions);
    
    // postReactions 배열에서 반응 카운트와 내 반응 추출
    const postReactions = postData.postReactions || [];
    const counts: Record<string, number> = {};
    const usersMap: Record<string, Array<{userId: string, nickname: string, profileImageUrl: string}>> = {};
    let currentUserReaction: string | null = null;
    
    // postReactions 배열을 순회하면서 반응 타입별로 카운트 계산 및 사용자 정보 수집
    postReactions.forEach((reaction: any) => {
      console.log('[개별 반응]', reaction);
      
      const reactionType = reaction.type || reaction.reactionType;
      const user = reaction.user;
      
      if (reactionType && user) {
        // 카운트 증가
        counts[reactionType] = (counts[reactionType] || 0) + 1;
        
        // 툴팁용 사용자 정보 수집
        if (!usersMap[reactionType]) {
          usersMap[reactionType] = [];
        }
        
        // 프로필 이미지 URL 결정 (props에서 제공된 맵 우선 사용)
        let profileImageUrl = '/src/assets/images/default_profile_image.jpg';
        if (user.profileThumbnailMediaId && props.profileImageMap?.[user.userId]) {
          profileImageUrl = props.profileImageMap[user.userId];
        }
        
        usersMap[reactionType].push({
          userId: user.userId,
          nickname: user.nickname || user.handle || 'Unknown',
          profileImageUrl
        });
        
        // 내 반응인지 확인 (현재 사용자 ID와 비교)
        if (user.userId === authStore.user?.userId) {
          currentUserReaction = reactionType;
        }
      }
    });
    
    // 상태 업데이트
    reactionMap.value = counts;
    myReaction.value = currentUserReaction;
    reactionUsersMap.value = usersMap;
    
    console.log('[반응 상태 업데이트]', {
      counts: reactionMap.value,
      myReaction: myReaction.value,
      usersMap: reactionUsersMap.value
    });
  } catch (err) {
    console.warn('반응 정보 로딩 실패:', err);
  }
};

/**
 * 게시글 반응 처리 함수 (핵심 로직)
 *  * 사용자가 반응 버튼을 클릭했을 때 호출되는 메인 함수입니다.
 * Optimistic Update 패턴을 사용하여 즉시 UI를 업데이트한 후 서버와 동기화합니다.
 *  * 처리 시나리오:
 * 1. 같은 반응 재클릭: 반응 해제 (API 1회 호출)
 * 2. 다른 반응으로 변경: 기존 반응 제거 + 새 반응 추가 (API 2회 호출)
 * 3. 새로운 반응 추가: 반응 추가 (API 1회 호출)
 *  * @param {string} newType - 새로 선택할 반응 타입
 * @param {string} newType.Like - 좋아요 👍
 * @param {string} newType.Awesome - 멋져요 🔥
 * @param {string} newType.Happy - 기뻐요 😄
 * @param {string} newType.Sad - 슬퍼요 😢
 * @param {string} newType.Support - 힘내요 💪
 *  * @async
 * @throws {Error} API 호출 실패 시 자동으로 원래 상태로 롤백
 *  * @example
 * ```vue
 * <button @click="postReaction('Like')">👍 좋아요</button>
 * ```
 */
const postReaction = async (newType: string) => {
  const previousReaction = myReaction.value;
  const originalReactionMap = { ...reactionMap.value };
  
  try {
    console.log(`[반응 처리] 이전: ${previousReaction}, 새로운: ${newType}`);
    console.log('[현재 반응 상태]', { reactionMap: reactionMap.value, myReaction: myReaction.value });
    
    if (previousReaction === newType) {
      // === 시나리오 1: 같은 반응 재클릭 → 해제 ===
      console.log(`[반응 API 호출 - 해제] POST /api/Post/${props.post.id}/reaction/${newType}`);
      
      // Optimistic Update: 즉시 UI에서 제거
      reactionMap.value[newType] = Math.max((reactionMap.value[newType] || 1) - 1, 0);
      myReaction.value = null;
      
      const response = await apiClient.post(`/api/Post/${props.post.id}/reaction/${newType}`);
      console.log('[반응 API 응답 - 해제]', response);
      
    } else if (previousReaction && previousReaction !== newType) {
      // === 시나리오 2: 다른 반응으로 변경 ===
      console.log(`[반응 변경] ${previousReaction} → ${newType}`);
      
      // Optimistic Update: 즉시 UI 업데이트
      reactionMap.value[previousReaction] = Math.max((reactionMap.value[previousReaction] || 1) - 1, 0);
      reactionMap.value[newType] = (reactionMap.value[newType] || 0) + 1;
      myReaction.value = newType;
      
      // 1차: 기존 반응 제거 (서버의 토글 방식 때문에 필요)
      console.log(`[반응 API 호출 - 기존 제거] POST /api/Post/${props.post.id}/reaction/${previousReaction}`);
      await apiClient.post(`/api/Post/${props.post.id}/reaction/${previousReaction}`);
      
      // 2차: 새 반응 추가
      console.log(`[반응 API 호출 - 새로 추가] POST /api/Post/${props.post.id}/reaction/${newType}`);
      const response = await apiClient.post(`/api/Post/${props.post.id}/reaction/${newType}`);
      console.log('[반응 API 응답 - 변경 완료]', response);
      
    } else {
      // === 시나리오 3: 새로운 반응 추가 ===
      console.log(`[반응 API 호출 - 추가] POST /api/Post/${props.post.id}/reaction/${newType}`);
      
      // Optimistic Update: 즉시 UI에 추가
      reactionMap.value[newType] = (reactionMap.value[newType] || 0) + 1;
      myReaction.value = newType;
      
      const response = await apiClient.post(`/api/Post/${props.post.id}/reaction/${newType}`);
      console.log('[반응 API 응답 - 추가]', response);
      console.log('[반응 후 상태]', { reactionMap: reactionMap.value, myReaction: myReaction.value });
    }
    
    // 최종 서버 데이터로 동기화 (실제 데이터와 일치 보장)
    await loadReactionData();
    
  } catch (err: any) {
    console.error('반응 처리 실패:', err);
    console.error('에러 응답:', err.response?.data);
    
    // 실패 시 원래 상태로 롤백 (사용자가 혼란스럽지 않도록)
    reactionMap.value = originalReactionMap;
    myReaction.value = previousReaction;
    
    alert('요청 처리에 실패했습니다. 잠시 후 다시 시도해 주세요.');
  }
};

/**
 * 게시글 상세 페이지로 이동
 *  * 현재 게시글의 PostDetailView로 라우팅하여 댓글 등 상세 정보를 볼 수 있도록 합니다.
 */
const goToPostDetail = () => {
  router.push(`/post/${props.post.id}`);
};

/**
 * 원본 게시글로 이동 (리포스트인 경우)
 *  * 현재 게시글이 리포스트인 경우, 원본 게시글의 상세 페이지로 이동합니다.
 * parentPost가 존재하는 경우에만 동작합니다.
 */
const goToOriginalPost = async () => {
  const parentPost = (props.post as any).parentPost;

  if (!parentPost) return;

  const parentPostId = parentPost.id || parentPost;

  try {
    await apiClient.get(`/api/Post/${parentPostId}`);
    router.push(`/post/${parentPostId}`);
  } catch (err: any) {
    if (err.response && err.response.status === 403) {
      deniedUserId.value = parentPost.user?.userId || '';  // 유저 ID
      deniedUserNickname.value = parentPost.user?.nickname || '작성자';  // 닉네임
      showAccessDeniedModal.value = true;
    } else {
      alert('게시글을 불러오는 중 문제가 발생했습니다.');
    }
  }
};

// === 신고 시스템 관련 상태 및 함수 ===
/** @description 신고 모달 표시 여부 */
const showReportModal = ref(false);

/** @description 선택된 신고 사유 (기본값: 성인물) */
const selectedReason = ref('ExplicitContent');



/**
 * 신고 대화상자 열기
 *  * 신고 모달을 표시하여 사용자가 신고 사유를 선택할 수 있도록 합니다.
 */
const openReportDialog = () => {
  showReportModal.value = true;
};

/**
 * 신고 취소
 *  * 신고 모달을 닫고 선택된 신고 사유를 초기값으로 리셋합니다.
 */
const cancelReport = () => {
  showReportModal.value = false;
  selectedReason.value = 'ExplicitContent';
};

/**
 * 신고 제출
 *  * 선택된 신고 사유로 서버에 신고 요청을 전송합니다.
 * 성공 시 모달을 닫고, 실패 시 적절한 오류 메시지를 표시합니다.
 *  * @async
 * @throws {Error} 409 상태 코드인 경우 "이미 신고한 게시물" 메시지 표시
 */
const submitReport = () => {
  const payload = {
    type: selectedReason.value,
    target: 'Post',
    associatedId: props.post.id,
  };

  console.log('[신고 요청 데이터]', payload);

  apiClient.post('/api/Report', payload)
    .then(() => {
      alert('신고가 접수되었습니다.');
      showReportModal.value = false;
    })
    .catch((err) => {
      const status = err.response?.status;
      const errorMsg = err.response?.data?.title || '신고 처리 중 오류가 발생했습니다.';

      if (status === 409) {
        alert('이미 신고한 게시물이에요.');
      } else {
        console.log('[신고 실패 응답]', err.response?.data?.errors);
        alert(errorMsg);
      }
    });
};

/**
 * @멘션 클릭 시 해당 유저 프로필로 이동
 *  * @handle 형식의 멘션에서 handle을 추출하여 프로필 페이지로 라우팅합니다.
 *  * @param {string} mentionText - @를 포함한 멘션 텍스트 (예: @john123)
 */
const navigateToProfile = async (mentionText: string) => {
  // @를 제거하고 handle만 추출
  const nickname = mentionText.substring(1);
  
  try {
    // handle로 사용자 검색
    const response = await apiClient.get(`/api/User/nickname-search/${nickname}`);
    const users = response.data;
    
    // handle이 정확히 일치하는 사용자 찾기
    const user = users.find((u: any) => u.nickname === nickname);
    
    if (user) {
      router.push(`/user/${user.userId}`);
    } else {
      console.warn(`사용자를 찾을 수 없습니다: ${nickname}`);
    }
  } catch (error) {
    console.error('사용자 검색 실패:', error);
  }
};

/**
 * URL이 이미지 URL인지 판단하는 함수 (강화된 버전)
 * @param {string} url - 검사할 URL
 * @returns {boolean} 이미지 URL인지 여부
 */
const isImageUrl = (url: string): boolean => {
  if (!url || typeof url !== 'string') return false;
  
  const trimmedUrl = url.trim();
  
  // 1. 이미지 확장자 패턴 체크
  const imageExtensions = /\.(jpg|jpeg|png|gif|webp|bmp|svg|ico|avif|tiff|tif)(\?.*)?$/i;
  if (imageExtensions.test(trimmedUrl)) return true;
  
  // 2. 이미지 서비스 도메인 체크
  const imageServices = [
    'dribbble.com',
    'imgur.com',
    'cloudinary.com',
    'unsplash.com',
    'pexels.com',
    'instagram.com',
    'pinimg.com',
    'googleusercontent.com',
    'githubusercontent.com',
    'flickr.com',
    'staticflickr.com',
    'photobucket.com',
    'imageshack.com',
    'tinypic.com',
    'deviantart.net',
    'twimg.com',
    'discordapp.com',
    'discord.com',
    'ibb.co',
    'imgbb.com',
    'i.imgur.com',
    'prnt.sc',
    'gyazo.com'
  ];
  
  const lowerUrl = trimmedUrl.toLowerCase();
  if (imageServices.some(service => lowerUrl.includes(service))) return true;
  
  // 3. URL에 이미지 관련 키워드가 포함되어 있는지 체크
  const imageKeywords = ['/image/', '/img/', '/photo/', '/picture/', '/media/', '/upload/', '/file/original'];
  if (imageKeywords.some(keyword => lowerUrl.includes(keyword))) return true;
  
  // 4. HTTP(S) URL인지 확인 (보안을 위해)
  if (!trimmedUrl.startsWith('http://') && !trimmedUrl.startsWith('https://')) return false;
  
  return false;
};
</script>

<template>
  <div v-if="post.isRepost" class="repost-wrapper">
    <div class="repost-label-standalone">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
        <path d="M23.77 15.67c-.292-.293-.767-.293-1.06 0l-2.22 2.22V7.65c0-2.068-1.683-3.75-3.75-3.75h-5.85c-.414 0-.75.336-.75.75s.336.75.75.75h5.85c1.24 0 2.25 1.01 2.25 2.25v10.24l-2.22-2.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.294-.292.294-.767.001-1.06zM3.5 16.44c.414 0 .75-.336.75-.75V5.44c0-1.24 1.01-2.25 2.25-2.25h5.85c.414 0 .75-.336.75-.75s-.336-.75-.75-.75H6.5c-2.068 0-3.75 1.682-3.75 3.75v10.24L.53 13.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.293-.292.293-.767 0-1.06s-.767-.293-1.06 0L3.5 15.44z"/>
      </svg>
      <span>{{ post.user.nickname }}님이 리포스트했습니다</span>
    </div>

    <template v-if="post.parentPost && post.parentPost.user">
      <div class="original-post-card" @click.stop="goToOriginalPost">
        <div class="original-post-author">
          <img
            :src="props.profileImageMap && post.parentPost.user.userId
                    ? props.profileImageMap[post.parentPost.user.userId]
                    : '/src/assets/images/default_profile_image.jpg'"
            class="original-author-avatar"
            @click.stop="goToUserProfile(post.parentPost.user.userId)"
          />
          <div class="original-author-info">
            <div class="original-author-name">{{ post.parentPost.user.nickname }}</div>
            <div class="original-post-timestamp">{{ new Date(post.parentPost.createdAt).toLocaleString() }}</div>
          </div>
        </div>

        <div class="original-post-content">
          <div v-for="(content, index) in post.parentPost.contents" :key="index">
            <template v-if="(content as any).$type === 'text'">
              <template v-if="isImageUrl((content as any).text)">
                <img 
                  :src="(content as any).text.trim()" 
                  alt="이미지" 
                  class="original-post-media external-image"
                  @click.stop="openImageModal((content as any).text.trim())"
                  @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
                />
              </template>
              <p v-else>{{ (content as any).text }}</p>
            </template>
            
            <div v-else-if="(content as any).$type === 'media' && ((content as any).mediaId || (content as any).thumbnailMediaId)">
              <template v-if="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]">
                <video
                  v-if="(content as any).mimeType && (content as any).mimeType.startsWith('video/')"
                  controls
                  class="original-post-media"
                >
                  <source :src="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]" :type="(content as any).mimeType" />
                  브라우저가 video 태그를 지원하지 않습니다.
                </video>
                <img
                  v-else
                  :src="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]"
                  :alt="(content as any).description || '게시물 이미지'"
                  class="original-post-media"
                />
              </template>
            </div>

            <div v-else-if="(content as any).$type === 'externalUrl'" class="external-link-container">
              <a :href="(content as any).sourceUrl || (content as any).SourceUrl || (content as any).url || (content as any).Url" target="_blank" rel="noopener noreferrer" class="external-link" @click.stop>
                <div class="link-preview small" :class="{ 'has-image': !!(content as any).thumbnailImageUrl || !!(content as any).ThumbnailImageUrl || !!(content as any).image || !!(content as any).Image }">
                  <img 
                    v-if="(content as any).thumbnailImageUrl || (content as any).ThumbnailImageUrl || (content as any).image || (content as any).Image"
                    :src="(content as any).thumbnailImageUrl || (content as any).ThumbnailImageUrl || (content as any).image || (content as any).Image"
                    :alt="(content as any).title || (content as any).Title || '링크 미리보기'"
                    class="link-preview-image"
                    @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
                  />
                  <div class="link-info">
                    <div v-if="(content as any).title || (content as any).Title" class="link-title">
                      {{ (content as any).title || (content as any).Title }}
                    </div>
                    <div v-if="(content as any).description || (content as any).Description" class="link-description">
                      {{ (content as any).description || (content as any).Description }}
                    </div>
                    <div class="link-url">
                      <span class="link-icon">🔗</span>
                      <span class="link-text">{{ (content as any).sourceUrl || (content as any).SourceUrl || (content as any).url || (content as any).Url }}</span>
                    </div>
                  </div>
                </div>
              </a>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>

  <div v-else class="post-card" @click="goToPostDetail">
    <div class="post-author">
      <RouterLink :to="`/user/${post.user.userId}`" @click.stop>
        <img
          :src="props.profileImageMap?.[post.user.userId] || '/src/assets/images/default_profile_image.jpg'"
          alt="프로필"
          class="author-avatar"
        />
      </RouterLink>
      <div>
        <RouterLink :to="`/user/${post.user.userId}`" class="author-name" @click.stop>
          {{ post.user.nickname }}
        </RouterLink>
        <div class="post-timestamp">{{ formatRelativeTime(post.createdAt) }}</div>
      </div>
      <div v-if="canEdit" class="more-menu-container" @click.stop="toggleMenu">
        <button class="more-button">...</button>
        <div v-if="isMenuOpen" class="dropdown-menu">
          <div @click="goToEditPage">수정</div>
          <div @click="deleteMyPost">삭제</div>
          <button @click="openReportDialog">🚨 신고</button>
        </div>
      </div>
    </div>

    <div class="post-content-area">
      <div v-for="(content, index) in post.contents" :key="index">
        <p v-if="(content as any).$type === 'text'">
          <template v-for="(chunk, index) in splitTextWithLinksAndMentions((content as any).text)" :key="index">
            <a v-if="chunk.type === 'link'"
              :href="chunk.text.startsWith('www.') ? 'https://' + chunk.text : chunk.text"
              target="_blank"
              rel="noopener noreferrer"
              style="color: #0066cc; word-break: break-all;"
              @click.stop>
              {{ chunk.text }}
            </a>
            <span v-else-if="chunk.type === 'mention'"
              class="mention"
              @click.stop="navigateToProfile(chunk.text)">
              {{ chunk.text }}
            </span>
            <span v-else>{{ chunk.text }}</span>
          </template>
        </p>

        <div v-else-if="((content as any).$type === 'media') && ((content as any).mediaId || (content as any).thumbnailMediaId)">
          <template v-if="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]">
            <video v-if="(content as any).mimeType && (content as any).mimeType.startsWith('video/')" controls class="post-image">
              <source :src="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]" :type="(content as any).mimeType" />
              브라우저가 video 태그를 지원하지 않습니다.
            </video>
            <img
              v-else
              :src="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]"
              :alt="(content as any).description || '게시물 이미지'"
              class="post-image"
              @click.stop="openImageModal(mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId])"
            />
          </template>
          <p v-if="(content as any).description">{{ (content as any).description }}</p>
        </div>

        <div v-else-if="(content as any).$type === 'externalUrl'" class="external-link-container">
          <a :href="(content as any).sourceUrl || (content as any).SourceUrl || (content as any).url || (content as any).Url" target="_blank" rel="noopener noreferrer" class="external-link" @click.stop>
            <div class="link-preview" :class="{ 'has-image': !!(content as any).thumbnailImageUrl || !!(content as any).ThumbnailImageUrl || !!(content as any).image || !!(content as any).Image }">
              <img 
                v-if="(content as any).thumbnailImageUrl || (content as any).ThumbnailImageUrl || (content as any).image || (content as any).Image"
                :src="(content as any).thumbnailImageUrl || (content as any).ThumbnailImageUrl || (content as any).image || (content as any).Image"
                :alt="(content as any).title || (content as any).Title || '링크 미리보기'"
                class="link-preview-image"
                @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
              />
              <div class="link-info">
                <div v-if="(content as any).title || (content as any).Title" class="link-title">
                  {{ (content as any).title || (content as any).Title }}
                </div>
                <div v-if="(content as any).description || (content as any).Description" class="link-description">
                  {{ (content as any).description || (content as any).Description }}
                </div>
                <div class="link-url">
                  <span class="link-icon">🔗</span>
                  <span class="link-text">{{ (content as any).sourceUrl || (content as any).SourceUrl || (content as any).url || (content as any).Url }}</span>
                </div>
              </div>
            </div>
          </a>
        </div>

        <RouterLink
          v-else-if="(content as any).$type === 'profile'"
          :to="`/user/${(content as any).userId}`"
          class="mention"
          @click.stop>
          {{ (content as any).nickname }}
        </RouterLink>

        <div v-else-if="(content as any).$type === 'UploadContent'">
          <p style="color: red;">[이미지 처리 실패] {{ (content as any).FileName }}</p>
        </div>
      </div>
      
      <div v-if="post.parentPost" class="original-post-card" @click.stop="goToOriginalPost">
        <div class="original-post-author">
          <img
            :src="props.profileImageMap && post.parentPost?.user?.userId
                    ? props.profileImageMap[post.parentPost.user.userId]
                    : '/src/assets/images/default_profile_image.jpg'"
            class="original-author-avatar"
            @click.stop="goToUserProfile(post.parentPost.user.userId)"
          />
          <div class="original-author-info">
            <div class="original-author-name">{{ (post as any).parentPost.user.nickname }}</div>
            <div class="original-post-timestamp">{{ new Date((post as any).parentPost.createdAt).toLocaleString() }}</div>
          </div>
        </div>
        
        <div class="original-post-content">
          <div v-for="(content, index) in (post as any).parentPost.contents" :key="index">
            <template v-if="(content as any).$type === 'text'">
              <template v-if="isImageUrl((content as any).text)">
                <img 
                  :src="(content as any).text.trim()" 
                  :alt="'이미지'" 
                  class="original-post-media external-image"
                  @click.stop="openImageModal((content as any).text.trim())"
                  @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
                />
              </template>
              <p v-else>{{ (content as any).text }}</p>
            </template>
            
            <div v-else-if="((content as any).$type === 'media') && ((content as any).mediaId || (content as any).thumbnailMediaId)">
              <template v-if="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]">
                <video
                  v-if="(content as any).mimeType && (content as any).mimeType.startsWith('video/')"
                  controls
                  class="original-post-media"
                >
                  <source :src="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]" :type="(content as any).mimeType" />
                  브라우저가 video 태그를 지원하지 않습니다.
                </video>
                <img
                  v-else
                  :src="mediaUrlMap[(content as any).mediaId || (content as any).thumbnailMediaId]"
                  :alt="(content as any).description || '게시물 이미지'"
                  class="original-post-media"
                />
              </template>
            </div>

            <div v-else-if="(content as any).$type === 'externalUrl'" class="external-link-container">
              <a :href="(content as any).sourceUrl || (content as any).SourceUrl || (content as any).url || (content as any).Url" target="_blank" rel="noopener noreferrer" class="external-link" @click.stop>
                <div class="link-preview small" :class="{ 'has-image': !!(content as any).thumbnailImageUrl || !!(content as any).ThumbnailImageUrl || !!(content as any).image || !!(content as any).Image }">
                  <img 
                    v-if="(content as any).thumbnailImageUrl || (content as any).ThumbnailImageUrl || (content as any).image || (content as any).Image"
                    :src="(content as any).thumbnailImageUrl || (content as any).ThumbnailImageUrl || (content as any).image || (content as any).Image"
                    :alt="(content as any).title || (content as any).Title || '링크 미리보기'"
                    class="link-preview-image"
                    @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
                  />
                  <div class="link-info">
                    <div v-if="(content as any).title || (content as any).Title" class="link-title">
                      {{ (content as any).title || (content as any).Title }}
                    </div>
                    <div v-if="(content as any).description || (content as any).Description" class="link-description">
                      {{ (content as any).description || (content as any).Description }}
                    </div>
                    <div class="link-url">
                      <span class="link-icon">🔗</span>
                      <span class="link-text">{{ (content as any).sourceUrl || (content as any).SourceUrl || (content as any).url || (content as any).Url }}</span>
                    </div>
                  </div>
                </div>
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="post-footer">
      <button 
        @click.stop="handleReactionClick"
        @mousedown.stop="startLongPress"
        @mouseup.stop="endLongPress"
        @mouseleave.stop="endLongPress"
        @touchstart.stop="startLongPress"
        @touchend.stop="endLongPress"
        class="footer-btn" 
        :class="{ active: myReaction }">
        <span v-if="!myReaction" aria-hidden="true">🤍</span>
        <span v-else-if="myReaction === 'Like'" aria-hidden="true">❤️</span>
        <span v-else-if="myReaction === 'Awesome'" aria-hidden="true">🔥</span>
        <span v-else-if="myReaction === 'Happy'" aria-hidden="true">😄</span>
        <span v-else-if="myReaction === 'Sad'" aria-hidden="true">😢</span>
        <span v-else-if="myReaction === 'Support'" aria-hidden="true">💪</span>
        <span aria-hidden="true">{{ totalReactions }}</span>
      </button>

      <button @click.stop="goToPostDetail" class="footer-btn">
        <span aria-hidden="true">💬 {{ post.comments ? post.comments.length : 0 }}</span>
      </button>

      <button @click.stop="openShareEditor" class="footer-btn" title="공유하기">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
            <path d="M12 22v-9m-4 4 4-4 4 4"/>
            <path d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/>
        </svg>
        <span v-if="(post.sharedAndRepostedUsers ?? []).filter(u => !u.isRepost).length > 0">
          {{ post.sharedAndRepostedUsers?.filter(u => !u.isRepost).length }}
        </span>
      </button>

      <button @click.stop="handleInstantRepost" class="footer-btn repost-btn" title="리포스트하기">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="repost-icon">
          <path d="M17 1l4 4-4 4M3 23l-4-4 4-4M21 5H10a4 4 0 00-4 4v1M3 19h11a4 4 0 004-4v-1"/>
        </svg>
        <span v-if="(post.sharedAndRepostedUsers ?? []).filter(u => u.isRepost).length > 0" class="repost-count">
          {{ post.sharedAndRepostedUsers?.filter(u => u.isRepost).length }}
        </span>
      </button>
    </div>
  </div>

  <Teleport to="body">
    <div v-if="hoveredReaction && reactionUsersMap[hoveredReaction]?.length > 0" 
          class="reaction-tooltip"
          :style="{ 
            top: `${tooltipPosition.top}px`, 
            left: `${tooltipPosition.left}px` 
          }">
      <div class="tooltip-header">
        <span v-if="hoveredReaction === 'Like'">좋아요</span>
        <span v-else-if="hoveredReaction === 'Awesome'">멋져요</span>
        <span v-else-if="hoveredReaction === 'Happy'">기뻐요</span>
        <span v-else-if="hoveredReaction === 'Sad'">슬퍼요</span>
        <span v-else-if="hoveredReaction === 'Support'">힘내요</span>
      </div>
      <div class="tooltip-users">
        <div v-for="user in reactionUsersMap[hoveredReaction]" :key="user.userId" class="tooltip-user">
          <img :src="user.profileImageUrl" :alt="user.nickname" class="tooltip-avatar">
          <span class="tooltip-nickname">{{ user.nickname }}</span>
        </div>
      </div>
    </div>
  </Teleport>

  <Teleport to="body">
    <div v-if="showReactionPopup" 
          class="reaction-popup"
          :style="{ 
            top: `${reactionPopupPosition.top}px`, 
            left: `${reactionPopupPosition.left}px` 
          }"
          @click.stop>
      <button @click="selectReaction('Like')" class="popup-reaction-btn" :class="{ active: myReaction === 'Like' }" aria-label="좋아요 반응" :aria-pressed="myReaction === 'Like'">
        <span class="popup-emoji" aria-hidden="true">👍</span>
        <span class="popup-label">좋아요</span>
      </button>
      <button @click="selectReaction('Awesome')" class="popup-reaction-btn" :class="{ active: myReaction === 'Awesome' }" aria-label="멋져요 반응" :aria-pressed="myReaction === 'Awesome'">
        <span class="popup-emoji" aria-hidden="true">🔥</span>
        <span class="popup-label">멋져요</span>
      </button>
      <button @click="selectReaction('Happy')" class="popup-reaction-btn" :class="{ active: myReaction === 'Happy' }" aria-label="기뻐요 반응" :aria-pressed="myReaction === 'Happy'">
        <span class="popup-emoji" aria-hidden="true">😄</span>
        <span class="popup-label">기뻐요</span>
      </button>
      <button @click="selectReaction('Sad')" class="popup-reaction-btn" :class="{ active: myReaction === 'Sad' }" aria-label="슬퍼요 반응" :aria-pressed="myReaction === 'Sad'">
        <span class="popup-emoji" aria-hidden="true">😢</span>
        <span class="popup-label">슬퍼요</span>
      </button>
      <button @click="selectReaction('Support')" class="popup-reaction-btn" :class="{ active: myReaction === 'Support' }" aria-label="힘내요 반응" :aria-pressed="myReaction === 'Support'">
        <span class="popup-emoji" aria-hidden="true">💪</span>
        <span class="popup-label">힘내요</span>
      </button>
    </div>
  </Teleport>

  <!-- 접근 거부 모달 -->
  <div v-if="showAccessDeniedModal" class="access-denied-modal">
    <div class="modal-overlay" @click="showAccessDeniedModal = false"></div>
    <div class="modal-content">
      <p class="modal-text">
        친구공개 스토리입니다.<br />
        {{ deniedUserNickname }}님과 친구를 맺으면 스토리를 확인할 수 있습니다.
      </p>
      <div class="modal-actions">
        <button class="modal-cancel" @click="showAccessDeniedModal = false">취소</button>
        
        <button class="modal-visit" @click="router.push(`/user/${deniedUserId}`)">스토리 방문</button>
      </div>
    </div>
  </div>


  <div v-if="showReportModal" class="report-modal" @click.stop>
    <p>🚨 신고 사유를 선택해주세요:</p>
    <select v-model="selectedReason">
      <option value="ExplicitContent">성인물</option>
      <option value="CopyrightViolation">저작권 위반</option>
      <option value="IllegalContent">불법 콘텐츠</option>
      <option value="Other">기타</option>
    </select>
    <div class="report-actions">
      <button @click="submitReport">신고하기</button>
      <button @click="cancelReport">취소</button>
    </div>
  </div>

  <Teleport to="body">
    <div v-if="showImageModal" class="image-modal" @click="closeImageModal">
      <img :src="selectedImageUrl" alt="확대 이미지" class="modal-image" />
    </div>
  </Teleport>
</template>

<style scoped>
/* ================================== */
/* [수정된 부분] 외부 링크         */
/* ================================== */
.external-link-container {
  margin: 12px 0;
}

.external-link {
  text-decoration: none;
  color: inherit;
  display: block;
}

.link-preview {
  border: 1px solid #e1e5e9;
  border-radius: 12px;
  background: white;
  transition: all 0.2s ease;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.link-preview:hover {
  border-color: #d1d5db;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

/* 이미지가 있는 링크 미리보기 레이아웃 */
.link-preview.has-image {
  display: flex;
}

/* 큰 이미지 (일반 포스트) */
.link-preview.has-image:not(.small) {
  flex-direction: column;
}
.link-preview.has-image:not(.small) .link-preview-image {
  width: 100%;
  height: 200px;
  object-fit: cover;
}
.link-preview.has-image:not(.small) .link-info {
  padding: 16px;
}

/* 작은 이미지 (리포스트 안의 원본글) */
.link-preview.small.has-image {
  flex-direction: row;
  align-items: stretch;
}
.link-preview.small.has-image .link-preview-image {
  width: 100px;
  height: auto;
  object-fit: cover;
  flex-shrink: 0;
  border-right: 1px solid #e1e5e9;
}
.link-preview.small.has-image .link-info {
  padding: 12px;
}

/* 이미지가 없는 링크 미리보기 */
.link-preview:not(.has-image) {
  padding: 16px;
  background: #f8f9fa;
}
.link-preview.small:not(.has-image) {
  padding: 12px;
}

.link-preview-image {
  background: #f5f5f5; /* 이미지 로딩 전 배경색 */
}

.link-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0; /* flex-shrink 방지 */
}

.link-title {
  font-weight: 600;
  color: #212529;
  font-size: 1rem;
  line-height: 1.3;
  /* 2줄 이상일 경우 말줄임표 */
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.link-preview.small .link-title {
  font-size: 0.9rem;
  -webkit-line-clamp: 1; /* 작은 UI에선 1줄 */
}

/* [수정된 부분] .link-desc -> .link-description 으로 변경 및 스타일 보강 */
.link-description {
  color: #495057;
  font-size: 0.875rem;
  line-height: 1.4;
  /* 3줄 이상일 경우 말줄임표 */
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  margin: 0;
}
.link-preview.small .link-description {
  font-size: 0.8rem;
  -webkit-line-clamp: 2; /* 작은 UI에선 2줄 */
}

.link-url {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6c757d;
  font-size: 0.85rem;
  margin-top: 8px;
}
.link-icon {
  flex-shrink: 0;
  font-size: 14px;
}
.link-text {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}


/* ================================== */
/* 기존 스타일 (유지)           */
/* ================================== */
.repost-wrapper {
  background: white;
  border-radius: 8px;
  border: 1px solid #ddd;
  padding: 12px 16px;
  margin-bottom: 12px;
}

.repost-label-standalone {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6c757d;
  font-size: 0.85rem;
  font-weight: 500;
  margin-bottom: 8px;
}

.repost-label-standalone svg {
  color: #6c757d;
}

.mention {
  color: #ed664d;
  font-weight: 700;
  cursor: pointer;
  text-decoration: none;
}

.mention:hover {
  font-weight: 700;
  background-color: #fff0ed;
}

.post-card {
  background: white;
  border-radius: 8px;
  border: 1px solid #ddd;
  padding: 16px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.post-footer {
  display: flex;
  justify-content: space-around;
  border-top: 1px solid #eee;
  padding-top: 10px;
  margin-top: 16px;
}

.footer-btn {
  background: none;
  border: none;
  font-size: 14px;
  color: #666;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 8px 12px;
  border-radius: 20px;
  transition: background-color 0.2s ease;
}

.footer-btn:hover {
  background-color: #f8f9fa;
}

.footer-btn.active {
  color: #ed664d;
  font-weight: 600;
}

.repost-btn {
  position: relative;
}

.repost-btn .repost-icon {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.repost-btn:hover .repost-icon {
  stroke: #22c55e;
  transform: rotate(180deg);
}

.repost-btn.active .repost-icon {
  stroke: #22c55e;
}

.repost-count {
  font-size: 14px;
  font-weight: 600;
  margin-left: 2px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes repostSuccess {
  0% { transform: scale(1) rotate(0); }
  50% { transform: scale(1.2) rotate(180deg); }
  100% { transform: scale(1) rotate(360deg); }
}

.repost-success {
  animation: repostSuccess 0.6s ease-out;
}

.post-card:hover {
  background-color: #fafafa;
}

.post-author {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.author-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  object-fit: cover;
}

.author-name {
  color: #333;
  font-weight: 600;
  text-decoration: none;
  transition: color 0.2s;
}

.author-name:hover {
  color: #ed664d;
  text-decoration: none;
  cursor: pointer;
}

.post-timestamp {
  font-size: 0.8rem;
  color: #666;
}

.post-content-area {
  margin-bottom: 12px;
  word-break: break-word;
}

.post-text {
  line-height: 1.6;
  white-space: pre-wrap;
  margin: 0 0 1em 0;
}

.reaction-tooltip {
  position: fixed;
  transform: translateX(-50%) translateY(-100%);
  background: rgba(0, 0, 0, 0.92);
  color: white;
  border-radius: 8px;
  padding: 12px;
  z-index: 9999 !important;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
  min-width: 200px;
  max-width: 300px;
  animation: tooltipFadeIn 0.2s ease-out;
  pointer-events: none;
  margin-top: -8px;
}

.reaction-tooltip::after {
  content: '';
  position: absolute;
  top: 100%;
  left: 50%;
  transform: translateX(-50%);
  border: 6px solid transparent;
  border-top-color: rgba(0, 0, 0, 0.92);
}

.tooltip-header {
  font-weight: 600;
  margin-bottom: 8px;
  text-align: center;
  font-size: 14px;
  color: #ed664d;
}

.tooltip-users {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 200px;
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: rgba(255, 255, 255, 0.3) transparent;
}

.tooltip-user {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 0;
}

.tooltip-avatar {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.tooltip-nickname {
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

@keyframes tooltipFadeIn {
  from { opacity: 0; transform: translateX(-50%) translateY(-90%); }
  to { opacity: 1; transform: translateX(-50%) translateY(-100%); }
}

.more-menu-container {
  position: relative;
  margin-left: auto;
}

.more-button {
  background: none;
  border: none;
  font-size: 20px;
  cursor: pointer;
}

.dropdown-menu {
  position: absolute;
  top: 28px;
  right: 0;
  background: #fff;
  border: 1px solid #ccc;
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  z-index: 10;
  width: 120px;
  padding: 4px 0;
  text-align: left;
}

.dropdown-menu div {
  padding: 8px 12px;
  cursor: pointer;
  white-space: nowrap;
}

.dropdown-menu div:hover {
  background-color: #f2f2f2;
}

.post-image {
  max-width: 100%;
  max-height: 400px;
  object-fit: contain;
  border-radius: 8px;
  margin-top: 8px;
}

.report-modal {
  position: fixed;
  top: 30%;
  left: 50%;
  transform: translateX(-50%);
  background: #fff;
  border: 1px solid #999;
  border-radius: 10px;
  padding: 20px;
  z-index: 100;
  width: 280px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.2);
}

.report-modal select {
  width: 100%;
  margin: 10px 0;
  padding: 6px;
}

.report-actions {
  display: flex;
  justify-content: space-between;
  margin-top: 10px;
}

.report-actions button {
  flex: 1;
  margin: 0 4px;
  padding: 6px;
  cursor: pointer;
}

.original-post-card {
  border: 1px solid #e9ecef;
  border-radius: 8px;
  padding: 16px; 
  margin-top: 8px;
  background-color: #fff;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 1rem; 
  line-height: 1.6;
}

.original-post-card:hover {
  border-color: #ced4da;
  background-color: #f1f3f4;
}

.original-post-author {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.original-author-avatar {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  object-fit: cover;
}

.original-author-info {
  flex: 1;
}

.original-author-name {
  font-weight: 600;
  font-size: 0.85rem;
  color: #212529;
}

.original-post-timestamp {
  font-size: 0.75rem;
  color: #6c757d;
  margin-top: 1px;
}

.original-post-content {
  color: #495057;
  line-height: 1.4;
  font-size: 0.9rem;
}

.original-post-content p {
  margin: 0 0 6px 0;
}

.original-post-media {
  max-width: 100%;
  max-height: 400px;
  border-radius: 4px;
  object-fit: contain;
  margin-top: 6px;
}

.external-image {
  border: 1px solid #e9ecef;
  transition: all 0.2s ease;
}

.external-image:hover {
  border-color: #ed664d;
  transform: scale(1.02);
  box-shadow: 0 4px 12px rgba(237, 102, 77, 0.15);
}

.reaction-popup {
  position: fixed;
  transform: translateX(-50%);
  background: white;
  border-radius: 32px;
  padding: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  display: flex;
  gap: 4px;
  z-index: 9999;
  animation: popupSlideIn 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

@keyframes popupSlideIn {
  from { opacity: 0; transform: translateX(-50%) translateY(20px) scale(0.6); }
  to { opacity: 1; transform: translateX(-50%) translateY(0) scale(1); }
}

@keyframes emojiFloat {
  0% { opacity: 1; transform: translate(-50%, 0) scale(1); }
  100% { opacity: 0; transform: translate(-50%, -40px) scale(1.5); }
}

.floating-emoji {
  position: fixed;
  pointer-events: none;
  z-index: 10000;
  animation: emojiFloat 0.8s ease-out forwards;
  font-size: 24px;
}

.popup-reaction-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  background: none;
  border: none;
  padding: 8px 12px;
  border-radius: 24px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.popup-reaction-btn:hover {
  background-color: #f8f9fa;
  transform: scale(1.1);
}

.popup-reaction-btn.active {
  background-color: #fef7f5;
}

.popup-emoji {
  font-size: 24px;
}

.popup-label {
  font-size: 11px;
  color: #6c757d;
  font-weight: 500;
}

.image-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  cursor: zoom-out;
}

.modal-image {
  max-width: 90%;
  max-height: 90%;
  border-radius: 8px;
  box-shadow: 0 0 16px rgba(0, 0, 0, 0.4);
  object-fit: contain;
}

.access-denied-modal {
  position: fixed;
  z-index: 9999;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}
.modal-overlay {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
}
.modal-content {
  position: relative;
  background: white;
  padding: 24px;
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
  max-width: 320px;
  text-align: center;
}
.modal-text {
  font-size: 1rem;
  margin-bottom: 16px;
  color: #333;
  line-height: 1.5;
}
.modal-actions {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}
.modal-cancel,
.modal-visit {
  flex: 1;
  padding: 10px;
  font-weight: bold;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}
.modal-cancel {
  background: #ddd;
  color: #333;
}
.modal-visit {
  background: #ed664d;
  color: white;
}


</style>
