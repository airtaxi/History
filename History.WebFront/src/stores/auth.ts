import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import apiClient from '@/api';
import type { UserResponseDto } from '@/types'; 

export const useAuthStore = defineStore('auth', () => {
  // 상태
  const accessToken = ref<string | null>(null);
  const refreshToken = ref<string | null>(null);
  const user = ref<UserResponseDto | null>(null);
  const isLoading = ref(false); 

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

  async function fetchMe() {
    if (user.value || isLoading.value) return; 

    try {
      isLoading.value = true;
      const res = await apiClient.get('/api/User/me');
      user.value = res.data;
    } catch {
      user.value = null;
    } finally {
      isLoading.value = false;
    }
  }

  return {
    accessToken,
    refreshToken,
    user,
    isAuthenticated,
    isLoading,
    setTokens,
    fetchMe,
    logout,
  };
}, {
  persist: true,
});

