<script setup lang="ts">
/**
 * @fileoverview CommentItem 컴포넌트 - 개별 댓글 표시 및 관리를 담당하는 메인 컴포넌트
 * 
 * 주요 기능:
 * - 댓글 내용 표시 (텍스트, 프로필 이미지, 작성 시간)
 * - 댓글 좋아요 기능
 * - 댓글 수정/삭제 (권한 기반)
 * - 사용자 멘션 기능
 * - PostCard.vue와 일관된 디자인
 * - 프로필 이미지 무한 로딩 방지
 * - 미디어 콘텐츠(이미지/비디오) 표시
 */

import { ref, computed, onMounted } from 'vue';
import type { CommentResponseDto } from '@/types';
import { useAuthStore } from '@/stores/auth';
import apiClient from '@/api';
import { useRouter, RouterLink } from 'vue-router';

/**
 * 컴포넌트 Props 정의
 * @typedef {Object} CommentItemProps
 * @property {CommentResponseDto} comment - 표시할 댓글 데이터
 * @property {string} profileImageUrl - 댓글 작성자의 프로필 이미지 URL
 */
const props = defineProps<{
  comment: CommentResponseDto;
  profileImageUrl: string;
}>();

/**
 * 컴포넌트 이벤트 정의
 * @typedef {Object} CommentItemEmits
 * @property {Function} mention-user - 사용자 멘션 시 발생 (nickname: string)
 * @property {Function} delete-comment - 댓글 삭제 시 발생 (commentId: string)
 * @property {Function} like-comment - 댓글 좋아요 시 발생 (commentId: string)
 * @property {Function} update-comment - 댓글 수정 시 발생 ({ commentId: string, newText: string })
 */
const emit = defineEmits(['mention-user', 'delete-comment', 'like-comment', 'update-comment']);

// === Store 인스턴스 ===
const authStore = useAuthStore(); // 인증 정보 관리
const router = useRouter();

// === 컴포넌트 상태 관리 ===
/** @description 댓글 수정 모드 여부 */
const isEditing = ref(false);

/** @description 수정할 텍스트 내용 */
const editedText = ref(props.comment.contents.find(c => c.$type === 'text')?.text || '');

/** @description 더보기 메뉴 열림/닫힘 상태 */
const isMenuOpen = ref(false);

/** @description 프로필 이미지 로딩 에러 플래그 (무한 재시도 방지용) */
const imageLoadError = ref(false);

/** @description 미디어 파일의 Blob URL 저장 (메모리 관리를 위한 캐싱) */
const mediaUrlMap = ref<Record<string, string>>({});

// === Computed Properties ===

/**
 * 현재 로그인한 사용자가 이 댓글의 작성자인지 확인
 * 
 * 수정/삭제 권한 표시 여부를 결정하는 핵심 로직입니다.
 * 로그인하지 않은 상태거나 다른 사용자의 댓글인 경우 false를 반환합니다.
 * 
 * @returns {boolean} 내 댓글이면 true, 아니면 false
 */
const isMyComment = computed(() => {
  return authStore.user?.userId === props.comment.user.userId;
});

/**
 * 현재 로그인한 사용자가 이 댓글에 좋아요를 눌렀는지 확인
 * 
 * 좋아요 버튼의 활성 상태를 표시하기 위해 사용됩니다.
 * likedUsers 배열에서 현재 사용자의 ID를 찾아 확인합니다.
 * 
 * @returns {boolean} 좋아요를 눌렀으면 true, 아니면 false
 */
const isLikedByMe = computed(() => {
    // likedUsers 배열에 내 userId가 있는지 확인
    return props.comment.likedUsers?.some(user => user.userId === authStore.user?.userId);
});

/**
 * 안전한 프로필 이미지 URL 반환
 * 
 * 프로필 이미지 로딩 실패나 URL이 없는 경우를 처리하여
 * 무한 재시도를 방지하고 기본 이미지를 표시합니다.
 * 
 * @returns {string} 표시할 프로필 이미지 URL (기본 이미지 포함)
 */
const safeProfileImageUrl = computed(() => {
  if (imageLoadError.value || !props.profileImageUrl || props.profileImageUrl === '') {
    return '/src/assets/images/default_profile_image.jpg';
  }
  return props.profileImageUrl;
});

// === 메소드(Methods) ===

/**
 * 프로필 이미지 로딩 에러 처리 함수
 * 
 * 이미지 로딩 실패 시 한 번만 기본 이미지로 교체하여 
 * 무한 재시도로 인한 성능 저하를 방지합니다.
 * 
 * @param {Event} event - 이미지 오류 이벤트 객체
 * 
 * @example
 * ```vue
 * <img :src="safeProfileImageUrl" @error="handleImageError" />
 * ```
 */
const handleImageError = (event: Event) => {
  const target = event.target as HTMLImageElement;
  if (!imageLoadError.value) {
    imageLoadError.value = true; // 한 번만 실행되도록 플래그 설정
    target.src = '/src/assets/images/default_profile_image.jpg';
  }
};

/**
 * 사용자 멘션 처리 함수
 * 
 * 댓글 작성자의 닉네임을 클릭했을 때 호출되어 
 * 새 댓글 작성 시 해당 사용자를 멘션합니다.
 * 
 * @emits mention-user 멘션할 사용자의 닉네임 전달
 * 
 * @example
 * ```vue
 * <span class="author-name" @click="mentionUser">김철수</span>
 * ```
 */
const mentionUser = () => {
  emit('mention-user', props.comment.user.nickname);
};

/**
 * 더보기 메뉴 토글 함수
 * 
 * 점 세개 버튼 클릭 시 수정/삭제 메뉴를 열고 닫습니다.
 * 이벤트 전파를 중단하여 댓글 전체 클릭과 분리합니다.
 * 
 * @param {Event} e - 클릭 이벤트 객체
 */
const toggleMenu = (e: Event) => {
  e.stopPropagation();
  isMenuOpen.value = !isMenuOpen.value;
};

/**
 * 댓글 수정 모드 시작
 * 
 * 수정 모드로 전환하여 사용자가 댓글 내용을 편집할 수 있게 합니다.
 * 기존 텍스트를 편집 필드에 로드하고 더보기 메뉴를 닫습니다.
 */
const startEdit = () => {
  editedText.value = props.comment.contents.find(c => c.$type === 'text')?.text || '';
  isEditing.value = true;
  isMenuOpen.value = false; // 메뉴 닫기
};

/**
 * 댓글 수정 취소
 * 
 * 수정 모드를 종료하고 원래 댓글 표시 상태로 돌아갑니다.
 * 편집된 내용은 저장되지 않습니다.
 */
const cancelEdit = () => {
  isEditing.value = false;
};

/**
 * 댓글 수정 내용 저장
 * 
 * 수정된 댓글 내용을 부모 컴포넌트에 전달하여 서버에 저장합니다.
 * 빈 내용인 경우 사용자에게 경고를 표시합니다.
 * 
 * @emits update-comment 댓글 ID와 수정된 텍스트를 객체로 전달
 * 
 * @example
 * ```typescript
 * // 이벤트 페이로드: { commentId: string, newText: string }
 * emit('update-comment', { commentId: 'abc123', newText: '수정된 댓글 내용' });
 * ```
 */
const saveComment = () => {
  if (!editedText.value.trim()) {
    alert('수정할 내용을 입력하세요.');
    return;
  }
  // commentId와 수정된 텍스트를 함께 부모 컴포넌트로 전달
  emit('update-comment', { commentId: props.comment.id, newText: editedText.value });
  isEditing.value = false; // 수정 모드 종료
};

/**
 * 댓글 좋아요 처리
 * 
 * 좋아요 버튼 클릭 시 부모 컴포넌트에 이벤트를 전달합니다.
 * 실제 API 호출은 부모 컴포넌트에서 처리합니다.
 * 
 * @emits like-comment 좋아요할 댓글의 ID 전달
 */
const likeComment = () => {
  emit('like-comment', props.comment.id);
};

/**
 * 댓글 삭제 처리
 * 
 * 사용자 확인 후 댓글 삭제를 요청합니다.
 * 확인 시에만 부모 컴포넌트에 삭제 이벤트를 전달하고 메뉴를 닫습니다.
 * 
 * @emits delete-comment 삭제할 댓글의 ID 전달
 */
const deleteComment = () => {
  if (confirm('정말로 이 댓글을 삭제하시겠습니까?')) {
    emit('delete-comment', props.comment.id);
    isMenuOpen.value = false;
  }
};

/**
 * 미디어 파일의 Blob URL 생성
 * 
 * 서버에서 미디어 파일을 다운로드하여 브라우저에서 표시 가능한 Blob URL로 변환합니다.
 * 메모리 효율성을 위해 각 미디어 ID마다 한 번만 로드하여 캐싱합니다.
 * 
 * @param {string} mediaId - 서버에 저장된 미디어 파일의 고유 ID
 * @returns {Promise<string>} Blob URL 문자열 (실패 시 빈 문자열)
 * 
 * @async
 * @example
 * ```typescript
 * const imageUrl = await getMediaBlobUrl('abc123');
 * // 반환값: "blob:http://localhost:3000/abc123-def456"
 * ```
 */
const getMediaBlobUrl = async (mediaId: string) => {
  try {
    const response = await apiClient.get(`/api/media/${mediaId}`, {
      responseType: 'blob',
    });
    const blob = response.data;
    return URL.createObjectURL(blob);
  } catch (error) {
    console.warn('댓글 미디어 로딩 실패:', mediaId);
    return '';
  }
};

/**
 * 텍스트에서 URL과 @멘션을 분리하여 배열로 반환
 * 
 * @param {string} text - 원본 텍스트
 * @returns {Array} 분리된 텍스트 청크 배열
 */
const splitTextWithLinksAndMentions = (text: string): Array<{ text: string; type: 'text' | 'link' | 'mention' }> => {
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
};

/**
 * @멘션 클릭 시 해당 유저 프로필로 이동
 * 
 * @닉네임 형식의 멘션에서 닉네임을 추출하여 프로필 페이지로 라우팅합니다.
 * 
 * @param {string} mentionText - @를 포함한 멘션 텍스트 (예: @김철수)
 */
const navigateToProfile = async (mentionText: string) => {
  // @를 제거하고 닉네임만 추출
  const nickname = mentionText.substring(1).trim();
  
  try {
    // 닉네임으로 사용자 검색
    const response = await apiClient.get(`/api/User/nickname-search/${encodeURIComponent(nickname)}`);
    const users = response.data;
    
    // 닉네임이 정확히 일치하는 사용자 찾기
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
 * 컴포넌트 마운트 시 초기화 작업
 * 
 * 댓글의 모든 미디어 파일을 Blob URL로 변환하여 캐싱합니다.
 * 사용자가 댓글을 볼 때 즉시 이미지/동영상이 표시되도록 보장합니다.
 */
onMounted(async () => {
  // 디버깅: 댓글 콘텐츠 구조 확인
  //console.log('🔍 댓글 콘텐츠 구조:', props.comment.contents);
  
  // 미디어 URL 로드 (이미지, 동영상 등)
  for (const content of props.comment.contents) {
    if (content.$type === 'media' && (content.mediaId || content.thumbnailMediaId)) {
      const id = content.mediaId || content.thumbnailMediaId;
      mediaUrlMap.value[id] = await getMediaBlobUrl(id);
    }
  }
});

function formatRelativeTime(dateString: string): string {
  const created = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - created.getTime();
  const diffMinutes = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMinutes / 60);

  if (diffMinutes < 1) return '방금 전';
  if (diffMinutes < 60) return `${diffMinutes}분 전`;
  if (diffHours < 12) return `${diffHours}시간 전`;

  return `${created.getFullYear()}-${(created.getMonth() + 1).toString().padStart(2, '0')}-${created.getDate().toString().padStart(2, '0')} ${created.getHours().toString().padStart(2, '0')}:${created.getMinutes().toString().padStart(2, '0')}`;
}

</script>

<template>
  <div class="comment-item">
  <!-- 왼쪽: 프로필 이미지 -->
  <RouterLink :to="`/user/${comment.user.userId}`" @click.stop>
    <img
      :src="safeProfileImageUrl"
      class="author-avatar"
      @error="handleImageError"
    />
  </RouterLink>

  <!-- 오른쪽 전체 -->
  <div class="comment-main">
    <!-- 🧩 상단: 닉네임, 시간, 좋아요, 더보기 -->
    <div class="comment-header">
      <div class="nickname-time">
        <span class="author-name" @click="mentionUser">{{ comment.user.nickname }}</span>
        <span class="comment-timestamp">{{ formatRelativeTime(comment.createdAt) }}</span>
      </div>
      
      <!-- 좋아요 + 더보기 메뉴 같이 오른쪽 끝 정렬 -->
      <div class="header-actions" style="display: flex; align-items: center; gap: 8px;">
        <button @click="likeComment" :class="['like-btn', { 'liked': isLikedByMe }]">
          ❤️ <span v-if="comment.likedUsers?.length">{{ comment.likedUsers.length }}</span>
        </button>

        <div v-if="isMyComment" class="more-menu-container" @click.stop="toggleMenu">
          <button class="more-button">⋯</button>
          <div v-if="isMenuOpen" class="dropdown-menu">
            <div @click.stop="startEdit">수정</div>
            <div @click.stop="deleteComment">삭제</div>
          </div>
        </div>
      </div>
    </div>

      <div v-if="!isEditing" class="comment-body">
        <template v-for="(content, index) in comment.contents" :key="index">
          <p v-if="content.$type === 'text'" class="comment-text">
            <template v-for="(chunk, chunkIndex) in splitTextWithLinksAndMentions(content.text)" :key="`${index}-${chunkIndex}`">
              <a v-if="chunk.type === 'link'"
                :href="chunk.text.startsWith('www.') ? 'https://' + chunk.text : chunk.text"
                target="_blank"
                rel="noopener noreferrer"
                class="comment-link"
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

        <RouterLink
          v-else-if="(content as any).$type === 'profile'"
          :to="`/user/${(content as any).userId}`"
          class="mention"
          @click.stop>
          {{ (content as any).nickname }}
        </RouterLink>
          
          <!-- 미디어 콘텐츠 렌더링 추가 -->
          <div v-else-if="content.$type === 'media' && (content.mediaId || content.thumbnailMediaId)" class="comment-media-container">
            <template v-if="mediaUrlMap[content.mediaId || content.thumbnailMediaId]">
              <video
                v-if="content.mimeType && content.mimeType.startsWith('video/')"
                controls
                class="comment-media"
              >
                <source :src="mediaUrlMap[content.mediaId || content.thumbnailMediaId]" :type="content.mimeType" />
                브라우저가 video 태그를 지원하지 않습니다.
              </video>
              <img
                v-else
                :src="mediaUrlMap[content.mediaId || content.thumbnailMediaId]"
                :alt="content.description || '댓글 이미지'"
                class="comment-media"
              />
            </template>
            <p v-if="content.description" class="media-description">{{ content.description }}</p>
          </div>
          
          <!-- 외부 링크 렌더링 추가 -->
          <div v-else-if="content.$type === 'externalUrl'" class="comment-link-container">
            <a :href="(content as any).sourceUrl || (content as any).SourceUrl || content.url || content.Url" target="_blank" rel="noopener noreferrer" class="comment-link-card" @click.stop>
              <div class="link-preview" :class="{ 'has-image': !!(content as any).thumbnailImageUrl || !!(content as any).ThumbnailImageUrl || !!(content as any).image || !!(content as any).Image }">
                <!-- 백엔드에서 제공한 이미지가 있으면 표시 -->
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
                    <span class="link-text">{{ (content as any).sourceUrl || (content as any).SourceUrl || content.url || content.Url }}</span>
                  </div>
                </div>
              </div>
            </a>
          </div>
          
          <!-- ProfileContent (멘션) 렌더링 -->
          <RouterLink
            v-else-if="content.$type === 'profile'"
            :to="`/user/${content.userId}`"
            class="mention"
            @click.stop>
            @{{ content.nickname }}
          </RouterLink>
        </template>
      </div>

      <div v-else class="edit-mode">
        <textarea v-model="editedText" class="edit-textarea" rows="3" placeholder="댓글을 수정하세요..."></textarea>
        <div class="edit-actions">
          <button @click="saveComment" class="save-btn">저장</button>
          <button @click="cancelEdit" class="cancel-btn">취소</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.comment-item {
  display: flex;
  align-items: flex-start;
  padding: 12px 16px;
  gap: 12px;
  border-bottom: 1px solid #eee;
}

.comment-item:hover {
  background-color: #f8f9fa;
}

.author-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.comment-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}


.comment-content {
  flex: 1;
  min-width: 0;
}

.comment-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.nickname-time {
  display: flex;
  align-items: center;
  gap: 8px;
}

.comment-author-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.author-name {
  font-weight: 600;
  font-size: 0.9rem;
  color: #212529;
  cursor: pointer;
}

.author-name:hover {
  color: #ed664d;
}

.comment-timestamp {
  font-size: 0.75rem;
  color: #6c757d;
}

.more-menu-container {
  position: relative;
  flex-shrink: 0;
}

.more-button {
  background: none;
  border: none;
  color: #6c757d;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 16px;
  line-height: 1;
  transition: all 0.2s;
}

.more-button:hover {
  background-color: #e9ecef;
  color: #495057;
}

.dropdown-menu {
  position: absolute;
  top: 24px; 
  right: 0;
  background: white;
  border: 1px solid #dee2e6;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  z-index: 1000;
  min-width: 120px;
  padding: 4px 0;
}

.dropdown-menu div {
  padding: 8px 16px;
  cursor: pointer;
  font-size: 0.85rem;
  color: #495057;
  transition: background-color 0.2s;
}

.dropdown-menu div:hover {
  background-color: #f8f9fa;
}

.dropdown-menu div:first-child {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
}

.dropdown-menu div:last-child {
  border-bottom-left-radius: 8px;
  border-bottom-right-radius: 8px;
}

.comment-body {
  font-size: 0.9rem;
  color: #495057;
  white-space: pre-wrap;
  word-break: break-word;
}

.comment-text {
  margin: 0;
  line-height: 1.5;
  color: #495057;
  font-size: 0.9rem;
  white-space: pre-wrap;
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

/* @멘션 스타일 */
.comment-text .mention {
  color: #ed664d;
  font-weight: 500;
  cursor: pointer;
  text-decoration: none;
}

.comment-text .mention:hover {
  text-decoration: underline;
}

/* 인라인 링크 스타일 */
.comment-text .comment-link {
  color: #0066cc;
  text-decoration: none;
  word-break: break-all;
  transition: color 0.2s;
}

.comment-text .comment-link:hover {
  color: #0052a3;
  text-decoration: underline;
}

.edit-mode {
  margin-bottom: 12px;
}

.edit-textarea {
  width: 100%;
  box-sizing: border-box;
  border: 2px solid #ed664d;
  border-radius: 8px;
  padding: 12px;
  font-size: 0.9rem;
  line-height: 1.5;
  resize: vertical;
  font-family: inherit;
  outline: none;
  transition: border-color 0.2s;
}

.edit-textarea:focus {
  border-color: #d85a47;
  box-shadow: 0 0 0 3px rgba(237, 102, 77, 0.1);
}

.edit-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 12px;
}

.save-btn {
  background-color: #ed664d;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.save-btn:hover {
  background-color: #d85a47;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(237, 102, 77, 0.3);
}

.cancel-btn {
  background-color: #f8f9fa;
  color: #495057;
  border: 1px solid #dee2e6;
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.cancel-btn:hover {
  background-color: #e9ecef;
  border-color: #adb5bd;
}

.comment-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.like-btn {
  background: none;
  border: none;
  color: #999;
  font-size: 0.85rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
}

.like-btn:hover {
  background-color: #f8f9fa;
  color: #495057;
}

.like-btn.liked {
  color: #ed664d;
}

.like-btn.liked:hover {
  background-color: #fed7d2;
}

.like-icon {
  font-size: 0.8rem;
}

.like-text {
  font-size: 0.8rem;
}

.like-count {
  font-size: 0.75rem;
  font-weight: 600;
  background-color: #ed664d;
  color: white;
  padding: 2px 6px;
  border-radius: 10px;
  min-width: 18px;
  text-align: center;
}

.like-btn.liked .like-count {
  background-color: #d85a47;
}

@media (max-width: 768px) {
  .comment-item {
    padding: 12px;
    gap: 10px;
  }
  
  .author-avatar {
    width: 36px;
    height: 36px;
  }
  
  .edit-textarea {
    padding: 10px;
    font-size: 0.85rem;
  }
  
  .save-btn, .cancel-btn {
    padding: 6px 12px;
    font-size: 0.8rem;
  }
}

.comment-media-container {
  margin: 8px 0;
}

.comment-media {
  max-width: 100%;
  max-height: 300px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  object-fit: contain;
  display: block;
  margin: 8px 0;
}

.media-description {
  margin: 4px 0;
  font-size: 0.85rem;
  color: #6c757d;
  font-style: italic;
}

.comment-link-container {
  margin: 12px 0;
}

.comment-link-card {
  display: block;
  text-decoration: none;
  color: inherit;
  transition: all 0.2s;
}

.comment-link-card:hover .link-preview {
  border-color: #adb5bd;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.link-preview {
  border: 1px solid #e1e5e9;
  border-radius: 12px;
  background: white;
  transition: all 0.2s ease;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.link-preview.has-image {
  display: flex;
  flex-direction: column;
}

.link-preview-image {
  width: 100%;
  height: 160px;
  object-fit: cover;
  flex-shrink: 0;
  background: #f5f5f5;
}

.link-info {
  padding: 12px;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.link-title {
  font-weight: 600;
  font-size: 0.9rem;
  color: #212529;
  line-height: 1.3;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.link-description {
  font-size: 0.8rem;
  color: #6c757d;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.link-url {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 0.75rem;
  color: #adb5bd;
  margin-top: auto;
}

.link-icon {
  flex-shrink: 0;
}

.link-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 이미지 없는 경우 컴팩트 스타일 */
.link-preview:not(.has-image) .link-info {
  padding: 10px 12px;
}

.link-preview:not(.has-image) .link-title {
  font-size: 0.85rem;
}

.link-preview:not(.has-image) .link-description {
  display: none;
}

/* 링크 미리보기 로딩 스켈레톤 */
.link-preview-skeleton {
  border: 1px solid #dee2e6;
  border-radius: 8px;
  background: #f8f9fa;
  padding: 12px;
  animation: skeleton-loading 1.4s infinite ease-in-out;
}

@keyframes skeleton-loading {
  0% {
    background-color: #f8f9fa;
  }
  50% {
    background-color: #e9ecef;
  }
  100% {
    background-color: #f8f9fa;
  }
}

.skeleton-title {
  height: 16px;
  background: #e9ecef;
  border-radius: 4px;
  width: 70%;
  margin-bottom: 8px;
}

.skeleton-description {
  height: 14px;
  background: #e9ecef;
  border-radius: 4px;
  width: 90%;
  margin-bottom: 6px;
}

.skeleton-url {
  height: 12px;
  background: #e9ecef;
  border-radius: 4px;
  width: 50%;
}
</style>