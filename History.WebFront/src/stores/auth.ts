import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import apiClient from '@/api';
import type { UserResponseDto } from '@/types'; // 꼭 타입 임포트해줘

export const useAuthStore = defineStore('auth', () => {
  // 상태
  const accessToken = ref<string | null>(null);
  const refreshToken = ref<string | null>(null);
  const user = ref<UserResponseDto | null>(null);

  // 게터
  const isAuthenticated = computed(() => !!accessToken.value);

  // 액션
  function setTokens(newAccessToken: string, newRefreshToken: string) {
    accessToken.value = newAccessToken;
    refreshToken.value = newRefreshToken;
    apiClient.defaults.headers.common['Authorization'] = `Bearer ${newAccessToken}`;
  }

  function logout() {
    accessToken.value = null;
    refreshToken.value = null;
    user.value = null;
    delete apiClient.defaults.headers.common['Authorization'];
  }

  function fetchMe() {
    return apiClient.get('/api/User/me')
      .then((res) => {
        user.value = res.data;
      })
      .catch(() => {
        user.value = null;
      });
  }

  return {
    accessToken,
    refreshToken,
    user,
    isAuthenticated,
    setTokens,
    fetchMe,
    logout,
  };
}, {
  persist: true, 
});
