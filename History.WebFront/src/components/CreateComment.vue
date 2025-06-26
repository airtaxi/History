<script setup lang="ts">
import { ref, defineProps, defineEmits, defineExpose } from 'vue';
import apiClient from '@/api';

const props = defineProps<{ postId: string }>();
const emit = defineEmits(['comment-created']);

const newCommentText = ref('');
const textareaRef = ref<HTMLTextAreaElement | null>(null);
const attachedImage = ref<File | null>(null);
const attachedLink = ref('');
const showImagePreview = ref(false);
const imagePreviewUrl = ref('');

const submitComment = async () => {
  if (!newCommentText.value.trim() && !attachedImage.value && !attachedLink.value.trim()) {
    alert('댓글 내용을 입력하세요!');
    return;
  }

  const contents: any[] = [];

  if (newCommentText.value.trim()) {
    contents.push({
      $type: 'text',
      Text: newCommentText.value.trim(),
    });
  }

  if (attachedLink.value.trim()) {
    contents.push({
      $type: 'externalUrl',
      Url: attachedLink.value.trim(),
    });
  }

  if (attachedImage.value && attachedImage.value.name) {
    contents.push({
      $type: 'upload',
      FileName: attachedImage.value.name,
    });
  }

  const jsonPayload = { Contents: contents };

  const formData = new FormData();
  formData.append('JsonData', JSON.stringify(contents));

  if (attachedImage.value) {
    formData.append('Files', attachedImage.value);
  }

  console.log('[FormData 디버깅 시작]');
  console.log('JsonData (stringified):', JSON.stringify(contents));
  if (attachedImage.value) {
    console.log('첨부된 파일:', {
      name: attachedImage.value.name,
      size: attachedImage.value.size,
      type: attachedImage.value.type,
    });
  } else {
    console.log('첨부된 파일 없음');
  }

  // FormData 전체 반복 출력 (보이는 key-value 확인)
  for (const [key, value] of formData.entries()) {
    if (value instanceof File) {
      console.log(`📎 ${key}:`, {
        name: value.name,
        size: value.size,
        type: value.type,
      });
    } else {
      console.log(`📨 ${key}:`, value);
    }
  }
  console.log('[FormData 디버깅 끝]');

  try {
    const res = await apiClient.post(`/api/Comment/${props.postId}`, formData);
    console.log('서버 응답:', res);
    
    // 성공 시 폼 초기화
    clearForm();
    
    emit('comment-created');
  } catch (e: any) {
    console.error('댓글 작성 실패:', e);
    console.log('응답 data:', e.response?.data);
    console.log('응답 status:', e.response?.status);
    console.log('응답 headers:', e.response?.headers);
  }
};

const handleFileChange = (event: Event) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (file) {
    attachedImage.value = file;
    imagePreviewUrl.value = URL.createObjectURL(file);
  }
};

const addMention = (nickname: string) => {
  const mention = `@${nickname} `;
  newCommentText.value += mention;
  textareaRef.value?.focus();
};

// 폼 초기화 함수 추가
const clearForm = () => {
  newCommentText.value = '';
  attachedImage.value = null;
  attachedLink.value = '';
  showImagePreview.value = false;
  imagePreviewUrl.value = '';
};

defineExpose({ addMention, clearForm });
</script>

<template>
  <div class="create-comment-form">
    <textarea ref="textareaRef" v-model="newCommentText" placeholder="댓글을 입력하세요..." />

    <div class="attach-section">
      <label class="upload-btn">
        🖼 이미지
        <input type="file" accept="image/*,video/*" @change="handleFileChange" hidden />
      </label>

      <button @click="showImagePreview = true" v-if="attachedImage">미리보기</button>

      <input v-model="attachedLink" placeholder="링크 첨부 (https://...)" class="link-input" />
    </div>

    <button @click="submitComment">등록</button>

    <div v-if="showImagePreview" class="modal-overlay" @click.self="showImagePreview = false">
      <img :src="imagePreviewUrl" class="image-popup" />
    </div>
  </div>
</template>

<style scoped>
.create-comment-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-top: 1px solid #eee;
}
textarea {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid #ddd;
  border-radius: 6px;
  padding: 10px;
  resize: vertical;
  min-height: 60px;
}
.attach-section {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}
.upload-btn {
  background-color: #f0f0f0;
  padding: 6px 10px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.9rem;
  border: 1px solid #ddd;
}
.link-input {
  flex: 1;
  min-width: 200px;
  padding: 6px 8px;
  border: 1px solid #ddd;
  border-radius: 6px;
}
button {
  background-color: #ed664d;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  align-self: flex-end;
}
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 999;
}
.image-popup {
  max-width: 80%;
  max-height: 80%;
  border-radius: 8px;
  box-shadow: 0 0 12px rgba(0, 0, 0, 0.3);
}
</style>
