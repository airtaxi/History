<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { UserResponseDto } from '@/types';

const props = defineProps<{
  modelValue: string[];
  discoveryOption: 'SelectedUsers' | 'UnselectedUsers';
  friendsList: UserResponseDto[]; // CreatePost.vue에서 전달받을 친구 목록
}>();

const emit = defineEmits(['update:modelValue']);

const friendSearchText = ref('');
const friendSearchResults = ref<UserResponseDto[]>([]);
const isFriendSearchFocused = ref(false);
let friendSearchTimeout: number;

const onFriendSearchInput = () => {
  clearTimeout(friendSearchTimeout);
  if (!friendSearchText.value.trim()) {
    friendSearchResults.value = [];
    return;
  }
  friendSearchTimeout = window.setTimeout(() => {
    friendSearchResults.value = props.friendsList.filter(user => 
      user.nickname.toLowerCase().includes(friendSearchText.value.toLowerCase()) ||
      user.handle.toLowerCase().includes(friendSearchText.value.toLowerCase())
    );
  }, 300);
};

const selectFriendFromSearch = (user: UserResponseDto) => {
  toggleFriendSelection(user.userId);
  friendSearchText.value = '';
  friendSearchResults.value = [];
  isFriendSearchFocused.value = false;
};

const hideFriendSearchResults = () => {
  setTimeout(() => {
    isFriendSearchFocused.value = false;
    friendSearchResults.value = [];
  }, 200);
};

const toggleFriendSelection = (userId: string) => {
  const newSelectedIds = [...props.modelValue];
  const index = newSelectedIds.indexOf(userId);
  if (index > -1) {
    newSelectedIds.splice(index, 1);
  } else {
    newSelectedIds.push(userId);
  }
  emit('update:modelValue', newSelectedIds);
};

const getSelectedFriends = computed(() => {
  return props.friendsList.filter(friend => props.modelValue.includes(friend.userId));
});

</script>

<template>
  <div class="friend-selector-section">
    <div class="friend-selector-header">
      <h4>{{ discoveryOption === 'SelectedUsers' ? '공개할 친구 선택' : '비공개할 친구 선택' }}</h4>
    </div>
    
    <div class="friend-search-container">
      <input 
        v-model="friendSearchText" 
        @input="onFriendSearchInput"
        @focus="isFriendSearchFocused = true"
        @blur="hideFriendSearchResults"
        placeholder="친구 검색..." 
        class="friend-search-input"
      />
      
      <div v-if="isFriendSearchFocused && friendSearchText" class="friend-search-dropdown">
        <div v-if="friendSearchResults.length === 0" class="no-results">검색 결과가 없습니다.</div>
        <div v-else v-for="user in friendSearchResults" :key="user.userId" 
             @click="selectFriendFromSearch(user)" class="friend-search-item">
          <img :src="(user as any).profileImageUrl || '/src/assets/images/default_profile_image.jpg'" 
               class="friend-search-avatar">
          <div class="friend-search-info">
            <div class="friend-search-name">{{ user.nickname }}</div>
            <div class="friend-search-handle">@{{ user.handle }}</div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="modelValue.length > 0" class="selected-friends-display">
      <div class="selected-friends-header">선택된 친구 ({{ modelValue.length }}명)</div>
      <div class="selected-friends-list">
        <div v-for="friend in getSelectedFriends" :key="friend.userId" class="selected-friend-item">
          <img :src="(friend as any).profileImageUrl || '/src/assets/images/default_profile_image.jpg'" 
               class="selected-friend-avatar">
          <span class="selected-friend-name">{{ friend.nickname }}</span>
          <button @click="toggleFriendSelection(friend.userId)" class="remove-friend-btn">×</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* CreatePost.vue에서 복사해온 스타일 */
.friend-selector-section {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 16px;
  margin: 16px 0;
  border: 1px solid #e9ecef;
}

.friend-selector-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.friend-selector-header h4 {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: #495057;
}

.friend-search-container {
  position: relative;
  margin-bottom: 16px;
}

.friend-search-input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 0.9rem;
  background-color: white;
  transition: border-color 0.2s;
}

.friend-search-input:focus {
  outline: none;
  border-color: #ed664d;
}

.friend-search-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  background: white;
  border-radius: 6px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  max-height: 200px;
  overflow-y: auto;
  z-index: 1000;
  border: 1px solid #e9ecef;
}

.friend-search-item {
  display: flex;
  align-items: center;
  padding: 10px 12px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.friend-search-item:hover {
  background-color: #f8f9fa;
}

.friend-search-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  margin-right: 10px;
  object-fit: cover;
}

.friend-search-info {
  flex: 1;
}

.friend-search-name {
  font-weight: 500;
  font-size: 0.9rem;
  margin-bottom: 2px;
}

.friend-search-handle {
  font-size: 0.8rem;
  color: #6c757d;
}

.no-results {
  padding: 16px;
  text-align: center;
  color: #6c757d;
  font-size: 0.9rem;
}

.selected-friends-display {
  background-color: white;
  border-radius: 6px;
  padding: 12px;
  border: 1px solid #e9ecef;
}

.selected-friends-header {
  font-weight: 600;
  font-size: 0.9rem;
  color: #495057;
  margin-bottom: 8px;
}

.selected-friends-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.selected-friend-item {
  display: flex;
  align-items: center;
  background-color: #e3f2fd;
  border: 1px solid #2196f3;
  border-radius: 16px;
  padding: 4px 8px 4px 4px;
  font-size: 0.85rem;
}

.selected-friend-avatar {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  margin-right: 6px;
  object-fit: cover;
}

.selected-friend-name {
  font-weight: 500;
  margin-right: 6px;
}

.remove-friend-btn {
  background: none;
  border: none;
  color: #2196f3;
  cursor: pointer;
  font-size: 1rem;
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background-color 0.2s;
}

.remove-friend-btn:hover {
  background-color: rgba(33, 150, 243, 0.1);
}
</style>