import axios from 'axios';
import { useAuthStore } from '@/stores/auth';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL
  // 기본 Content-Type 헤더를 삭제합니다.
  // axios가 데이터에 맞춰 자동으로 올바른 헤더를 설정해줍니다.
});

// 모든 API 요청이 보내지기 전에 로그인 토큰을 헤더에 추가하는 로직 (이전과 동일)
apiClient.interceptors.request.use(config => {
  const authStore = useAuthStore();
  if (authStore.accessToken) {
    config.headers.Authorization = `Bearer ${authStore.accessToken}`;
  }
  return config;
}, error => {
  return Promise.reject(error);
});

export default apiClient;