<!--
 * PostHeader.vue
 *
 * 이 컴포넌트는 게시물의 헤더 부분을 렌더링합니다.
 * 작성자 정보(아바타, 닉네임), 작성 시간, 그리고 게시물 관리(수정/삭제/신고)를 위한 "더보기" 메뉴를 표시합니다.
 *
 * @props {
 *   user: { avatar: string; nickname: string; } - 게시물 작성자 정보.
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
      <img :src="user.avatar" alt="User Avatar" class="avatar" />
      <span class="nickname">{{ user.nickname }}</span>
    </div>
    <!-- Created time -->
    <span class="created-at">{{ formatRelativeTime(createdAt) }}</span>
    <!-- More options menu -->
    <div class="more-options">
      <button @click="toggleDropdown">...</button>
      <div v-if="showDropdown" class="dropdown-menu">
        <button v-if="canEdit" @click="$emit('edit')">Edit</button>
        <button v-if="canEdit" @click="$emit('delete')">Delete</button>
        <button @click="$emit('report')">Report</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

interface User {
  avatar: string;
  nickname: string;
}

const props = defineProps({
  user: {
    type: Object as () => User,
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
const formatRelativeTime = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diff = now.getTime() - date.getTime();
  const seconds = Math.floor(diff / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);

  if (days > 0) {
    return `${days} days ago`;
  } else if (hours > 0) {
    return `${hours} hours ago`;
  } else if (minutes > 0) {
    return `${minutes} minutes ago`;
  } else {
    return `${seconds} seconds ago`;
  }
};
</script>

<style scoped>
.post-header {
  display: flex;
  align-items: center;
  padding: 10px;
  border-bottom: 1px solid #eee;
}

.author-info {
  display: flex;
  align-items: center;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
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
  margin-left: 10px;
}

.dropdown-menu {
  position: absolute;
  top: 100%;
  right: 0;
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
