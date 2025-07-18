<!--
 * PostHeader.vue
 *
 * 이 컴포넌트는 게시물의 헤더 부분을 렌더링합니다.
 * 작성자 정보(아바타, 닉네임), 작성 시간, 그리고 게시물 관리(수정/삭제/신고)를 위한 "더보기" 메뉴를 표시합니다.
 *
 * @props {
 *   user: { userId: string; nickname: string; } - 게시물 작성자 정보.
 *   profileImageUrl: string - 프로필 이미지의 Blob URL 또는 기본 이미지 경로.
 *   createdAt: string - 게시물 작성 시간 (ISO 8601 형식의 문자열).
 *   canEdit: boolean - 현재 사용자가 게시물을 수정/삭제할 권한이 있는지 여부.
 * }
 * @emits {
 *   edit: 게시물 수정 버튼 클릭 시 발생.
 *   delete: 게시물 삭제 버튼 클릭 시 발생.
 *   report: 게시물 신고 버튼 클릭 시 발생.
 * }
-->

<template>
  <div class="post-header">
    <div class="author-info">
      
      <RouterLink :to="`/user/${post.user.userId}`">
        <img :src="profileImageUrl" alt="User Avatar" class="avatar" />
      </RouterLink>

      <div class="postinfo-container">
        
        <RouterLink :to="`/user/${post.user.userId}`" class="nickname-link">
          <span class="nickname">{{ post.user.nickname }}</span>
        </RouterLink>
        
        <span class="created-at">{{ formatRelativeTime(post.createdAt) }}</span>
      </div>
    </div>

    <div class="more-options" @click.stop>
      <button class="more-options" @click="toggleDropdown">...</button>
      <div v-if="showDropdown" class="dropdown-menu">
        <template v-if="canEdit">
          <button @click="$emit('promote')">게시글 홍보</button>
          <button @click="openDiscoveryModal">공개범위 설정</button>
          <button @click="$emit('toggle-pin')">프로필에 고정/해제</button>
          <button @click="$emit('delete')">게시글 삭제</button>
        </template>
        
        <button v-if="!canEdit" @click="$emit('report')">게시글 신고</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { formatRelativeTime } from '@/components/src/utils/timeUtils';
import type { PostResponseDto } from '@/types';

interface User {
  userId: string;
  nickname: string;
}

const props = defineProps({
  post: {
    type: Object as () => PostResponseDto,
    required: true,
  },
  profileImageUrl: {
    type: String,
    required: true,
  },
  canEdit: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits([
  'open-discovery-modal','promote', 'change-discovery', 'toggle-pin', 'toggle-bookmark', 
  'toggle-notifications', 'delete', 'report'
]);

const showDropdown = ref(false);

const toggleDropdown = () => {
  showDropdown.value = !showDropdown.value;
};

const openDiscoveryModal = () => {
  emit('open-discovery-modal');
}

</script>

<style scoped>
.post-header {
  display: flex;
  align-items: center;
  padding: 0 0 10px 0;
  border-bottom: 1px solid #eee;
  justify-content: space-between; 
}

.author-info {
  display: flex;
  align-items: center;
}

.postinfo-container{
  display: flex;
  flex-direction: column;
  align-items: left;
}
.avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    object-fit: cover;
    margin-right: 10px;
}

.nickname {
  font-weight: bold;
}

.nickname-link {
  color: inherit; 
  text-decoration: none; 
}


.created-at {
  margin-right: auto;
  color: #888;
  font-size: 0.9em;
}

.more-options {
  position: relative;
  margin-left: auto;
}

button.more-options {
  background: none;
    border: none;
    font-size: 20px;
    cursor: pointer;
}

.dropdown-menu {
  position: absolute;
  top: 100%;
  right: 0;
  width: 10rem;
  background-color: white;
  border: 1px solid #ccc;
  border-radius: 5px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 10;
}

.dropdown-menu button {
  display: block;
  width: 100%;
  padding: 8px 12px;
  border: none;
  background: none;
  text-align: left;
  cursor: pointer;
}

.dropdown-menu button:hover {
  background-color: #f0f0f0;
}
</style>
