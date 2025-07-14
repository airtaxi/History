<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import apiClient from '@/api'; // API 클라이언트
import type { PostResponseDto } from '@/types'; // Post 타입
import PostCard from '@/components/PostCard.vue'; // 기존에 만드신 PostCard 컴포넌트

// Vue Router의 useRoute를 사용해 현재 URL의 정보를 가져옵니다.
const route = useRoute();

// 컴포넌트의 상태를 관리할 ref 변수들을 선언합니다.
const post = ref<PostResponseDto | null>(null); // 불러온 게시물 데이터
const isLoading = ref(true); // 로딩 중인지 여부
const error = ref<string | null>(null); // 에러 메시지

// 컴포넌트가 화면에 마운트된 후 실행될 로직입니다.
onMounted(async () => {
  // 1. URL 파라미터에서 게시물 ID를 가져옵니다. (예: /post/123 -> '123')
  const postId = route.params.postId as string;

  if (!postId) {
    error.value = '게시물 ID가 올바르지 않습니다.';
    isLoading.value = false;
    return;
  }

  try {
    // 2. API 클라이언트를 사용해 서버에 특정 게시물의 데이터를 요청합니다.
    const response = await apiClient.get<PostResponseDto>(`/api/Post/${postId}`);
    post.value = response.data; // 성공 시, 응답 데이터를 post 상태에 저장
  } catch (err) {
    // 3. 데이터 요청 실패 시, 에러 상태를 설정합니다.
    console.error('게시물 상세 정보를 불러오는 데 실패했습니다:', err);
    error.value = '게시물을 찾을 수 없거나 불러오는 중 문제가 발생했습니다.';
  } finally {
    // 4. 성공하든 실패하든 로딩 상태를 종료합니다.
    isLoading.value = false;
  }
});
</script>

<template>
  <div class="post-detail-container">
    <div v-if="isLoading" class="status-message">
      게시물을 불러오는 중입니다...
    </div>

    <div v-else-if="error" class="status-message error">
      {{ error }}
    </div>

    <PostCard v-else-if="post" :post="post" />
  </div>
</template>

<style scoped>
.post-detail-container {
  max-width: 600px; 
  margin: 20px auto;
  padding: 0 10px;
}

.status-message {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 200px;
  color: #888;
  font-size: 1rem;
}

.status-message.error {
  color: #d32f2f;
}
</style>