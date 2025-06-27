<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import apiClient from '@/api';
import type { PostResponseDto } from '@/types';
import TheHeader from '@/components/layout/TheHeader.vue';
import PostCard from '@/components/PostCard.vue';
import RightSidebar from '@/components/layout/RightSidebar.vue';
import CreatePost from '@/components/CreatePost.vue';

// --- 상태 관리 ---
const posts = ref<PostResponseDto[]>([]);
const isLoading = ref(true); 
const isLoadingMore = ref(false); 
const noMorePosts = ref(false); 
const loadMoreSentinel = ref<HTMLElement | null>(null); 
const profileImageMap = ref<Record<string, string>>({});

const getMediaBlobUrl = async (mediaId: string) => {
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, {
      responseType: 'blob'
    });
    const contentType = response.headers['content-type'];
    if (!contentType.startsWith('image')) return '';
    return URL.createObjectURL(response.data);
  } catch {
    return '';
  }
};

const prepareProfileImageMap = async (postList: PostResponseDto[]) => {
  const map: Record<string, string> = {};
  const userIds = new Set<string>();
  
  // 게시글 작성자들의 ID 수집
  postList.forEach(p => {
    userIds.add(p.user.userId);
    
    // 리포스트인 경우 원본 게시글 작성자도 추가
    if ((p as any).isRepost && (p as any).parentPost?.user) {
      userIds.add((p as any).parentPost.user.userId);
    }
  });

  // 각 사용자의 프로필 이미지 처리
  for (const uid of userIds) {
    // 이미 처리된 사용자는 건너뛰기
    if (profileImageMap.value[uid]) continue;
    
    // 일반 게시글에서 사용자 찾기
    let user = postList.find(p => p.user.userId === uid)?.user;
    
    // 리포스트 원본에서 사용자 찾기
    if (!user) {
      for (const post of postList) {
        if ((post as any).isRepost && (post as any).parentPost?.user?.userId === uid) {
          user = (post as any).parentPost.user;
          break;
        }
      }
    }
    
    if (user?.profileThumbnailMediaId) {
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId);
      map[uid] = blobUrl || '/src/assets/images/default_profile_image.jpg';
    }
  }

  profileImageMap.value = { ...profileImageMap.value, ...map };
};


const fetchTimeline = async () => {
  try {
    isLoading.value = true;
    posts.value = [];

    let hasMore = true;
    let fromId = null;

    while (hasMore) {
      const response = await apiClient.get<PostResponseDto[]>('/api/Post/timeline', {
        params: fromId ? { from: fromId } : {}
      });

      const pagePosts: PostResponseDto[] = response.data.filter(
        (post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null)
      );

      if (pagePosts.length === 0) {
        hasMore = false;
        break;
      }

      posts.value.push(...pagePosts);

      await prepareProfileImageMap(pagePosts);

      fromId = pagePosts[pagePosts.length - 1].id;

      if (posts.value.length >= 5) {
        hasMore = false;
      }
    }

    if (posts.value.length === 0) {
      noMorePosts.value = true;
    }

  } catch (error) {
    console.error('타임라인 로딩 실패:', error);
  } finally {
    isLoading.value = false;
  }
};


// --- 무한 스크롤을 위한 추가 데이터 로딩 함수 ---
const loadMorePosts = async () => {
  if (isLoadingMore.value || noMorePosts.value) return;

  const lastPost = posts.value[posts.value.length - 1];
  if (!lastPost) return;

  try {
    isLoadingMore.value = true;
    const response = await apiClient.get<PostResponseDto[]>('/api/Post/timeline', {
      params: { from: lastPost.id }
    });

    const newPosts: PostResponseDto[] = response.data.filter((post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null));

    if (newPosts.length > 0) {
      posts.value.push(...newPosts);
      
      await prepareProfileImageMap(newPosts);
    } else {
      noMorePosts.value = true;
    }
  } catch (error) {
    console.error('추가 타임라인 로딩 실패:', error);
  } finally {
    isLoadingMore.value = false;
  }
};


// --- IntersectionObserver를 사용한 스크롤 감지 ---
let observer: IntersectionObserver;

onMounted(() => {
  fetchTimeline(); // 최초 데이터 로드

  observer = new IntersectionObserver(
    (entries) => {
      // 감시 대상(sentinel)이 화면에 보이면 loadMorePosts 함수 호출
      if (entries[0].isIntersecting) {
        loadMorePosts();
      }
    },
    {
      rootMargin: '200px', // 화면에 보이기 200px 전에 미리 로드 시작
    }
  );

  if (loadMoreSentinel.value) {
    observer.observe(loadMoreSentinel.value);
  }
});

// 컴포넌트가 사라질 때 observer 정리
onUnmounted(() => {
  if (observer) {
    observer.disconnect();
  }
});

// 새 글 작성 후 타임라인 새로고침
const handlePostCreated = async () => {
  try {
    const response = await apiClient.get<PostResponseDto[]>('/api/Post/timeline');

    const newPosts: PostResponseDto[] = response.data.filter(
      (post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null)
    );

    for (let i = newPosts.length - 1; i >= 0; i--) {
      const post = newPosts[i];
      const isDuplicate = posts.value.some(p => p.id === post.id);
      if (!isDuplicate) {
        posts.value.unshift(post);
      }
    }

  } catch (error) {
    console.error('글 작성 후 타임라인 갱신 실패:', error);
  }
};
</script>

<template>
  <div class="timeline-layout">
    <TheHeader />
    <main class="main-content">
      <div class="feed-column">
        <CreatePost @post-created="handlePostCreated" />
        
        <div v-if="isLoading" class="loading-indicator">
          <div class="spinner"></div>
        </div>
        
        <div v-else class="post-list">
          <PostCard v-for="post in posts" :key="post.id" :post="post" :profile-image-map="profileImageMap" />
        </div>

        <div ref="loadMoreSentinel" class="sentinel"></div>

        <div v-if="isLoadingMore" class="loading-indicator">
          <div class="spinner"></div>
        </div>

        <div v-if="noMorePosts && !isLoading" class="end-of-feed">
          모든 글을 불러왔습니다.
        </div>
      </div>
      <RightSidebar />
    </main>
  </div>
</template>

<style scoped>
/* 이전과 동일한 스타일 */
.timeline-layout { background-color: #f8f9fa; min-height: 100vh; }
.main-content { display: flex; justify-content: center; gap: 24px; width: 100%; max-width: 1024px; margin: 24px auto; padding: 0 24px; }
.feed-column { flex: 1; max-width: 620px; display: flex; flex-direction: column; gap: 16px; }
.post-list { display: flex; flex-direction: column; gap: 16px; }
.loading-indicator { text-align: center; padding: 40px; }
.spinner { border: 4px solid #f3f3f3; border-top: 4px solid #ed664d; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto; }
@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
.end-of-feed { text-align: center; color: #888; padding: 20px; }
.sentinel { height: 1px; }

@media (max-width: 960px) {
  .sidebar-column { display: none; }
  .main-content { justify-content: center; }
}
@media (max-width: 768px) {
  .main-content { padding: 0 16px; margin-top: 16px; }
}
</style>