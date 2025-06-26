<script setup lang="ts">
import { ref, onMounted } from 'vue';
import apiClient from '@/api';
import { defineEmits } from 'vue';
import type { UserResponseDto } from '@/types';

// 부모 컴포넌트(MyPageView)로 신호를 보내기 위한 이벤트 정의
const emit = defineEmits(['close', 'profile-updated']);

// API 로딩 및 에러 상태 관리
const isLoading = ref(true);
const errorMessage = ref('');

// 원본 사용자 정보와 폼에 바인딩될 데이터를 분리
const originalUser = ref<UserResponseDto | null>(null);
const nickname = ref('');
const handle = ref('');
const description = ref('');

// 이미지 미리보기 및 파일 관리를 위한 상태
const profileImageFile = ref<File | null>(null);
const profileImageUrl = ref<string | null>(null);
const backgroundImageFile = ref<File | null>(null);
const backgroundImageUrl = ref<string | null>(null);

const getMediaUrl = (mediaId: string) => mediaId ? `/api/Media/${mediaId}` : '';

// 컴포넌트가 로드될 때 현재 사용자 정보를 불러와 폼을 채움
onMounted(async () => {
  try {
    const response = await apiClient.get<UserResponseDto>('/api/User/me');
    originalUser.value = response.data;
    
    // 폼 데이터 초기화
    nickname.value = originalUser.value.nickname;
    handle.value = originalUser.value.handle;
    description.value = originalUser.value.description;
    profileImageUrl.value = getMediaUrl(originalUser.value.profileThumbnailMediaId) || '/src/assets/images/default_profile.png';
    backgroundImageUrl.value = getMediaUrl(originalUser.value.backgroundThumbnailMediaId) || '/src/assets/images/default_background.jpg';

  } catch (error) {
    console.error("프로필 정보 로딩 실패:", error);
    errorMessage.value = "프로필 정보를 불러오는데 실패했습니다.";
  } finally {
    isLoading.value = false;
  }
});

// 파일 선택 처리 함수
const handleFileSelection = (event: Event, target: 'profile' | 'background') => {
  const input = event.target as HTMLInputElement;
  if (input.files && input.files[0]) {
    const file = input.files[0];
    if (target === 'profile') {
      profileImageFile.value = file;
      profileImageUrl.value = URL.createObjectURL(file);
    } else {
      backgroundImageFile.value = file;
      backgroundImageUrl.value = URL.createObjectURL(file);
    }
  }
};

// 저장 버튼 클릭 시 실행될 로직
const handleSave = async () => {
  if (isLoading.value) return;
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const updateTasks: Promise<any>[] = [];

    // 변경된 항목만 API 호출 목록에 추가
    if (originalUser.value?.nickname !== nickname.value)
      updateTasks.push(apiClient.put('/api/User/nickname', { nickname: nickname.value }));
    
    if (originalUser.value?.handle !== handle.value)
      updateTasks.push(apiClient.put('/api/User/handle', { handle: handle.value }));
      
    if (originalUser.value?.description !== description.value)
      updateTasks.push(apiClient.put('/api/User/description', { description: description.value }));

    if (profileImageFile.value) {
      const formData = new FormData();
      formData.append('file', profileImageFile.value);
      updateTasks.push(apiClient.put('/api/User/profile-media', formData));
    }

    if (backgroundImageFile.value) {
      const formData = new FormData();
      formData.append('file', backgroundImageFile.value);
      updateTasks.push(apiClient.put('/api/User/background-media', formData));
    }
    
    // 변경사항이 있을 때만 API 호출 실행
    if (updateTasks.length > 0) {
      await Promise.all(updateTasks);
    }
    
    alert('프로필이 성공적으로 저장되었습니다.');
    emit('profile-updated'); // 부모에게 프로필이 업데이트되었음을 알림

  } catch (error: any) {
    console.error("프로필 저장 실패:", error);
    errorMessage.value = error.response?.data?.message || '프로필 저장에 실패했습니다.';
  } finally {
    isLoading.value = false;
  }
};

// 취소 버튼 로직
const handleCancel = () => {
  emit('close'); // 부모에게 닫기 신호 전송
};
</script>

<template>
  <div class="modal-overlay" @click.self="handleCancel">
    <div class="modal-content">
      <div v-if="isLoading" class="loading-spinner">
        <div class="spinner"></div>
      </div>
      <template v-else>
        <h2>프로필 수정</h2>
        
        <div class="form-group">
          <label for="nickname">닉네임</label>
          <input id="nickname" type="text" v-model="nickname" class="form-input">
        </div>

        <div class="form-group">
          <label for="handle">핸들 (ID)</label>
          <input id="handle" type="text" v-model="handle" class="form-input">
        </div>

        <div class="form-group">
          <label for="description">한 줄 소개</label>
          <textarea id="description" v-model="description" class="form-textarea"></textarea>
        </div>

        <div class="form-group">
          <label>프로필 사진</label>
          <input type="file" @change="event => handleFileSelection(event, 'profile')" accept="image/*">
          <img v-if="profileImageUrl" :src="profileImageUrl" class="image-preview profile-preview">
        </div>

        <div class="form-group">
          <label>배경 사진</label>
          <input type="file" @change="event => handleFileSelection(event, 'background')" accept="image/*">
          <img v-if="backgroundImageUrl" :src="backgroundImageUrl" class="image-preview background-preview">
        </div>

        <p v-if="errorMessage" class="error-text">{{ errorMessage }}</p>

        <div class="button-group">
          <button @click="handleCancel" class="btn-secondary">취소</button>
          <button @click="handleSave" class="btn-primary" :disabled="isLoading">
            {{ isLoading ? '저장 중...' : '저장하기' }}
          </button>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}
.modal-content {
  max-width: 500px; 
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
  background: white; 
  padding: 30px; 
  border-radius: 8px; 
  box-shadow: 0 5px 15px rgba(0,0,0,0.3);
}
h2 { text-align: center; margin-top: 0; margin-bottom: 30px; }
.form-group { margin-bottom: 20px; }
.form-group label { display: block; font-weight: 600; margin-bottom: 8px; }
.form-input, .form-textarea { width: 100%; padding: 10px; border: 1px solid #ccc; border-radius: 4px; font-size: 1rem; box-sizing: border-box; }
.form-textarea { min-height: 100px; resize: vertical; }
.image-preview { margin-top: 10px; display: block; border-radius: 4px; max-width: 100%; }
.profile-preview { width: 120px; height: 120px; border-radius: 50%; object-fit: cover; }
.background-preview { aspect-ratio: 16 / 9; object-fit: cover; }
.button-group { display: flex; justify-content: flex-end; gap: 10px; margin-top: 30px; }
.btn-primary { background-color: #ed664d; color: white; padding: 10px 20px; border-radius: 6px; border: none; cursor: pointer; }
.btn-primary:disabled { background-color: #f9c5b9; cursor: not-allowed; }
.btn-secondary { background-color: #e9ecef; color: #495057; padding: 10px 20px; border-radius: 6px; border: none; cursor: pointer; }
.error-text { color: red; text-align: center; margin-bottom: 15px; }
.loading-spinner { display: flex; justify-content: center; align-items: center; min-height: 400px; }
.spinner { border: 5px solid #f3f3f3; border-top: 5px solid #ed664d; border-radius: 50%; width: 50px; height: 50px; animation: spin 1s linear infinite; }
@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
</style>