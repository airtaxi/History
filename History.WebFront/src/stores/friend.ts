import { defineStore } from 'pinia';
import { ref } from 'vue';
import apiClient from '@/api';
import { useAuthStore } from './auth';
import type { UserResponseDto } from '@/types';

export const useFriendStore = defineStore('friend', () => {
  const friends = ref<UserResponseDto[]>([]);
  const isLoading = ref(false);

  async function fetchFriends() {
    if (friends.value.length > 0 || isLoading.value) return;
    const authStore = useAuthStore();

    try {
      isLoading.value = true;
      if (!authStore.user && !authStore.isLoading) {
        await authStore.fetchMe();
      }
      const me = authStore.user;
      if (!me) return;

      const res = await apiClient.get(`/api/Friendship/${me.userId}`);
      friends.value = res.data;
    } catch (e) {
      console.error('친구 목록 로딩 실패:', e);
      friends.value = [];
    } finally {
      isLoading.value = false;
    }
  }

  return {
    friends,
    fetchFriends,
    isLoading,
  };
});
