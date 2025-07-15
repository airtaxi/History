<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import apiClient from '@/api';
import type { PostResponseDto } from '@/types';
import PostCard from '@/components/PostCard.vue'; // PostCard 컴포넌트 경로

const route = useRoute();
const posts = ref<PostResponseDto[]>([]);
const isLoading = ref(false);
const searchQuery = ref(route.query.q || '');

/**
 * API를 호출하여 게시글 검색 결과를 가져오는 함수
 */
const fetchSearchResults = async (query: string) => {
  if (!query) {
    posts.value = [];
    return;
  }
  
  isLoading.value = true;
  posts.value = []; // 새 검색 시 기존 결과 초기화

  try {
    const response = await apiClient.get<PostResponseDto[]>('/api/post/search', {
      params: { query }
    });
    posts.value = response.data;
  } catch (error) {
    console.error('게시글 검색 실패:', error);
    posts.value = [];
  } finally {
    isLoading.value = false;
  }
};

// 컴포넌트가 마운트될 때 첫 검색 실행
onMounted(() => {
  fetchSearchResults(searchQuery.value as string);
});

// URL의 검색어가 변경될 때마다 다시 검색 실행 (헤더에서 새로운 검색 시)
watch(
  () => route.query.q,
  (newQuery) => {
    if (typeof newQuery === 'string') {
      searchQuery.value = newQuery;
      fetchSearchResults(newQuery);
    }
  }
);
</script>

<template>
  <div class="search-results-view">
    <h1 class="search-title">
      '<span>{{ searchQuery }}</span>'에 대한 검색 결과
    </h1>

    <div v-if="isLoading" class="feedback-message">
      <p>검색 중입니다...</p>
    </div>
    
    <div v-else-if="posts.length > 0" class="post-list">
      <PostCard
        v-for="post in posts"
        :key="post.id"
        :post="post"
      />
    </div>

    <div v-else class="feedback-message">
      <p>검색 결과가 없습니다.</p>
    </div>
  </div>
</template>

<style scoped>
.search-results-view {
  max-width: 800px;
  margin: 2rem auto;
  padding: 0 1rem;
}

.search-title {
  font-size: 1.8rem;
  font-weight: 700;
  margin-bottom: 2rem;
  border-bottom: 2px solid #f0f0f0;
  padding-bottom: 1rem;
}

.search-title span {
  color: #ed664d;
}

.post-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.feedback-message {
  text-align: center;
  margin-top: 4rem;
  color: #888;
  font-size: 1.1rem;
}
</style>