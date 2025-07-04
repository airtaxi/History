<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import type { CommentResponseDto } from '@/types';
import { useAuthStore } from '@/stores/auth';
import apiClient from '@/api';
import { useRouter, RouterLink } from 'vue-router';


const props = defineProps<{
  comment: CommentResponseDto;
  profileImageUrl: string;
}>();

const emit = defineEmits(['mention-user', 'delete-comment', 'like-comment', 'update-comment', 'report-comment']);

// === Store 인스턴스 ===
const authStore = useAuthStore();
const router = useRouter();

// === 컴포넌트 상태 관리 ===
const isEditing = ref(false);
const editedText = ref('');
const isMenuOpen = ref(false);
const imageLoadError = ref(false);
const mediaUrlMap = ref<Record<string, string>>({});

// === 수정 관련 상태 ===
const editedImage = ref<File | null>(null);
const editedImageUrl = ref('');
const originalImageMediaId = ref<string | null>(null);

// === Computed Properties ===
const isMyComment = computed(() => {
  return authStore.user?.userId === props.comment.user.userId;
});

const isLikedByMe = computed(() => {
  return props.comment.likedUsers?.some(user => user.userId === authStore.user?.userId);
});

const safeProfileImageUrl = computed(() => {
  if (imageLoadError.value || !props.profileImageUrl) {
    return '/src/assets/images/default_profile_image.jpg';
  }
  return props.profileImageUrl;
});

// === 메소드(Methods) ===
const startEdit = () => {
  const textContent = props.comment.contents.find(c => c.$type === 'text');
  editedText.value = textContent?.text || '';

  const mediaContent = props.comment.contents.find(c => c.$type === 'media');
  originalImageMediaId.value = mediaContent?.mediaId || null;

  // 수정 시작 시 새 이미지 관련 상태 초기화
  editedImage.value = null;
  editedImageUrl.value = '';

  isEditing.value = true;
  isMenuOpen.value = false;
};

const cancelEdit = () => {
  isEditing.value = false;
};

const onImageChange = (event: Event) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (file) {
    editedImage.value = file;
    editedImageUrl.value = URL.createObjectURL(file);
  }
};

const removeImage = () => {
  editedImage.value = null;
  editedImageUrl.value = '';
  originalImageMediaId.value = null;
};

const saveComment = async () => {
  if (!editedText.value.trim() && !editedImage.value && !originalImageMediaId.value) {
    alert('댓글 내용을 입력하세요.');
    return;
  }

  const contents: any[] = [];

  if (editedText.value.trim()) {
    contents.push({ $type: 'text', Text: editedText.value.trim() });
  }

  if (editedImage.value) {
    contents.push({ $type: 'upload', FileName: editedImage.value.name });
  } else if (originalImageMediaId.value) {
    contents.push({ $type: 'media', mediaId: originalImageMediaId.value });
  }

  const formData = new FormData();
  formData.append('JsonData', JSON.stringify(contents));
  
  if (editedImage.value) {
    formData.append('Files', editedImage.value);
  }

  try {
    await apiClient.put(`/api/Comment/${props.comment.id}`, formData);
    isEditing.value = false;
    emit('update-comment', { commentId: props.comment.id });
  } catch (e) {
    console.error('댓글 수정 실패:', e);
    alert('댓글 수정 중 오류가 발생했습니다.');
  }
};

const handleImageError = (event: Event) => {
  const target = event.target as HTMLImageElement;
  if (!imageLoadError.value) {
    imageLoadError.value = true;
    target.src = '/src/assets/images/default_profile_image.jpg';
  }
};

const mentionUser = () => {
  emit('mention-user', props.comment.user.nickname);
};

const toggleMenu = (e: Event) => {
  e.stopPropagation();
  isMenuOpen.value = !isMenuOpen.value;
};

const likeComment = () => {
  emit('like-comment', props.comment.id);
};

const deleteComment = () => {
  if (confirm('정말로 이 댓글을 삭제하시겠습니까?')) {
    emit('delete-comment', props.comment.id);
    isMenuOpen.value = false;
  }
};

const getMediaBlobUrl = async (mediaId: string) => {
  try {
    const response = await apiClient.get(`/api/media/${mediaId}`, { responseType: 'blob' });
    return URL.createObjectURL(response.data);
  } catch (error) {
    console.warn('댓글 미디어 로딩 실패:', mediaId);
    return '';
  }
};

const splitTextWithLinksAndMentions = (text: string): Array<{ text: string; type: 'text' | 'link' | 'mention' }> => {
  const urlRegex = /(?:https?:\/\/[^\s]+)|(?:www\.[^\s]+)|(?:[a-zA-Z0-9][a-zA-Z0-9-]*(?:\.[a-zA-Z0-9][a-zA-Z0-9-]*)+(?:\/[^\s]*)?)/g;
  const mentionRegex = /@[a-zA-Z0-9_가-힣\s]+/g;
  
  const matches: Array<{ text: string; type: 'link' | 'mention'; index: number; length: number }> = [];
  
  let match;
  while ((match = urlRegex.exec(text)) !== null) {
    matches.push({ text: match[0], type: 'link', index: match.index, length: match[0].length });
  }
  while ((match = mentionRegex.exec(text)) !== null) {
    matches.push({ text: match[0], type: 'mention', index: match.index, length: match[0].length });
  }
  
  matches.sort((a, b) => a.index - b.index);
  
  const result: Array<{ text: string; type: 'text' | 'link' | 'mention' }> = [];
  let lastIndex = 0;
  
  for (const match of matches) {
    if (match.index > lastIndex) {
      result.push({ text: text.slice(lastIndex, match.index), type: 'text' });
    }
    result.push({ text: match.text, type: match.type });
    lastIndex = match.index + match.length;
  }

  if (lastIndex < text.length) {
    result.push({ text: text.slice(lastIndex), type: 'text' });
  }
  return result;
};

const reportComment = () => {
  emit('report-comment', props.comment.id);
  isMenuOpen.value = false; // 메뉴 닫기
};

const navigateToProfile = async (mentionText: string) => {
  const nickname = mentionText.substring(1).trim();
  try {
    const response = await apiClient.get(`/api/User/nickname-search/${encodeURIComponent(nickname)}`);
    const user = response.data.find((u: any) => u.nickname === nickname);
    if (user) {
      router.push(`/user/${user.userId}`);
    } else {
      console.warn(`사용자를 찾을 수 없습니다: ${nickname}`);
    }
  } catch (error) {
    console.error('사용자 검색 실패:', error);
  }
};

onMounted(async () => {
  for (const content of props.comment.contents) {
    if (content.$type === 'media' && (content.mediaId || content.thumbnailMediaId)) {
      const id = content.mediaId || content.thumbnailMediaId;
      if (id) {
        mediaUrlMap.value[id] = await getMediaBlobUrl(id);
      }
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
    <RouterLink :to="`/user/${comment.user.userId}`" @click.stop>
      <img
        :src="safeProfileImageUrl"
        class="author-avatar"
        @error="handleImageError"
      />
    </RouterLink>

    <div class="comment-main">
      <div class="comment-header">
        <div class="nickname-time">
          <span class="author-name" @click="mentionUser">{{ comment.user.nickname }}</span>
          <span class="comment-timestamp">{{ formatRelativeTime(comment.createdAt) }}</span>
        </div>
        
        <div class="header-actions">
          <button @click="likeComment" :class="['like-btn', { 'liked': isLikedByMe }]">
            <span v-if="isLikedByMe">❤️</span>
            <span v-else>🤍</span>
            <span v-if="comment.likedUsers?.length"> {{ comment.likedUsers.length }} </span>
          </button>

          <div class="more-menu-container" @click.stop>
            <button class="more-button" @click="toggleMenu">⋯</button>
            <div v-if="isMenuOpen" class="dropdown-menu">
              <template v-if="isMyComment">
                <div @click.stop="startEdit">수정</div>
                <div @click.stop="deleteComment">삭제</div>
              </template>
              <template v-else>
                <div @click.stop="reportComment">🚨 신고</div>
              </template>
            </div>
          </div>
        </div> </div> <div v-if="!isEditing" class="comment-body">
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

          <div v-else-if="content.$type === 'media' && (content.mediaId || content.thumbnailMediaId)" class="comment-media-container">
            <template v-if="mediaUrlMap[content.mediaId || content.thumbnailMediaId]">
              <video
                v-if="content.mimeType?.startsWith('video/')"
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
          </div>
          
          <div v-else-if="content.$type === 'externalUrl'" class="comment-link-container">
            <a :href="content.url || content.Url" target="_blank" rel="noopener noreferrer" class="comment-link-card" @click.stop>
              <div class="link-preview" :class="{ 'has-image': !!content.image }">
                <img 
                  v-if="content.image"
                  :src="content.image"
                  :alt="content.title || '링크 미리보기'"
                  class="link-preview-image"
                  @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
                />
                <div class="link-info">
                  <div v-if="content.title" class="link-title">{{ content.title }}</div>
                  <div v-if="content.description" class="link-description">{{ content.description }}</div>
                  <div class="link-url">
                    <span class="link-icon">🔗</span>
                    <span class="link-text">{{ content.url || content.Url }}</span>
                  </div>
                </div>
              </div>
            </a>
          </div>
          
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
        <div class="edit-main">
          <textarea v-model="editedText" class="edit-textarea" rows="3" placeholder="댓글을 수정하세요..."></textarea>
          
          <div class="edit-attachments">
            <div v-if="editedImageUrl || (originalImageMediaId && mediaUrlMap[originalImageMediaId])" class="image-preview">
              <img :src="editedImageUrl || mediaUrlMap[originalImageMediaId || '']" />
              <button @click="removeImage" class="remove-image-btn" title="이미지 삭제">×</button>
            </div>
          </div>
        </div>
        
        <div class="edit-actions">
          <div class="edit-options">
            <label class="image-upload-label" title="사진 추가">
              📷
              <input type="file" accept="image/*,video/*" @change="onImageChange" hidden />
            </label>
          </div>
          <div class="edit-buttons">
            <button class="cancel-btn" @click="cancelEdit">취소</button>
            <button class="save-btn" @click="saveComment">저장</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ================================== */
/* 댓글 아이템 기본 스타일              */
/* ================================== */
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

/* ================================== */
/* 더보기 메뉴, 좋아요 버튼 등 액션 스타일 */
/* ================================== */
.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
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

.like-btn {
  background: none;
  border: none;
  color: #6c757d;
  font-size: 0.85rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px;
  border-radius: 4px;
  transition: all 0.2s;
}

.like-btn:hover {
  background-color: #f8f9fa;
}

.like-btn.liked {
  color: #ed664d;
}

.like-btn.liked:hover {
  background-color: #fff0ed;
}

/* ================================== */
/* 댓글 본문 스타일                   */
/* ================================== */
.comment-body {
  font-size: 0.9rem;
  color: #495057;
  white-space: pre-wrap;
  word-break: break-word;
}

.comment-text {
  margin: 0;
  line-height: 1.5;
}

.mention {
  color: #ed664d;
  font-weight: 500;
  cursor: pointer;
  text-decoration: none;
  background-color: #fff0ed;
  padding: 2px 4px;
  border-radius: 4px;
}

.mention:hover {
  text-decoration: underline;
}

.comment-link {
  color: #0066cc;
  text-decoration: none;
  word-break: break-all;
}

.comment-link:hover {
  text-decoration: underline;
}

/* ================================== */
/* 미디어 및 링크 미리보기 스타일        */
/* ================================== */
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

.comment-link-container {
  margin: 12px 0;
}

.comment-link-card {
  display: block;
  text-decoration: none;
  color: inherit;
}

.comment-link-card:hover .link-preview {
  border-color: #adb5bd;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.link-preview {
  border: 1px solid #e1e5e9;
  border-radius: 12px;
  background: white;
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
  background: #f5f5f5;
}

.link-info {
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.link-title {
  font-weight: 600;
  font-size: 0.9rem;
  line-height: 1.3;
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
  margin-top: 4px;
}

.link-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ================================== */
/* 댓글 수정창 스타일 (개선됨)          */
/* ================================== */
.edit-mode {
  border: 1px solid #dee2e6;
  border-radius: 12px;
  background-color: #f8f9fa;
  margin-top: 8px;
  transition: all 0.2s;
}

.edit-mode:focus-within {
  border-color: #ed664d;
  box-shadow: 0 0 0 3px rgba(237, 102, 77, 0.1);
}

.edit-main {
  padding: 12px;
}

.edit-textarea {
  width: 100%;
  box-sizing: border-box;
  border: none;
  background: transparent;
  padding: 0;
  font-size: 0.9rem;
  line-height: 1.5;
  resize: vertical;
  font-family: inherit;
  outline: none;
  min-height: 60px;
}

.edit-attachments {
  margin-top: 12px;
}

.image-preview {
  position: relative;
  display: inline-block;
  max-width: 180px;
}

.image-preview img {
  width: 100%;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.remove-image-btn {
  position: absolute;
  top: -8px;
  right: -8px;
  background: rgba(0,0,0,0.7);
  color: white;
  border: none;
  border-radius: 50%;
  width: 24px;
  height: 24px;
  font-size: 16px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.remove-image-btn:hover {
  background-color: #ed664d;
  transform: scale(1.1);
}

.edit-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  border-top: 1px solid #e9ecef;
}

.image-upload-label {
  cursor: pointer;
  font-size: 1.2rem;
  color: #6c757d;
  padding: 4px;
  border-radius: 50%;
  transition: all 0.2s;
}

.image-upload-label:hover {
  background-color: #e9ecef;
  color: #212529;
}

.edit-buttons {
  display: flex;
  gap: 8px;
}

.save-btn, .cancel-btn {
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid transparent;
}

.save-btn {
  background-color: #ed664d;
  color: white;
}

.save-btn:hover {
  background-color: #d85a47;
}

.cancel-btn {
  background-color: #e9ecef;
  color: #495057;
  border-color: #dee2e6;
}

.cancel-btn:hover {
  background-color: #dee2e6;
}

/* ================================== */
/* 반응형 스타일                       */
/* ================================== */
@media (max-width: 768px) {
  .comment-item {
    padding: 12px;
    gap: 10px;
  }
  
  .author-avatar {
    width: 32px;
    height: 32px;
  }
  
  .save-btn, .cancel-btn {
    padding: 6px 12px;
    font-size: 0.8rem;
  }
}
</style>