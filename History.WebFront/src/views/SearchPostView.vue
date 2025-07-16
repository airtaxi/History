<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import apiClient from '@/api';
import type { PostResponseDto } from '@/types';
import PostCard from '@/components/PostCard.vue';

const route = useRoute();
const posts = ref<PostResponseDto[]>([]);
const isLoading = ref(false);
const searchQuery = ref(route.query.keyword || '');

// --- 무한 스크롤을 위한 상태 변수 ---
const isLoadingMore = ref(false);
const noMoreResults = ref(false);
const loadMoreSentinel = ref<HTMLElement | null>(null);
let observer: IntersectionObserver;
const limit = 20; // 한 번에 불러올 개수

/**
 * 새로운 검색어에 대한 첫 페이지 결과를 가져오는 함수
 */
async function fetchSearchResults(query: string) {
  if (!query) {
    posts.value = [];
    return;
  }
  
  isLoading.value = true;
  posts.value = []; // 결과 초기화
  noMoreResults.value = false; // '더 보기' 상태 초기화

  try {
    const response = await apiClient.get<PostResponseDto[]>('/api/Post/search', {
      params: { keyword: query, limit }
    });
    posts.value = response.data;

    // 받아온 결과가 limit보다 적으면 더 이상 데이터가 없는 것
    if (response.data.length < limit) {
      noMoreResults.value = true;
    }
  } catch (error) {
    console.error('게시글 검색 실패:', error);
    posts.value = [];
  } finally {
    isLoading.value = false;
  }
};

/**
 * 스크롤을 맨 아래로 내렸을 때 다음 페이지 결과를 가져오는 함수
 */
async function loadMoreResults() {
  if (isLoadingMore.value || noMoreResults.value || posts.value.length === 0) return;

  isLoadingMore.value = true;
  const lastPostId = posts.value[posts.value.length - 1].id;

  try {
    const response = await apiClient.get<PostResponseDto[]>('/api/Post/search', {
      params: { 
        keyword: searchQuery.value, 
        limit, 
        from: lastPostId // 마지막 게시물 ID를 from으로 전달
      }
    });

    posts.value.push(...response.data);

    if (response.data.length < limit) {
      noMoreResults.value = true;
    }
  } catch (error) {
    console.error('검색 결과 추가 로딩 실패:', error);
  } finally {
    isLoadingMore.value = false;
  }
};

onMounted(() => {
  fetchSearchResults(searchQuery.value as string);

  observer = new IntersectionObserver(
    (entries) => {
      if (entries[0].isIntersecting) {
        loadMoreResults();
      }
    },
    { rootMargin: '200px' }
  );

  if (loadMoreSentinel.value) {
    observer.observe(loadMoreSentinel.value);
  }
});

onUnmounted(() => {
  if (observer) {
    observer.disconnect();
  }
});

watch(
  () => route.query.keyword,
  (newKeyword) => {
    if (typeof newKeyword === 'string' && newKeyword !== searchQuery.value) {
      searchQuery.value = newKeyword;
      fetchSearchResults(newKeyword);
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

    <div ref="loadMoreSentinel" class="sentinel"></div>

    <div v-if="isLoadingMore" class="feedback-message">
      <p>결과 더 불러오는 중...</p>
    </div>

    <div v-if="noMoreResults && posts.length > 0" class="feedback-message">
      <p>모든 검색 결과를 불러왔습니다.</p>
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

.sentinel {
  height: 50px;
}
</style>