<script setup lang="ts">
import { ref, computed } from 'vue';
import { useUiStore } from '@/stores/ui';
import apiClient from '@/api';
import { defineEmits } from 'vue';
import FriendSelector from './CreatePostComponent/FriendSelector.vue';
import PostAdvancedOptions from './CreatePostComponent/PostAdvancedOptions.vue';
import PostAttachments from './CreatePostComponent/PostAttachments.vue';
import RepostPreview from './CreatePostComponent/RepostPreview.vue';
import { useMentions } from './composables/useMentions.ts';
import { useFriendData } from './composables/useFriendData.ts';

/**
 * CreatePost 컴포넌트의 props와 emits 정의
 */
const uiStore = useUiStore();
const emit = defineEmits(['post-created']);
const showAdvancedOptions = ref(false);

// useFriendData 컴포저블 사용
const { friendsList, myProfile, loadFriends } = useFriendData();
console.log('[CreatePost] useFriendData 초기화 시점 friendsList:', friendsList.value);

// ==================== 반응형 상태 변수들 ====================

/**
 * 게시글 텍스트 내용
 * @type {import('vue').Ref<string>}
 */
const newPostText = ref('');

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
 * 업로드할 파일들의 배열
 * @type {import('vue').Ref<File[]>}
 */
const attachedFiles = ref<File[]>([]);



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
    // friendsList는 useFriendData에서 이미 로드되거나 useMentions에 의해 로드됨
    // loadFriends(); // 불필요한 중복 호출 제거
  } else {
    showFriendSelector.value = false;
  }
};

// ==================== 게시글 작성 함수들 ====================

/**
 * 메인 게시글 제출 함수
 * 리포스트 모드와 일반 모드를 구분하여 처리합니다.
 *
 * @async
 * @function submitPost
 * @returns {Promise<void>}
 *
 * @description
 * 1. 리포스트 모드인 경우 handleRepost() 함수를 호출합니다.
 * 2. 일반 모드인 경우 새로운 게시글 작성 로직을 수행합니다.
 * 3. 특정 친구 선택 옵션의 경우 2단계 프로세스로 처리합니다:
 *    - 1단계: Friends 옵션으로 게시글 생성
 *    - 2단계: 원하는 옵션으로 공개 설정 변경
 *
 * @throws {Error} 게시글 작성 실패 시 에러를 throw하고 사용자에게 알림
 *
 * @example
 * await submitPost();
 */
const submitPost = async () => {
  if (!newPostText.value.trim() && attachedFiles.value.length === 0 && !attachedLink.value.trim()) {
    alert('내용을 입력해주세요.');
    return;
  }

  try {
    // selectedUserIds를 일반 배열로 변환
    const selectedUserIdsArray = [...selectedUserIds.value];

    // 특정 친구 선택이 필요한 경우 일단 Friends로 생성 후 변경
    const isSpecificFriendOption = ['SelectedUsers', 'UnselectedUsers'].includes(discoveryOption.value);
    const initialDiscoveryOption = isSpecificFriendOption ? 'Friends' : discoveryOption.value;

    // 게시글 데이터 타입을 명시적으로 정의하여 타입 안전성 확보
    const postDto = {
      DiscoveryOption: initialDiscoveryOption,
      CommentPermission: commentPermission.value,
      DisallowShare: disallowShare.value,
      ReservationTime: reservationTime.value,
      Contents: [] as any[],
      // ✨ 공유 모드일 경우 ParentPostId 설정
      ParentPostId: isShareMode.value && originalPostForShare.value ? originalPostForShare.value.id : null,
      DiscoveryOptionSelectedUserIds: [] as string[]
    };

    // 텍스트 내용이 있는 경우 Contents 배열에 추가
    if (newPostText.value.trim()) {
      const text = newPostText.value;
      const textParts: Array<any> = [];

      // 닉네임 → userId 매핑
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

    // 링크가 있는 경우 externalUrl 콘텐츠로 추가
    if (attachedLink.value.trim()) {
      postDto.Contents.push({
        $type: 'externalUrl',
        SourceUrl: attachedLink.value.trim()
      });
    }

    const formData = new FormData();

    // 첨부 파일들을 FormData에 추가하고 Contents 배열에 메타데이터 추가
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

    // 특정 친구 선택이 필요한 경우 2단계 진행
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
    attachedFiles.value = [];
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
    console.error('📊 에러 상세정보:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status,
      statusText: error.response?.statusText
    });

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
 *
 * @function handleCancel
 * @returns {void}
 *
 * @description
 * 1. 모달 모드인 경우 에디터를 닫습니다.
 * 2. 타임라인 인라인 모드인 경우 compact-view로 돌아갑니다.
 * 3. 작성 중인 내용들을 초기화합니다.
 */
const handleCancel = () => {
  // 모달이 열려있다면 모달을 닫습니다.
  if (uiStore.isEditorOpen) {
    uiStore.closeEditor();
  }
  // 인라인으로 확장되었다면 축소합니다.
  if (isExpanded.value) {
    isExpanded.value = false;
  }

  // 작성 중인 내용 초기화
  newPostText.value = '';
  attachedFiles.value = [];
  attachedLink.value = '';
  selectedUserIds.value = [];
  showFriendSelector.value = false;

  // @멘션 관련 초기화
  isMentioning.value = false;
  mentionSearchResults.value = [];
};
</script>

<template>
  <div class="post-card create-post-card">
    <div v-if="!uiStore.isEditorOpen && !isExpanded" class="compact-view" @click="openInlineEditor">
      <textarea readonly placeholder="오늘 하루, 기억하고 싶은 순간이 있나요?"></textarea>
    </div>

    <div v-else class="expanded-view">
      <div v-if="isShareMode" class="repost-header">
        <div class="repost-label">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 22v-9m-4 4 4-4 4 4m-8-4a9 9 0 1 1 18 0 9 9 0 0 1-18 0Z"/>
          </svg>
          <span>공유하기</span>
        </div>
      </div>

      <textarea
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
        v-model:attached-files="attachedFiles"
        v-model:attached-link="attachedLink"
      />

    <RepostPreview :original-post="originalPostForShare" />



      <FriendSelector
        v-if="discoveryOption === 'SelectedUsers' || discoveryOption === 'UnselectedUsers'"
        v-model="selectedUserIds"
        :discovery-option="discoveryOption"
        :friends-list="friendsList"
      />

      <div class="create-post-footer">
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
        <div class="submit-buttons">
          <button @click="handleCancel" class="btn-cancel">취소</button>
          <button @click="submitPost" class="btn-submit">올리기</button>
        </div>
      </div>
    </div>
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

.create-post-card {
    padding: 20px;
}

.compact-view textarea {
  width: 100%;
  padding: 12px;
  font-size: 1rem;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  background-color: #f8f9fa;
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
  resize: vertical;
  font-size: 1.1rem;
  padding: 8px 0;
  line-height: 1.5;
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
</style>
