<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, RouterLink } from 'vue-router';
import apiClient from '@/api';
import type { UserResponseDto } from '@/types';
import ProfileEditView from '@/views/accounts/ProfileEditView.vue'; // 수정 폼 컴포넌트 import

const user = ref<UserResponseDto | null>(null);
const postCount = ref(0);
const friendCount = ref(0);
const isLoading = ref(true);
const isEditModalOpen = ref(false); // 팝업의 열림/닫힘 상태
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;
const getMediaUrl = (mediaId: string) => mediaId ? `${apiBaseUrl}/api/Media/${mediaId}` : '';

const fetchData = async () => {
  isLoading.value = true;
  try {
    const userResponse = await apiClient.get<UserResponseDto>('/api/User/me');
    user.value = userResponse.data;

    if (user.value) {
      const postCountResponse = await apiClient.get<{ count: number }>(`/api/Post/user/${user.value.userId}/count`);
      postCount.value = postCountResponse.data.count;

      const friendsResponse = await apiClient.get<UserResponseDto[]>(`/api/Friendship/${user.value.userId}`);
      friendCount.value = friendsResponse.data.length;
    }
  } catch (error) {
    console.error("프로필 정보 로딩 실패:", error);
  } finally {
    isLoading.value = false;
  }
};

onMounted(fetchData);

// 팝업이 닫히고 프로필이 업데이트되었을 때 실행될 함수
const handleProfileUpdated = () => {
  isEditModalOpen.value = false;
  fetchData(); // 데이터를 다시 불러와 화면을 갱신
};
</script>

<template>
  <div class="page-container">

    <main v-if="isLoading" class="loading-content">
      </main>

    <main v-else-if="user" class="profile-content">
      <div class="profile-page">
        <div class="profile-header">
          <div class="profile-actions">
            <button @click="isEditModalOpen = true" class="edit-profile-btn">프로필 수정</button>
          </div>
        </div>
        </div>
    </main>

    <ProfileEditView 
      v-if="isEditModalOpen" 
      @close="isEditModalOpen = false" 
      @profile-updated="handleProfileUpdated" 
    />
  </div>
</template>

<style scoped>
.profile-page { max-width: 980px; margin: 0 auto; background-color: #fff; }
.profile-header { position: relative; }
.background-image-wrapper { height: 250px; background-color: #f0f2f5; }
.background-image { width: 100%; height: 100%; object-fit: cover; }
.profile-info-bar { display: flex; justify-content: space-between; align-items: flex-end; padding: 0 24px; position: relative; top: -40px; margin-bottom: -40px; }
.profile-avatar-wrapper { border: 4px solid white; border-radius: 50%; width: 140px; height: 140px; background-color: white; }
.profile-avatar { width: 100%; height: 100%; border-radius: 50%; object-fit: cover; }
.profile-actions { padding-bottom: 12px; }
.edit-profile-btn { background-color: #ed664d; color: white; padding: 8px 16px; border-radius: 6px; text-decoration: none; font-weight: 600; }
.profile-details { padding: 0 24px 24px 24px; }
.nickname { font-size: 2rem; font-weight: 800; margin: 0 0 4px 0; }
.handle { font-size: 1rem; color: #666; margin-bottom: 16px; }
.description { font-size: 1rem; color: #333; margin-bottom: 16px; }
.stats-container { display: flex; gap: 24px; }
.stat { font-size: 1rem; }
.stat-value { font-weight: 600; margin-right: 4px; }
.stat-label { color: #666; }
.content-tabs { display: flex; border-top: 1px solid #eee; padding: 0 24px; }
.tab { padding: 16px 0; margin-right: 24px; font-weight: 600; cursor: pointer; border-bottom: 2px solid transparent; }
.tab.active { color: #ed664d; border-bottom-color: #ed664d; }
</style>