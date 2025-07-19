<!--
 * UserListModal.vue
 *
 * 이 컴포넌트는 특정 게시물을 공유하거나 리포스트한 사용자 목록을 표시하는 모달입니다.
 * 사용자 프로필 이미지와 닉네임을 보여주며, 각 사용자를 클릭하여 프로필 페이지로 이동할 수 있습니다.
 *
 * @props {
 *   show: boolean - 모달의 표시 여부.
 *   users: Array<any> - 표시할 사용자 객체 배열 (sharedAndRepostedUsers 구조).
 *   title: string - 모달의 제목.
 * }
 * @emits {
 *   close: 모달을 닫을 때 발생.
 * }
-->
<template>
  <Teleport to="body">
    <div v-if="show" class="user-list-overlay" @click.self="$emit('close')">
      <div class="user-list-content" @click.stop>
        <h3 class="modal-title">{{ title }}</h3>
        <ul class="shared-users-list">
          <li v-for="item in users" :key="item.user.userId" style="display: flex; align-items: center; gap: 10px;">
            <img
              :src="profileBlobUrlMap[item.user.userId] || '/src/assets/images/default_profile_image.jpg'"
              alt="프로필 이미지"
              style="width: 28px; height: 28px; border-radius: 50%; object-fit: cover; cursor: pointer;"
              @click.stop="goToUserProfile(item.user.userId)"
            />
            <RouterLink :to="`/user/${item.user.userId}`" @click="$emit('close')" class="user-nickname-link">
              {{ item.user.nickname || item.user.handle || '알 수 없음' }}
            </RouterLink>
          </li>
          <li v-if="users.length === 0" style="text-align: center; color: #999; padding: 10px;">
            목록이 비어 있습니다.
          </li>
        </ul>
        <button @click="$emit('close')" class="modal-close">닫기</button>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { defineProps, defineEmits, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useMediaLoader } from '../../composables/useMediaLoader';

const props = defineProps<{
  show: boolean;
  users: Array<any>;
  title: string;
}>();

const emit = defineEmits(['close']);
const router = useRouter();
const { profileBlobUrlMap, getMediaBlobUrl } = useMediaLoader();

/**
 * 사용자 프로필 페이지로 이동합니다.
 * @param {string} userId - 이동할 사용자의 ID.
 */
const goToUserProfile = (userId: string) => {
  router.push(`/user/${userId}`);
};

/**
 * 모달이 열릴 때 사용자 프로필 이미지를 로드합니다.
 */
const loadProfileImages = async () => {
  const usersToFetch = new Map<string, string>();
  props.users.forEach(item => {
    const userId = item.user.userId;
    const mediaId = item.user.profileThumbnailMediaId;
    if (mediaId && !profileBlobUrlMap.value[userId]) {
      usersToFetch.set(userId, mediaId);
    }
  });

  await Promise.all(Array.from(usersToFetch.entries()).map(async ([userId, mediaId]) => {
    const blobUrl = await getMediaBlobUrl(mediaId);
    profileBlobUrlMap.value[userId] = blobUrl;
  }));
};

// 모달이 열릴 때마다 프로필 이미지 로드
watch(() => props.show, (isOpened) => {
  if (isOpened) {
    loadProfileImages();
  }
}, { immediate: true });

</script>

<style scoped>
.user-list-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 10000;
}
.user-list-content {
  background: white;
  padding: 24px;
  border-radius: 12px;
  width: 320px;
  max-height: 80vh;
  overflow-y: auto;
}

.modal-title {
  font-weight: bold;
  margin-bottom: 12px;
  text-align: center;
}
.shared-users-list {
  list-style: none;
  padding: 0;
  margin: 0 0 12px 0;
}
.shared-users-list li {
  margin: 6px 0;
}
.modal-close {
  width: 100%;
  padding: 8px;
  background: #ed664d;
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: bold;
  cursor: pointer;
}
.user-nickname-link {
  color: #212529;
  text-decoration: none;
  font-weight: 500;
  transition: color 0.2s;
}

.user-nickname-link:hover {
  color: #ed664d;
}
</style>
