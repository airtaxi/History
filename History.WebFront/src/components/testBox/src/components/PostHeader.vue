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
    <!-- Author info (avatar, nickname) -->
    <div class="author-info">
      <img :src="profileImageUrl" alt="User Avatar" class="avatar" />
      <div class="postinfo-container">
        <span class="nickname">{{ user.nickname }}</span>
      <!-- Created time -->
        <span class="created-at">{{ formatRelativeTime(createdAt) }}</span>
      </div>
    </div>
    <!-- More options menu -->
    <div class="more-options" @click.stop> <!-- @click.stop 추가 -->
      <button class="more-options" @click="toggleDropdown">...</button>
      <div v-if="showDropdown" class="dropdown-menu">
        <button v-if="canEdit" @click="$emit('edit')">수정</button>
        <button v-if="canEdit" @click="$emit('delete')">삭제</button>
        <button @click="$emit('report')">신고</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

interface User {
  userId: string;
  nickname: string;
}

const props = defineProps({
  user: {
    type: Object as () => User,
    required: true,
  },
  profileImageUrl: {
    type: String,
    required: true,
  },
  createdAt: {
    type: String,
    required: true,
  },
  canEdit: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(['edit', 'delete', 'report']);

const showDropdown = ref(false);

/**
 * "더보기" 드롭다운 메뉴의 표시 여부를 토글합니다.
 */
const toggleDropdown = () => {
  showDropdown.value = !showDropdown.value;
};

/**
 * 주어진 날짜 문자열을 현재 시간과의 상대적인 시간으로 포맷합니다.
 * 예: "방금 전", "5분 전", "3시간 전", "2023-07-07 10:30"
 * @param {string} dateString - 포맷할 날짜 문자열 (ISO 8601 형식).
 * @returns {string} 포맷된 시간 문자열.
 */
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
</script>

<style scoped>
.post-header {
  display: flex;
  align-items: center;
  padding: 10px;
  border-bottom: 1px solid #eee;
  justify-content: space-between; /* 양쪽 끝으로 아이템 정렬 */
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

.created-at {
  margin-left: auto;
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
  width: 5rem;
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
