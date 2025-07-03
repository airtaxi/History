<script setup lang="ts">
import { ref, defineProps, defineEmits, defineExpose } from 'vue';
import apiClient from '@/api';
import type { UserResponseDto } from '@/types';
import 'emoji-picker-element';

const props = defineProps<{ postId: string }>();
const emit = defineEmits(['comment-created']);

const newCommentText = ref('');
const textareaRef = ref<HTMLTextAreaElement | null>(null);
const attachedImage = ref<File | null>(null);
const attachedLink = ref('');
const imagePreviewUrl = ref('');

// 이모지 피커 상태
const showEmojiPicker = ref(false);

// @멘션 관련 상태
const mentionSearchText = ref('');
const mentionSearchResults = ref<UserResponseDto[]>([]);
const isMentioning = ref(false);
const mentionStartIndex = ref(-1);
const mentionDropdownPosition = ref({ top: 0, left: 0 });
const friendsList = ref<UserResponseDto[]>([]);
const myProfile = ref<any | null>(null);
const selectedMentionIndex = ref(-1);

// 이모지 선택
const onEmojiSelect = (event: any) => {
  newCommentText.value += event.detail.unicode;
};
const toggleEmojiPicker = () => {
  showEmojiPicker.value = !showEmojiPicker.value;
};

const generateContentsFromText = (): Array<any> => {
  const text = newCommentText.value;
  const result: Array<any> = [];
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
      result.push({ $type: 'text', Text: text.substring(currentIndex, mentionStart) });
    }
    const userId = nicknameToUserIdMap[nickname];
    if (userId) {
      result.push({ $type: 'profile', UserId: userId });
    } else {
      result.push({ $type: 'text', Text: text.substring(mentionStart, mentionEnd) });
    }
    currentIndex = mentionEnd;
  }
  if (currentIndex < text.length) {
    result.push({ $type: 'text', Text: text.substring(currentIndex) });
  }
  return result;
};


const submitComment = async () => {
  if (showEmojiPicker.value) showEmojiPicker.value = false;
  if (isMentioning.value) isMentioning.value = false;

  if (!newCommentText.value.trim() && !attachedImage.value && !attachedLink.value.trim()) {
    alert('댓글 내용을 입력하세요!');
    return;
  }
  const contents: any[] = [];
  if (newCommentText.value.trim()) {
    contents.push(...generateContentsFromText());
  }
  if (attachedLink.value.trim()) {
    contents.push({ $type: 'externalUrl', Url: attachedLink.value.trim() });
  }
  if (attachedImage.value && attachedImage.value.name) {
    contents.push({ $type: 'upload', FileName: attachedImage.value.name });
  }
  const formData = new FormData();
  formData.append('JsonData', JSON.stringify(contents));
  if (attachedImage.value) {
    formData.append('Files', attachedImage.value);
  }
  try {
    await apiClient.post(`/api/Comment/${props.postId}`, formData);
    clearForm();
    emit('comment-created');
  } catch (e: any) {
    console.error('댓글 작성 실패:', e);
  }
};

const handleFileChange = (event: Event) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (file) {
    attachedImage.value = file;
    imagePreviewUrl.value = URL.createObjectURL(file);
    (event.target as HTMLInputElement).value = ''; 
  }
};

const removeAttachedImage = () => {
  attachedImage.value = null;
  imagePreviewUrl.value = '';
};

const addMention = (nickname: string) => {
  const mention = `@${nickname} `;
  newCommentText.value += mention;
  textareaRef.value?.focus();
};

const clearForm = () => {
  newCommentText.value = '';
  attachedLink.value = '';
  removeAttachedImage();
  isMentioning.value = false;
  mentionSearchText.value = '';
  mentionSearchResults.value = [];
};

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

let mentionSearchTimeout: number | null = null;

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

const searchMentions = () => {
  if (mentionSearchTimeout) {
    clearTimeout(mentionSearchTimeout);
  }
  
  if (friendsList.value.length === 0) {
    loadFriends().then(() => {
      performMentionSearch();
    });
  } else {
    performMentionSearch();
  }
};

// [수정] 프로필 이미지 로딩 로직이 포함된 완전한 검색 함수
const performMentionSearch = async () => {
  let results: UserResponseDto[];

  if (!mentionSearchText.value) {
    results = friendsList.value.slice(0, 5);
  } else {
    results = friendsList.value.filter(friend =>
      friend.nickname.toLowerCase().includes(mentionSearchText.value.toLowerCase()) ||
      friend.handle.toLowerCase().includes(mentionSearchText.value.toLowerCase())
    ).slice(0, 5);
  }

  // [수정] 검색 결과의 프로필 이미지를 불러오는 로직 복원
  for (const user of results) {
    if (user.profileThumbnailMediaId) {
      try {
        const res = await apiClient.get(`/api/Media/${user.profileThumbnailMediaId}`, {
          responseType: 'blob',
        });
        user.profileImageUrl = URL.createObjectURL(res.data);
      } catch {
        user.profileImageUrl = '/src/assets/images/default_profile_image.jpg';
      }
    } else {
      user.profileImageUrl = '/src/assets/images/default_profile_image.jpg';
    }
  }

  mentionSearchResults.value = results;
  selectedMentionIndex.value = -1;
};

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

const handleKeyDown = (event: KeyboardEvent) => {
  if (!isMentioning.value || mentionSearchResults.value.length === 0) return;
  
  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault();
      selectedMentionIndex.value = (selectedMentionIndex.value + 1) % mentionSearchResults.value.length;
      break;
      
    case 'ArrowUp':
      event.preventDefault();
      selectedMentionIndex.value = (selectedMentionIndex.value - 1 + mentionSearchResults.value.length) % mentionSearchResults.value.length;
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
      break;
  }
};
defineExpose({ addMention, clearForm });

</script>

<template>
  <div class="comment-form-container">
    <div v-if="attachedImage" class="image-preview-container">
      <img :src="imagePreviewUrl" class="preview-thumbnail" alt="첨부 이미지 미리보기" />
      <button @click="removeAttachedImage" class="remove-image-btn">×</button>
    </div>
    <div class="input-row">
      <div class="comment-input-wrapper">
        <textarea 
          ref="textareaRef" 
          v-model="newCommentText" 
          @input="handleTextInput"
          @keydown="handleKeyDown"
          @keydown.enter.prevent="submitComment"
          placeholder="댓글을 입력하세요."
        />
        <div class="input-actions">
          <label class="action-btn">
            <span>📷</span>
            <input type="file" accept="image/*" @change="handleFileChange" hidden />
          </label>
          <button class="action-btn" @click="toggleEmojiPicker">
            <span>🙂</span>
          </button>
        </div>
      </div>
      <button class="submit-btn" @click="submitComment">전송</button>
    </div>
    <div v-if="showEmojiPicker" class="emoji-picker-container">
      <emoji-picker @emoji-click="onEmojiSelect" class="light"></emoji-picker>
    </div>
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
        <img :src="user.profileImageUrl || '/src/assets/images/default_profile_image.jpg'" />
        <div>
          <div class="nickname">{{ user.nickname }}</div>
          <div class="handle">@{{ user.handle }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.comment-form-container {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 8px 16px;
  border-top: 1px solid #e0e0e0;
  background-color: #f9f9f9;
  position: relative;
}

.input-row {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.comment-input-wrapper {
  flex: 1;
  display: flex;
  align-items: center;
  border: 1px solid #ddd;
  border-radius: 18px;
  background-color: white;
  padding: 0 4px 0 12px;
  min-width: 0;
}


.image-preview-container {
  position: relative;
  align-self: flex-start;
  width: fit-content;
}
.preview-thumbnail {
  height: 72px;
  width: 72px;
  border-radius: 8px;
  border: 1px solid #ddd;
  object-fit: cover;
}
.remove-image-btn {
  position: absolute;
  top: -5px;
  right: -5px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background-color: rgba(0, 0, 0, 0.7);
  color: white;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  line-height: 20px;
}

textarea {
  flex: 1;
  border: none;
  background: transparent;
  padding: 8px 0;
  resize: none;
  height: 20px; 
  line-height: 20px;
  overflow-y: hidden;
  font-size: 14px;
  outline: none;
}

.input-actions {
  display: flex;
  align-items: center;
}
.action-btn {
  background: none;
  border: none;
  padding: 6px;
  cursor: pointer;
  font-size: 1.2rem;
  color: #555;
  border-radius: 50%;
}
.action-btn:hover {
  background-color: #f0f0f0;
}

.submit-btn {
  background-color: #ed664d;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 18px;
  font-weight: 600;
  cursor: pointer;
  flex-shrink: 0;
}

.submit-btn:hover {
  background-color: #d85a47;
}

.emoji-picker-container {
  position: absolute;
  bottom: 55px; 
  right: 16px; 
  z-index: 100;
}
emoji-picker {
  --border-radius: 12px;
  --outline-color: #ed664d;
}

.mention-dropdown {
  background: white; border: 1px solid #e0e0e0; border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1); max-height: 200px;
  overflow-y: auto; width: 250px; position: fixed; z-index: 1000;
}
.mention-item {
  display: flex; align-items: center; padding: 8px 12px;
  cursor: pointer; transition: background-color 0.2s;
}
.mention-item:hover, .mention-item.selected { background-color: #f5f5f5; }
.mention-item.selected { background-color: #e8f5ff; }
.mention-no-results {
  padding: 12px; text-align: center; color: #666; font-size: 14px;
}
.mention-item img {
  width: 32px; height: 32px; border-radius: 50%;
  margin-right: 8px; object-fit: cover;
}
.mention-item .nickname { font-weight: 500; color: #333; font-size: 14px; }
.mention-item .handle { color: #666; font-size: 12px; }
</style>