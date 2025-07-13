<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { useUiStore } from '@/stores/ui';
import apiClient from '@/api';
import { defineEmits } from 'vue';
import FriendSelector from './CreatePostComponent/FriendSelector.vue';
import PostAdvancedOptions from './CreatePostComponent/PostAdvancedOptions.vue';
import PostAttachments from './CreatePostComponent/PostAttachments.vue';
import RepostPreview from './CreatePostComponent/RepostPreview.vue';
import { useMentions } from './composables/useMentions.ts';
import { useFriendData } from './composables/useFriendData.ts';
import { useFileAttachment } from './composables/useFileAttachment.ts';

/**
 * CreatePost 컴포넌트의 props와 emits 정의
 */
const uiStore = useUiStore();
const emit = defineEmits(['post-created']);
const showAdvancedOptions = ref(false);

// useFriendData 컴포저블 사용
const { friendsList, myProfile, loadFriends } = useFriendData();
console.log('[CreatePost] useFriendData 초기화 시점 friendsList:', friendsList.value);

// useFileAttachment 컴포저블 사용
const { attachedFiles, previewItems, isDragOver, addFiles, removeFile, handlePaste, setupDragAndDrop } = useFileAttachment();

// ==================== 반응형 상태 변수들 ====================

/**
 * 게시글 텍스트 내용
 * @type {import('vue').Ref<string>}
 */
const newPostText = ref('');
const textareaRef = ref<HTMLTextAreaElement | null>(null); // textarea 참조를 위한 ref

/**
 * 게시글 공개 설정 옵션
 * - OnlyMe: 나만 보기
 * - SelectedUsers: 특정 친구 공개
 * - UnselectedUsers: 특정 친구 비공개
 * - Friends: 친구 공개
 * - FriendsOfFriends: 친구의 친구 공개
 * - Everyone: 전체 공개
 * @type {import('vue').Ref<string>}
 */
const discoveryOption = ref('Friends');

/**
 * 댓글 허용 범위 설정 옵션
 * @type {import('vue').Ref<string>}
 */
const commentPermission = ref<string | null>(null);

/**
 * 다른 사용자의 공유 허용 여부
 * @type {import('vue').Ref<boolean>}
 */
const disallowShare = ref(false); // 기본값 'false' (공유 허용)

/**
 * 예약 발행 시간 (ISO 8601 형식의 문자열 또는 null)
 * @type {import('vue').Ref<string | null>}
 */
const reservationTime = ref<string | null>(null);

/**
 * 첨부할 링크 URL
 * @type {import('vue').Ref<string>}
 */
const attachedLink = ref('');

/**
 * 특정 친구 공개/비공개 설정 시 선택된 친구들의 userId 배열
 * @type {import('vue').Ref<string[]>}
 */
const selectedUserIds = ref<string[]>([]);

/**
 * 친구 선택 UI 표시 여부
 * @type {import('vue').Ref<boolean>}
 */
const showFriendSelector = ref(false);

// ==================== 리포스트 관련 상태 ====================

/**
 * 리포스트 모드 여부를 확인하는 computed 속성
 * @type {import('vue').ComputedRef<boolean>}
 */
const isShareMode = computed(() => uiStore.isShareMode);

/**
 * 리포스트할 원본 게시글 정보
 * @type {import('vue').ComputedRef<any>}
 */
const originalPostForShare = computed(() => uiStore.shareOriginalPost);

/**
 * 컴포넌트 확장 상태 (인라인 에디터용)
 * @type {import('vue').Ref<boolean>}
 */
const isExpanded = ref(false);

const openInlineEditor = () => {
  isExpanded.value = true;
};

const {
  isMentioning,
  mentionSearchResults,
  mentionDropdownPosition,
  selectedMentionIndex,
  handleTextInput,
  handleKeyDown,
  selectMention
} = useMentions(newPostText);

// ==================== 자동 크기 조절 로직 ====================

/**
 * textarea 높이를 내용에 맞게 조절하는 함수
 */
const autoResizeTextarea = () => {
  const textarea = textareaRef.value;
  if (textarea) {
    textarea.style.height = 'auto'; // 높이를 초기화하여 scrollHeight를 정확하게 계산
    // nextTick을 사용하여 DOM 업데이트가 완료된 후 높이 설정
    nextTick(() => {
      textarea.style.height = `${textarea.scrollHeight}px`;
    });
  }
};

// newPostText 내용이 변경될 때마다 높이를 조절
watch(newPostText, autoResizeTextarea);

// 컴포넌트가 확장되거나 에디터가 열릴 때도 높이를 재조절
watch([isExpanded, () => uiStore.isEditorOpen], ([newIsExpanded, newIsEditorOpen]) => {
  if (newIsExpanded || newIsEditorOpen) {
    // expanded-view가 표시된 후 함수를 실행해야 정확한 계산이 가능
    nextTick(autoResizeTextarea);
  }
});

// ==================== Computed 속성들 ====================

/**
 * 특정 친구 선택이 필요한 공개 옵션인지 확인
 * @type {import('vue').ComputedRef<boolean>}
 */
const needsFriendSelection = computed(() => {
  return ['SelectedUsers', 'UnselectedUsers'].includes(discoveryOption.value);
});

/**
 * 공개 설정 옵션 변경 시 처리 함수
 */
const onDiscoveryOptionChange = () => {
  selectedUserIds.value = [];
  if (needsFriendSelection.value) {
    showFriendSelector.value = true;
  } else {
    showFriendSelector.value = false;
  }
};

// ==================== 게시글 작성 함수들 ====================

/**
 * 메인 게시글 제출 함수
 * 리포스트 모드와 일반 모드를 구분하여 처리합니다.
 */
const submitPost = async () => {
  // 공유 모드일 경우, 새로운 텍스트 내용이 없어도 원본 게시글이 있으면 게시 가능
  if (isShareMode.value && originalPostForShare.value) {
    // 새로운 텍스트 내용, 첨부 파일, 링크가 모두 없어도 공유는 가능
  } else {
    // 일반 게시글 작성 모드일 경우, 내용이 없으면 경고
    if (!newPostText.value.trim() && attachedFiles.value.length === 0 && !attachedLink.value.trim()) {
      alert('내용을 입력해주세요.');
      return;
    }
  }

  try {
    const selectedUserIdsArray = [...selectedUserIds.value];
    const isSpecificFriendOption = ['SelectedUsers', 'UnselectedUsers'].includes(discoveryOption.value);
    const initialDiscoveryOption = isSpecificFriendOption ? 'Friends' : discoveryOption.value;

    const postDto = {
      DiscoveryOption: initialDiscoveryOption,
      CommentPermission: commentPermission.value,
      DisallowShare: disallowShare.value,
      ReservationTime: reservationTime.value,
      Contents: [] as any[],
      ParentPostId: isShareMode.value && originalPostForShare.value ? originalPostForShare.value.id : null,
      DiscoveryOptionSelectedUserIds: [] as string[]
    };

    if (newPostText.value.trim()) {
      const text = newPostText.value;
      const textParts: Array<any> = [];
      const nicknameToUserIdMap: Record<string, string> = {};
      friendsList.value.forEach(friend => {
        nicknameToUserIdMap[friend.nickname] = friend.userId;
      });

      let currentIndex = 0;
      const mentionRegex = /@(\S+)/g;
      let match;

      while ((match = mentionRegex.exec(text)) !== null) {
        const mentionStart = match.index;
        const mentionEnd = mentionRegex.lastIndex;
        const nickname = match[1];

        if (mentionStart > currentIndex) {
          const beforeText = text.substring(currentIndex, mentionStart);
          textParts.push({ $type: 'text', Text: beforeText });
        }

        const userId = nicknameToUserIdMap[nickname];
        if (userId) {
          textParts.push({ $type: 'profile', UserId: userId });
        } else {
          textParts.push({ $type: 'text', Text: text.substring(mentionStart, mentionEnd) });
        }
        currentIndex = mentionEnd;
      }

      if (currentIndex < text.length) {
        textParts.push({ $type: 'text', Text: text.substring(currentIndex) });
      }
      postDto.Contents.push(...textParts);
    }

    if (attachedLink.value.trim()) {
      postDto.Contents.push({
        $type: 'externalUrl',
        SourceUrl: attachedLink.value.trim()
      });
    }

    const formData = new FormData();
    attachedFiles.value.forEach(file => {
      formData.append('Files', file, file.name);
      postDto.Contents.push({
        $type: 'upload',
        FileName: file.name,
        Description: ''
      });
    });

    formData.append('JsonData', JSON.stringify(postDto));

    const createResponse = await apiClient.post('/api/Post', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    if (isSpecificFriendOption && selectedUserIdsArray.length > 0) {
      let postId;
      if (typeof createResponse.data === 'string' && createResponse.data.trim()) {
        postId = createResponse.data.trim();
      } else if (createResponse.data?.id) {
        postId = createResponse.data.id;
      } else if (Array.isArray(createResponse.data) && createResponse.data[0]?.id) {
        postId = createResponse.data[0].id;
      }

      if (!postId && createResponse.headers.location) {
        const locationHeader = createResponse.headers.location;
        const idMatch = locationHeader.match(/\/([^/]+)$/);
        if (idMatch) {
          postId = idMatch[1];
        }
      }

      if (!postId) {
        try {
          if (!myProfile.value) {
            const profileRes = await apiClient.get('/api/User/me');
            myProfile.value = profileRes.data;
          }
          const recentPostsRes = await apiClient.get(`/api/Post/user/${myProfile.value.userId}?limit=1`);
          if (recentPostsRes.data && recentPostsRes.data.length > 0) {
            postId = recentPostsRes.data[0].id;
          }
        } catch (recentPostError) {
          console.error('❌ 최근 게시글 조회 실패:', recentPostError);
        }
      }

      if (postId) {
        const discoveryUpdateDto = {
          newDiscoveryOption: discoveryOption.value,
          selectedUserIds: selectedUserIdsArray
        };
        await apiClient.put(`/api/Post/${postId}/discovery-option`, discoveryUpdateDto);
      } else {
        console.error('❌ 모든 방법으로도 게시글 ID를 찾을 수 없습니다.');
        alert('게시글은 생성되었지만 공개 설정 변경에 실패했습니다. 게시글을 직접 수정해주세요.');
      }
    }

    // 초기화
    newPostText.value = '';
    attachedLink.value = '';
    selectedUserIds.value = [];
    showFriendSelector.value = false;
    commentPermission.value = null;
    disallowShare.value = false;
    reservationTime.value = null;
    uiStore.closeEditor();
    emit('post-created');

  } catch (error: any) {
    console.error('❌ 게시글 작성 실패:', error);
    if (error.response?.status === 500) {
      alert('서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해주세요.');
    } else if (error.response?.data) {
      alert(`게시글 작성 실패: ${error.response.data}`);
    } else {
      alert('게시글 작성에 실패했습니다.');
    }
  }
};

/**
 * 취소 버튼 클릭 시 처리하는 함수
 */
const handleCancel = () => {
  if (uiStore.isEditorOpen) {
    uiStore.closeEditor();
  }
  if (isExpanded.value) {
    isExpanded.value = false;
  }

  newPostText.value = '';
  attachedLink.value = '';
  selectedUserIds.value = [];
  showFriendSelector.value = false;
  attachedFiles.value = [];
  isMentioning.value = false;
  mentionSearchResults.value = [];
};

// 드래그 앤 드롭 및 붙여넣기 이벤트 리스너 설정
const createPostCardRef = ref<HTMLElement | null>(null);

onMounted(() => {
  if (createPostCardRef.value) {
    setupDragAndDrop(createPostCardRef.value);
    createPostCardRef.value.addEventListener('paste', handlePaste);
  }
});

onUnmounted(() => {
  if (createPostCardRef.value) {
    createPostCardRef.value.removeEventListener('paste', handlePaste);
  }
});
</script>

<template>
  <div class="post-card create-post-card" :class="{ 'drag-over': isDragOver }" ref="createPostCardRef">
    <!-- Compact View: Always visible when not expanded -->
    <div v-if="!isExpanded && !uiStore.isEditorOpen" class="compact-view" @click="openInlineEditor">
      <textarea readonly placeholder="오늘 하루, 기억하고 싶은 순간이 있나요?"></textarea>
    </div>

    <!-- Expanded View: Wrapped in a transition component for smooth animation -->
    <transition name="expand">
      <div v-if="isExpanded || uiStore.isEditorOpen" class="expanded-view">
        <div v-if="isShareMode" class="repost-header">
          <div class="repost-label">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 22v-9m-4 4 4-4 4 4m-8-4a9 9 0 1 1 18 0 9 9 0 0 1-18 0Z"/>
            </svg>
            <span>공유하기</span>
          </div>
        </div>

        <textarea
          ref="textareaRef"
          v-model="newPostText"
          class="create-post-input"
          :placeholder="isShareMode ? '공유할 게시글에 생각을 추가해보세요...' : '오늘 하루, 기억하고 싶은 순간이 있나요?'"
          aria-label="게시글 내용 입력"
          @input="handleTextInput"
          @keydown="handleKeyDown"
        ></textarea>

        <div
          v-if="isMentioning"
          class="mention-dropdown"
          role="listbox"
          :aria-label="`친구 검색 결과: ${mentionSearchResults.length}명`"
          :style="{
            position: 'fixed',
            top: mentionDropdownPosition.top + 'px',
            left: mentionDropdownPosition.left + 'px',
            zIndex: 1000
          }"
        >
          <div v-if="mentionSearchResults.length === 0" class="mention-no-results" role="status">
            {{ friendsList.length === 0 ? '친구가 없습니다' : '검색 결과가 없습니다' }}
          </div>
          <div
            v-else
            v-for="(user, index) in mentionSearchResults"
            :key="user.userId"
            class="mention-item"
            :class="{ 'selected': index === selectedMentionIndex }"
            @click="selectMention(user)"
            @mouseenter="selectedMentionIndex = index"
            role="option"
            :aria-selected="index === selectedMentionIndex"
            :aria-label="`${user.nickname} @${user.handle}`"
          >
            <img :src="(user as any).profileImageUrl || '/src/assets/images/default_profile_image.jpg'" :alt="`${user.nickname} 프로필 이미지`">
            <div>
              <div class="nickname">{{ user.nickname }}</div>
              <div class="handle">@{{ user.handle }}</div>
            </div>
          </div>
        </div>

        <span id="mention-hint" class="sr-only">
          @ 심볼을 입력하여 친구를 멘션할 수 있습니다. 위아래 화살표로 선택하고 Enter로 확정하세요.
        </span>

        <PostAttachments
          v-model:attached-link="attachedLink"
          :preview-items="previewItems"
          @add-files="addFiles"
          @remove-file="removeFile"
        />

        <RepostPreview :original-post="originalPostForShare" />

        <FriendSelector
          v-if="discoveryOption === 'SelectedUsers' || discoveryOption === 'UnselectedUsers'"
          v-model="selectedUserIds"
          :discovery-option="discoveryOption"
          :friends-list="friendsList"
        />

        <div class="create-post-footer">
          <div> <!-- Wrapper for advanced options button -->
            <button class="toggle-advanced-btn" @click="showAdvancedOptions = !showAdvancedOptions">
              {{ showAdvancedOptions ? '🔽 고급 설정 닫기' : '⚙️ 고급 설정 열기' }}
            </button>
            <PostAdvancedOptions
              v-if="showAdvancedOptions"
              v-model:discovery-option="discoveryOption"
              v-model:comment-permission="commentPermission"
              v-model:disallow-share="disallowShare"
              v-model:reservation-time="reservationTime"
              @discovery-option-change="onDiscoveryOptionChange"
            />
          </div>
          <div class="submit-buttons">
            <button @click="handleCancel" class="btn-cancel">취소</button>
            <button @click="submitPost" class="btn-submit">올리기</button>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.toggle-advanced-btn {
  background: none;
  border: none;
  font-size: 0.9rem;
  color: #666;
  margin-bottom: 12px;
  cursor: pointer;
  padding: 4px;
  transition: color 0.2s;
}

.toggle-advanced-btn:hover {
  color: #ed664d;
  text-decoration: underline;
}

.post-card {
  background: white;
  border-radius: 8px;
  border: 1px solid #ddd;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.compact-view {
    padding: 20px;
}

.expanded-view {
    padding: 20px;
}

.compact-view textarea {
  width: 100%;
  padding: 5px 0;
  font-size: 1rem;
  border: none;
  border-bottom: 1px solid #e0e0e0;
  cursor: pointer;
  resize: none;
  transition: border-color 0.2s;
  box-sizing: border-box;
}

.create-post-input {
  font-family: 'Noto Sans KR', sans-serif;
  width: 100%;
  min-height: 100px;
  border: none;
  resize: none; /* 크기 조절 비활성화 */
  overflow-y: hidden; /* 스크롤바 숨김 */
  font-size: 1rem;
  padding: 0;
  line-height: 1.5;
  box-sizing: border-box; /* 패딩과 보더가 너비/높이에 포함되도록 설정 */
}

.create-post-input:focus { outline: none; }

.create-post-footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  flex-wrap: wrap;
  gap: 16px;
  margin-top: 12px;
}

.submit-buttons { display: flex; gap: 8px; }

.btn-cancel, .btn-submit {
  padding: 8px 24px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s;
}

.btn-cancel {
  background-color: #e9ecef;
  color: #495057;
}

.btn-cancel:hover {
  background-color: #dee2e6;
}

.btn-submit {
  background-color: #ed664d;
  color: white;
}

.btn-submit:hover {
  background-color: #e55a47;
}

/* @멘션 드롭다운 스타일 */
.mention-dropdown {
  background: white;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  max-height: 200px;
  overflow-y: auto;
  width: 250px;
}

.mention-item {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.mention-item:hover,
.mention-item.selected {
  background-color: #f5f5f5;
}

.mention-item.selected {
  background-color: #e8f5ff;
}

.mention-no-results {
  padding: 12px;
  text-align: center;
  color: #666;
  font-size: 14px;
}

.mention-item img {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  margin-right: 8px;
  object-fit: cover;
}

.mention-item .nickname {
  font-weight: 500;
  color: #333;
  font-size: 14px;
}

.mention-item .handle {
  color: #666;
  font-size: 12px;
}

/* 스크린 리더 전용 텍스트 */
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.create-post-card.drag-over {
  border: 2px dashed #ed664d;
  background-color: #fff0ed;
}

/* --- 포스트 입력란 --- */
.expand-enter-active,
.expand-leave-active {
  transition: max-height 0.8s ease-in-out, opacity 0.7s ease-in-out;
  overflow: hidden;
}

.expand-enter-from,
.expand-leave-to {
  max-height: 0;
  opacity: 0;
}

.expand-enter-to,
.expand-leave-from {
  max-height: 1000px; /* Set a sufficiently large value to not clip content */
  opacity: 1;
}
</style>
