<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import apiClient from '@/api';
import { useAuthStore } from '@/stores/auth';
import defaultProfileImage from '@/assets/images/default_profile_image.jpg';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

// 데이터 상태 관리
const nickname = ref('');
const handle = ref('');
const agreedToTerms = ref(false);
const profileImageFile = ref<File | null>(null);
const profileImageUrl = ref<string | null>(null);

const selectedYear = ref<number | null>(null);
const selectedMonth = ref<number | null>(null);
const selectedDay = ref<number | null>(null);

const years = ref<number[]>([]);
const months = ref<number[]>([]);
const days = ref<number[]>([]);

const isLoading = ref(false);
const errorMessage = ref('');

const idToken = route.query.id_token as string;

onMounted(() => {
  if (!idToken) {
    alert('잘못된 접근입니다. 로그인 페이지로 돌아갑니다.');
    router.push('/login');
    return;
  }
  const currentYear = new Date().getFullYear();
  for (let i = currentYear; i >= currentYear - 100; i--) years.value.push(i);
  for (let i = 1; i <= 12; i++) months.value.push(i);
  for (let i = 1; i <= 31; i++) days.value.push(i);
});

const fileInput = ref<HTMLInputElement | null>(null);

const onProfileImageClick = () => {
  fileInput.value?.click();
};

const onFileSelected = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    profileImageFile.value = target.files[0];
    profileImageUrl.value = URL.createObjectURL(target.files[0]);
  }
};

const isConfirmDisabled = computed(() => {
  return !nickname.value || !handle.value || !agreedToTerms.value || isLoading.value;
});

const handleCancel = () => {
  router.push('/login');
};

const handleConfirm = async () => {
  if (isConfirmDisabled.value) return;
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const registerResponse = await apiClient.post('/api/User/register', {
      idToken: idToken,
      provider: 'Google',
      name: nickname.value
    });

    const { accessToken, refreshToken } = registerResponse.data;
    authStore.setTokens(accessToken, refreshToken);
    
    const updateTasks = [];
    updateTasks.push(apiClient.put('/api/User/handle', { handle: handle.value }));
    
    if (selectedYear.value && selectedMonth.value && selectedDay.value) {
      const birthday = new Date(Date.UTC(selectedYear.value, selectedMonth.value - 1, selectedDay.value));
      updateTasks.push(apiClient.put('/api/User/birthday', { birthday: birthday.toISOString() }));
    }

    if (profileImageFile.value) {
      const formData = new FormData();
      formData.append('file', profileImageFile.value);
      updateTasks.push(apiClient.put('/api/User/profile-media', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      }));
    }

    await Promise.all(updateTasks);
    await router.push('/');

  } catch (error: any) {
    if (error.response && typeof error.response.data === 'string') {
        errorMessage.value = `프로필 생성 실패: ${error.response.data}`;
    } else {
        errorMessage.value = '알 수 없는 오류로 프로필 생성에 실패했습니다.';
    }
    console.error("프로필 생성 실패:", error);
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <div class="layout-container">
    <header class="main-header">
      <div class="header-content">
        <RouterLink to="/" class="header-logo-link">
          <img src="@/assets/images/icon_nobg_white.png" alt="History 로고" class="header-logo-image">
          <span class="header-title">History</span>
        </RouterLink>
      </div>
    </header>

    <main class="content-area">
      <div class="profile-card">
        <h2>프로필 만들기</h2>
        <p class="subtitle">히스토리에서 사용할 나만의 프로필을 설정해주세요.</p>

        <div v-if="isLoading" class="loading-overlay">
          <div class="spinner"></div>
          <p>프로필을 생성 중입니다...</p>
        </div>

        <div class="profile-image-section">
          <div class="image-wrapper" @click="onProfileImageClick">
            <img :src="profileImageUrl || defaultProfileImage" alt="프로필 사진" class="profile-image">
            <div class="edit-icon">
               <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="white" d="M14.06 9.02l.92.92L5.92 19H5v-.92l9.06-9.06M17.66 3c-.25 0-.51.1-.7.29l-1.83 1.83l3.75 3.75l1.83-1.83c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.2-.2-.45-.29-.71-.29m-3.6 3.19L3 17.25V21h3.75L17.81 9.94l-3.75-3.75Z"/></svg>
            </div>
          </div>
          <input type="file" ref="fileInput" @change="onFileSelected" accept="image/*" style="display: none;" />
        </div>

        <div class="form-group name-group">
          <input type="text" v-model="nickname" placeholder="닉네임 (20자 이내)" class="form-input" maxlength="20">
        </div>
        
        <div class="form-group">
          <label>생일</label>
          <div class="birthday-group">
            <select v-model="selectedYear" class="form-select">
              <option :value="null" disabled>년</option>
              <option v-for="year in years" :key="year" :value="year">{{ year }}</option>
            </select>
            <select v-model="selectedMonth" class="form-select">
              <option :value="null" disabled>월</option>
              <option v-for="month in months" :key="month" :value="month">{{ month }}</option>
            </select>
            <select v-model="selectedDay" class="form-select">
              <option :value="null" disabled>일</option>
              <option v-for="day in days" :key="day" :value="day">{{ day }}</option>
            </select>
          </div>
        </div>
        
        <div class="form-group">
          <label>스토리 ID</label>
          <div class="handle-group">
            <span class="handle-prefix">history.com/</span>
            <input type="text" v-model="handle" placeholder="영문 또는 숫자 (4~15자)" class="form-input handle-input" maxlength="15">
          </div>
        </div>
        
        <div class="terms-group">
          <input type="checkbox" id="terms" v-model="agreedToTerms">
          <label for="terms">개인정보 수집 및 이용약관에 동의합니다.</label>
        </div>

        <div v-if="errorMessage" class="error-message">{{ errorMessage }}</div>
        
        <div class="button-group">
          <button @click="handleCancel" class="btn btn-secondary">취소</button>
          <button @click="handleConfirm" class="btn btn-primary" :disabled="isConfirmDisabled">확인</button>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
.layout-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background-color: #f0f2f5;
}

.main-header {
  background-color: #ed664d;
  padding: 0 20px;
  height: 60px;
  display: flex;
  align-items: center;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  flex-shrink: 0;
}

.header-content {
  width: 100%;
  max-width: 1200px;
}

.header-logo-link {
  display: flex;
  align-items: center;
  gap: 8px;
  text-decoration: none;
}

.header-logo-image {
  width: 32px;
  height: 32px;
}

.header-title {
  color: white;
  font-size: 1.5rem;
  font-weight: 600;
}

.content-area {
  flex-grow: 1;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 40px 20px;
}

.profile-card {
  width: 100%;
  max-width: 480px;
  padding: 40px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}
h2 { font-size: 1.8rem; font-weight: 600; text-align: center; margin-bottom: 8px; }
.subtitle { text-align: center; color: #666; margin-bottom: 30px; }
.loading-overlay { padding: 0 20px; position: absolute; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(255, 255, 255, 0.8); display: flex; flex-direction: column; align-items: center; justify-content: center; z-index: 10; }
.spinner { border: 4px solid #f3f3f3; border-top: 4px solid #ed664d; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin-bottom: 15px; }
@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
.profile-image-section { display: flex; justify-content: center; margin-bottom: 30px; }
.image-wrapper { position: relative; width: 120px; height: 120px; cursor: pointer; }
.profile-image { width: 100%; height: 100%; border-radius: 50%; object-fit: cover; border: 3px solid #eee; background-color: #f8f8f8; }
.edit-icon { position: absolute; bottom: 0; right: 0; background-color: #ed664d; border-radius: 50%; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; border: 2px solid white; }
.form-group { margin-bottom: 20px; }
.form-group label { display: block; font-weight: 500; margin-bottom: 8px; color: #333; text-align: left; }
.form-input, .form-select { width: 100%; padding: 12px 15px; border: 1px solid #ddd; border-radius: 8px; font-size: 1rem; transition: border-color 0.2s; box-sizing: border-box; }
.form-input:focus, .form-select:focus { outline: none; border-color: #ed664d; }
.name-group, .birthday-group, .handle-group { display: flex; gap: 10px; }
.handle-group { align-items: center; }
.handle-prefix { color: #888; font-size: 1rem; white-space: nowrap; }
.handle-input { flex-grow: 1; }
.terms-group { display: flex; align-items: center; gap: 8px; margin-bottom: 30px; }
.terms-group label { margin: 0; font-size: 0.9rem; color: #555; }
.error-message { color: red; margin-bottom: 15px; font-size: 0.9rem; text-align: center; }
.button-group { display: flex; gap: 10px; }
.btn { flex-grow: 1; padding: 12px; font-size: 1rem; font-weight: 500; border-radius: 8px; border: none; cursor: pointer; transition: background-color 0.2s; }
.btn-primary { background-color: #ed664d; color: white; }
.btn-primary:hover:not(:disabled) { background-color: #e05a40; }
.btn-primary:disabled { background-color: #f9c5b9; cursor: not-allowed; }
.btn-secondary { background-color: #e9ecef; color: #495057; }
.btn-secondary:hover { background-color: #dee2e6; }
</style>