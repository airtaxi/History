<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import apiClient from '@/api';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const isLoading = ref(false);
const errorMessage = ref('');

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

const handleGoogleLogin = () => {
  isLoading.value = true;
  sessionStorage.setItem('login_provider', 'Google');
  const frontendRedirectUrl = `${window.location.origin}${route.path}`;
  window.location.href = `${apiBaseUrl}/api/auth/google/login?redirectUrl=${encodeURIComponent(frontendRedirectUrl)}`;
};

const handleAppleLogin = () => {
  isLoading.value = true;
  sessionStorage.setItem('login_provider', 'Apple');
  const frontendRedirectUrl = `${window.location.origin}${route.path}`;
  window.location.href = `${apiBaseUrl}/api/auth/apple/login?redirectUrl=${encodeURIComponent(frontendRedirectUrl)}`;
};

onMounted(async () => {
  const idToken = route.query.id_token as string;
  // 저장된 제공자 정보 읽기
  const provider = sessionStorage.getItem('login_provider');

  if (idToken && provider) { // idToken과 provider가 모두 있을 때만 실행
    isLoading.value = true;
    // 사용 후 즉시 삭제하여 다음 동작에 영향 없도록 함
    sessionStorage.removeItem('login_provider');

    try {
      const response = await apiClient.post('/api/User/login', {
        IdToken: idToken,
        Provider: provider // 동적으로 가져온 provider 값 사용
      });

      authStore.setTokens(response.data.accessToken, response.data.refreshToken);
      await router.push('/');

    } catch (error: any) {
      if (error.response && error.response.status === 404) {
        await router.push({ path: '/profile-setup', query: { id_token: idToken, provider: provider } }); // provider 정보도 함께 전달
      } else if (error.response && error.response.status === 403) {
        errorMessage.value = '가입 승인 대기 중입니다. 관리자에게 문의하세요.';
      } else {
        errorMessage.value = '로그인에 실패했습니다. 다시 시도해주세요.';
        console.error('Login failed:', error);
      }
    } finally {
      isLoading.value = false;
      router.replace({ query: {} });
    }
  }
});

</script>

<template>
  <div class="login-container">
    <div class="login-card">
      <div v-if="isLoading" class="loading-overlay">
        <div class="spinner"></div>
        <p>로그인 중입니다...</p>
      </div>

      <div class="logo-container">
        <img src="@/assets/images/icon_nobg_black.png" alt="History 로고" class="logo-image">
        <h1 class="app-title">History</h1>
      </div>
      <p class="tagline">당신의 이야기, 히스토리</p>

      <p class="login-guide">
        구글/애플 계정으로 간편하게 로그인하세요.<br>
        기존 계정이 없을 경우 회원가입 페이지로 연동됩니다.
      </p>

      <button @click="handleGoogleLogin" class="google-login-btn" :disabled="isLoading">
        <svg class="google-icon" width="20" height="20" viewBox="0 0 18 18" xmlns="http://www.w3.org/2000/svg"><g fill="none" fill-rule="evenodd"><path d="M17.64 9.2045c0-.6381-.0573-1.2518-.1636-1.8409H9.1818v3.4818h4.7909c-.2045 1.125-.8273 2.0782-1.7818 2.7227v2.2591h2.9091c1.7045-1.5682 2.6864-3.8727 2.6864-6.6227z" fill="#4285F4"></path><path d="M9.1818 18c2.4455 0 4.4955-.8045 5.9864-2.1818l-2.9091-2.2591c-.8045.5409-1.8409.8591-3.0773.8591-2.3591 0-4.3636-1.5818-5.0818-3.7182H1.0818v2.3318C2.5636 15.8136 5.6091 18 9.1818 18z" fill="#34A853"></path><path d="M4.0955 10.71c-.1136-.3273-.1818-.6818-.1818-1.0455s.0682-.7182.1818-1.0455V6.2864H1.0818C.3864 7.7364 0 9.3227 0 11s.3864 3.2636 1.0818 4.7136l3.0137-2.3318z" fill="#FBBC05"></path><path d="M9.1818 3.5455c1.3227 0 2.5182.4545 3.4409 1.3455l2.5818-2.5818C13.6727.6364 11.6273 0 9.1818 0 5.6091 0 2.5636 2.1864 1.0818 5.2864l3.0137 2.3318c.7182-2.1364 2.7227-3.7182 5.0864-3.7182z" fill="#EA4335"></path></g></svg>
        <span>Google 계정으로 로그인/회원가입</span>
      </button>

      <button @click="handleAppleLogin" class="apple-login-btn" :disabled="isLoading">
        <svg class="apple-icon" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" d="M16.365 1.43c0 1.14-.478 2.145-1.2 2.886-.765.765-2.025 1.338-3.14 1.253-.12-1.11.39-2.295 1.17-3.03.855-.84 2.25-1.38 3.17-1.11zM20.25 17.805c-.585 1.44-1.29 2.865-2.34 2.91-1.005.045-1.32-.66-2.745-.66-1.425 0-1.785.63-2.76.705-1.17.09-2.07-1.245-2.85-2.67-1.545-2.76-2.7-7.77-1.14-10.95.81-1.68 2.295-2.745 3.96-2.79 1.095-.015 2.13.72 2.76.72.6 0 1.755-.885 2.955-.75.51.015 1.935.21 2.85 1.59-.075.045-1.695.99-1.665 2.955.045 2.34 1.965 3.12 2.01 3.15-.045.075-1.29.885-1.275 2.655.015 2.115 1.59 2.805 1.62 2.82z"/></svg>
        <span>Apple 계정으로 로그인</span>
      </button>

      <div v-if="errorMessage" class="error-message">{{ errorMessage }}</div>

      <div class="divider"></div>

      </div>
  </div>
</template>

<style scoped>
.apple-login-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  width: 100%;
  padding: 12px;
  font-size: 1rem;
  font-weight: 500;
  border-radius: 8px;
  cursor: pointer;
  background-color: #000;
  color: white;
  border: 1px solid #333;
  transition: background-color 0.2s, box-shadow 0.2s;
  margin-top: 10px;
}

.apple-login-btn:hover:not(:disabled) {
  background-color: #222;
}

.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background-image: url('@/assets/images/GettyImages-jv13433179.jpg');
  background-size: cover;
  background-position: center;
}

.login-card {
  position: relative;
  overflow: hidden;
  width: 100%;
  max-width: 400px;
  padding: 40px 30px;
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  margin: 20px;
}

.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(255, 255, 255, 0.8);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 10;
}

.spinner {
  border: 4px solid #f3f3f3;
  border-top: 4px solid #ed664d;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  animation: spin 1s linear infinite;
  margin-bottom: 15px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-message {
  color: red;
  margin-top: 15px;
  font-size: 0.9rem;
}

.logo-image { width: 40px; height: 40px; }
.logo-container { display: flex; align-items: center; gap: 10px; margin-bottom: 8px; }
.app-title { font-size: 2rem; font-weight: 600; margin: 0; color: #333; }
.tagline { font-size: 1rem; color: #555; margin-bottom: 30px; }
.login-guide { font-size: 0.9rem; color: #666; margin-bottom: 25px; line-height: 1.5; }
.google-login-btn { display: inline-flex; align-items: center; justify-content: center; gap: 12px; width: 100%; padding: 12px; font-size: 1rem; font-weight: 500; border-radius: 8px; cursor: pointer; transition: background-color 0.2s, box-shadow 0.2s; background-color: #ed664d; color: white; border: 1px solid #d45f3e; }
.google-login-btn:hover:not(:disabled) { background-color: #e05a40; }
.google-login-btn:disabled { background-color: #f9c5b9; cursor: not-allowed; }
.divider { width: 80%; height: 1px; background-color: #e0e0e0; margin: 30px 0; }
.signup-link { font-size: 0.9rem; color: #555; text-decoration: none; transition: color 0.2s; }
.signup-link:hover { color: #000; text-decoration: underline; }
</style>
