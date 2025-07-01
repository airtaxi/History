<script setup lang="ts">
import { ref, defineProps, defineEmits, defineExpose } from 'vue';
import apiClient from '@/api';
import type { UserResponseDto } from '@/types';

const props = defineProps<{ postId: string }>();
const emit = defineEmits(['comment-created']);

const newCommentText = ref('');
const textareaRef = ref<HTMLTextAreaElement | null>(null);
const attachedImage = ref<File | null>(null);
const attachedLink = ref('');
const showImagePreview = ref(false);
const imagePreviewUrl = ref('');

// @멘션 관련 상태
const mentionSearchText = ref('');
const mentionSearchResults = ref<UserResponseDto[]>([]);
const isMentioning = ref(false);
const mentionStartIndex = ref(-1);
const mentionDropdownPosition = ref({ top: 0, left: 0 });
const friendsList = ref<UserResponseDto[]>([]);
const myProfile = ref<any | null>(null);
const selectedMentionIndex = ref(-1);

const generateContentsFromText = (): Array<any> => {
  const text = newCommentText.value;
  const result: Array<any> = [];

  // 닉네임 → userId 매핑
  const nicknameToUserIdMap: Record<string, string> = {};
  friendsList.value.forEach(friend => {
    nicknameToUserIdMap[friend.nickname] = friend.userId;
    nicknameToUserIdMap[friend.handle] = friend.userId;
  });

  let currentIndex = 0;
  const mentionRegex = /@(\S+)/g;
  let match;

  while ((match = mentionRegex.exec(text)) !== null) {
    const mentionStart = match.index;
    const mentionEnd = mentionRegex.lastIndex;
    const nickname = match[1];

    if (mentionStart > currentIndex) {
      result.push({
        $type: 'text',
        Text: text.substring(currentIndex, mentionStart),
      });
    }

    const userId = nicknameToUserIdMap[nickname];
    if (userId) {
      result.push({
        $type: 'profile',
        UserId: userId,
      });
    } else {
      // 못 찾으면 그냥 텍스트로 처리
      result.push({
        $type: 'text',
        Text: text.substring(mentionStart, mentionEnd),
      });
    }

    currentIndex = mentionEnd;
  }

  if (currentIndex < text.length) {
    result.push({
      $type: 'text',
      Text: text.substring(currentIndex),
    });
  }

  return result;
};


const submitComment = async () => {
  if (!newCommentText.value.trim() && !attachedImage.value && !attachedLink.value.trim()) {
    alert('댓글 내용을 입력하세요!');
    return;
  }

  const contents: any[] = [];

  if (newCommentText.value.trim()) {
    const parsedText = generateContentsFromText();
    console.log('✅ 멘션 파싱 결과:', parsedText); 
    contents.push(...parsedText);
  }

  if (attachedLink.value.trim()) {
    contents.push({
      $type: 'externalUrl',
      Url: attachedLink.value.trim(),
    });
  }

  if (attachedImage.value && attachedImage.value.name) {
    contents.push({
      $type: 'upload',
      FileName: attachedImage.value.name,
    });
  }

  const jsonPayload = { Contents: contents };

  const formData = new FormData();
  formData.append('JsonData', JSON.stringify(contents));

  if (attachedImage.value) {
    formData.append('Files', attachedImage.value);
  }

  console.log('[FormData 디버깅 시작]');
  console.log('JsonData (stringified):', JSON.stringify(contents));
  if (attachedImage.value) {
    console.log('첨부된 파일:', {
      name: attachedImage.value.name,
      size: attachedImage.value.size,
      type: attachedImage.value.type,
    });
  } else {
    console.log('첨부된 파일 없음');
  }

  // FormData 전체 반복 출력 (보이는 key-value 확인)
  for (const [key, value] of formData.entries()) {
    if (value instanceof File) {
      console.log(`📎 ${key}:`, {
        name: value.name,
        size: value.size,
        type: value.type,
      });
    } else {
      console.log(`📨 ${key}:`, value);
    }
  }
  console.log('[FormData 디버깅 끝]');

  try {
    const res = await apiClient.post(`/api/Comment/${props.postId}`, formData);
    console.log('서버 응답:', res);
    
    // 성공 시 폼 초기화
    clearForm();
    
    emit('comment-created');
  } catch (e: any) {
    console.error('댓글 작성 실패:', e);
    console.log('응답 data:', e.response?.data);
    console.log('응답 status:', e.response?.status);
    console.log('응답 headers:', e.response?.headers);
  }
};

const handleFileChange = (event: Event) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (file) {
    attachedImage.value = file;
    imagePreviewUrl.value = URL.createObjectURL(file);
  }
};

const addMention = (nickname: string) => {
  const mention = `@${nickname} `;
  newCommentText.value += mention;
  textareaRef.value?.focus();
};

// 폼 초기화 함수 추가
const clearForm = () => {
  newCommentText.value = '';
  attachedImage.value = null;
  attachedLink.value = '';
  showImagePreview.value = false;
  imagePreviewUrl.value = '';
  
  // @멘션 관련 초기화
  isMentioning.value = false;
  mentionSearchText.value = '';
  mentionSearchResults.value = [];
};

// 친구 목록 로드
const loadFriends = async () => {
  try {
    if (!myProfile.value) {
      const profileRes = await apiClient.get('/api/User/me');
      myProfile.value = profileRes.data;
    }
    
    if (myProfile.value) {
      const response = await apiClient.get(`/api/Friendship/${myProfile.value.userId}`);
      friendsList.value = response.data;
    }
  } catch (error) {
    console.error('친구 목록 로드 실패:', error);
    friendsList.value = [];
  }
};

// @멘션 검색 타이머
let mentionSearchTimeout: number | null = null;

// 텍스트 입력 시 @멘션 감지
const handleTextInput = (event: Event) => {
  const target = event.target as HTMLTextAreaElement;
  const cursorPosition = target.selectionStart;
  const text = target.value;
  
  const lastAtSymbol = text.lastIndexOf('@', cursorPosition - 1);
  
  if (lastAtSymbol !== -1 && lastAtSymbol < cursorPosition) {
    const searchText = text.substring(lastAtSymbol + 1, cursorPosition);
    
    if (searchText.includes(' ') || searchText.includes('\n')) {
      isMentioning.value = false;
      mentionSearchResults.value = [];
      return;
    }
    
    isMentioning.value = true;
    mentionStartIndex.value = lastAtSymbol;
    mentionSearchText.value = searchText;
    
    const textareaRect = target.getBoundingClientRect();
    mentionDropdownPosition.value = {
      top: textareaRect.bottom + 5,
      left: textareaRect.left
    };
    
    searchMentions();
  } else {
    isMentioning.value = false;
    mentionSearchResults.value = [];
  }
};

// @멘션 검색
const searchMentions = () => {
  if (mentionSearchTimeout) {
    clearTimeout(mentionSearchTimeout);
  }
  
  // 친구 목록이 없으면 먼저 로드
  if (friendsList.value.length === 0) {
    loadFriends().then(() => {
      performMentionSearch();
    });
  } else {
    performMentionSearch();
  }
};

const performMentionSearch = () => {
  if (!mentionSearchText.value) {
    // 검색어가 없으면 친구 목록 전체 표시
    mentionSearchResults.value = friendsList.value.slice(0, 5);
  } else {
    // 친구 목록에서 필터링
    const filtered = friendsList.value.filter(friend => 
      friend.nickname.toLowerCase().includes(mentionSearchText.value.toLowerCase()) ||
      friend.handle.toLowerCase().includes(mentionSearchText.value.toLowerCase())
    );
    
    mentionSearchResults.value = filtered.slice(0, 5);
  }
  
  // 검색 결과가 변경되면 선택 인덱스 초기화
  selectedMentionIndex.value = -1;
};

// @멘션 선택
const selectMention = (user: UserResponseDto) => {
  const text = newCommentText.value;
  const beforeMention = text.substring(0, mentionStartIndex.value);
  const afterCursor = text.substring(mentionStartIndex.value + mentionSearchText.value.length + 1);
  
  newCommentText.value = `${beforeMention}@${user.handle} ${afterCursor}`;
  
  isMentioning.value = false;
  mentionSearchResults.value = [];
  mentionSearchText.value = '';
  selectedMentionIndex.value = -1;
};

// 키보드 이벤트 처리
const handleKeyDown = (event: KeyboardEvent) => {
  if (!isMentioning.value || mentionSearchResults.value.length === 0) return;
  
  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault();
      selectedMentionIndex.value = Math.min(
        selectedMentionIndex.value + 1,
        mentionSearchResults.value.length - 1
      );
      break;
      
    case 'ArrowUp':
      event.preventDefault();
      selectedMentionIndex.value = Math.max(selectedMentionIndex.value - 1, 0);
      break;
      
    case 'Enter':
      event.preventDefault();
      if (selectedMentionIndex.value >= 0) {
        selectMention(mentionSearchResults.value[selectedMentionIndex.value]);
      }
      break;
      
    case 'Escape':
      event.preventDefault();
      isMentioning.value = false;
      mentionSearchResults.value = [];
      selectedMentionIndex.value = -1;
      break;
  }
};

defineExpose({ addMention, clearForm });
</script>

<template>
  <div class="create-comment-form">
    <textarea 
      ref="textareaRef" 
      v-model="newCommentText" 
      @input="handleTextInput"
      @keydown="handleKeyDown"
      placeholder="댓글을 입력하세요..." 
    />
    
    <!-- @멘션 드롭다운 -->
    <div 
      v-if="isMentioning"
      class="mention-dropdown"
      :style="{
        position: 'fixed',
        top: mentionDropdownPosition.top + 'px',
        left: mentionDropdownPosition.left + 'px',
        zIndex: 1000
      }"
    >
      <div v-if="mentionSearchResults.length === 0" class="mention-no-results">
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
      >
        <img :src="user.profileThumbnailMediaId ? `/api/media/${user.profileThumbnailMediaId}` : '/default_profile_image.jpg'" alt="">
        <div>
          <div class="nickname">{{ user.nickname }}</div>
          <div class="handle">@{{ user.handle }}</div>
        </div>
      </div>
    </div>

    <div class="attach-section">
      <label class="upload-btn">
        🖼 이미지
        <input type="file" accept="image/*,video/*" @change="handleFileChange" hidden />
      </label>

      <button @click="showImagePreview = true" v-if="attachedImage">미리보기</button>

      <input v-model="attachedLink" placeholder="링크 첨부 (https://...)" class="link-input" />
    </div>

    <button @click="submitComment">등록</button>

    <div v-if="showImagePreview" class="modal-overlay" @click.self="showImagePreview = false">
      <img :src="imagePreviewUrl" class="image-popup" />
    </div>
  </div>
</template>

<style scoped>
.create-comment-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-top: 1px solid #eee;
}
textarea {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid #ddd;
  border-radius: 6px;
  padding: 10px;
  resize: vertical;
  min-height: 60px;
}
.attach-section {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}
.upload-btn {
  background-color: #f0f0f0;
  padding: 6px 10px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.9rem;
  border: 1px solid #ddd;
}
.link-input {
  flex: 1;
  min-width: 200px;
  padding: 6px 8px;
  border: 1px solid #ddd;
  border-radius: 6px;
}
button {
  background-color: #ed664d;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  align-self: flex-end;
}
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 999;
}
.image-popup {
  max-width: 80%;
  max-height: 80%;
  border-radius: 8px;
  box-shadow: 0 0 12px rgba(0, 0, 0, 0.3);
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
</style>
