<script lang="ts">
  export default {
    name: 'TimelineView'
  }
</script>
<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue';
import apiClient from '@/api';
import type { PostResponseDto } from '@/types';
import PostCard from '@/components/testBox/PostCard.vue';
import RightSidebar from '@/components/layout/RightSidebar.vue';
import CreatePost from '@/components/CreatePost.vue';
import PostDetailModal from '@/components/PostDetailModal.vue';


// --- 상태 관리 ---
const posts = ref<PostResponseDto[]>([]);
const isLoading = ref(true);
const isLoadingMore = ref(false);
const noMorePosts = ref(false);
const loadMoreSentinel = ref<HTMLElement | null>(null);
const profileImageMap = ref<Record<string, string>>({});
const isDetailModalOpen = ref(false);
const selectedPostId = ref('');
const openModalWithPost = (postId: string) => {
  selectedPostId.value = postId;
  isDetailModalOpen.value = true;
};

// 스크롤 위로 올리는 코드
const showScrollTopBtn = ref(false);

// 스크롤 위치 감지
const handleScroll = () => {
  showScrollTopBtn.value = window.scrollY > 200;
};

// 맨 위로 스크롤
const scrollToTop = () => {
  window.scrollTo({ top: 0, behavior: 'smooth' });
};

let observer: IntersectionObserver;

onMounted(() => {
  fetchTimeline();

  // 기존 IntersectionObserver 유지
  nextTick(() => {
    observer = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadMorePosts();
    }, {
      rootMargin: '200px',
    });
    if (loadMoreSentinel.value) observer.observe(loadMoreSentinel.value);
  });

  // 스크롤 이벤트 등록
  window.addEventListener('scroll', handleScroll);
});

onUnmounted(() => {
  observer?.disconnect();
  window.removeEventListener('scroll', handleScroll);
});

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
    noMorePosts.value = false; // 무한 스크롤을 위해 false로 초기화

    let hasMore = true;
    let fromId = null;

    // 10개의 게시글을 모을 때까지 반복
    while (hasMore && posts.value.length < 10) {
      const { data }: { data: PostResponseDto[] } = await apiClient.get('/api/Post/timeline', {
        params: fromId ? { from: fromId } : {}
      });

      const pagePosts: PostResponseDto[] = data.filter(
        (post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null)
      );

      if (data.length === 0) {
        hasMore = false;
        break; // ✅ 이제 유효한 while 루프 내 break
      }

      if (pagePosts.length > 0) {
        posts.value.push(...pagePosts);
        await prepareProfileImageMap(pagePosts);
      }

      fromId = data[data.length - 1]?.id;
    }


    // 정확히 10개만 남기기
    if (posts.value.length > 10) {
      posts.value = posts.value.slice(0, 10);
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
    let fromId = lastPost.id;
    let addedCount = 0;
    const targetAddCount = 5; // 한 번에 추가할 게시글 목표 개수

    // 목표 개수만큼 추가할 때까지 반복
    while (addedCount < targetAddCount && !noMorePosts.value) {
      const response = await apiClient.get<PostResponseDto[]>('/api/Post/timeline', {
        params: { from: fromId }
      });

      if (response.data.length === 0) {
        noMorePosts.value = true;
        break;
      }

      const newPosts: PostResponseDto[] = response.data.filter(
        (post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null)
      );

      if (newPosts.length > 0) {
        posts.value.push(...newPosts);
        await prepareProfileImageMap(newPosts);
        addedCount += newPosts.length;
      }

      // 다음 페이지를 위한 fromId 업데이트
      fromId = response.data[response.data.length - 1].id;

      // API 응답은 있지만 필터링 후 추가된 게시글이 없으면 계속 시도
      if (response.data.length > 0 && newPosts.length === 0) {
        continue;
      }
    }

    //console.log(`무한 스크롤: ${addedCount}개 게시글 추가됨`); // 디버깅용

  } catch (error) {
    console.error('추가 타임라인 로딩 실패:', error);
  } finally {
    isLoadingMore.value = false;
  }
};

// 새 글 작성 후 타임라인 새로고침
const handlePostCreated = async () => {
  try {
    const response: { data: PostResponseDto[] } = await apiClient.get<PostResponseDto[]>('/api/Post/timeline');

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
    <main class="main-content">
      <div class="feed-column">
        <CreatePost @post-created="handlePostCreated" />

        <div v-if="isLoading" class="loading-indicator">
          <div class="spinner"></div>
        </div>

        <div v-else class="post-list">
          <PostCard v-for="post in posts" :key="post.id" :post="post" :profile-image-map="profileImageMap" @open-detail="openModalWithPost" />
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
    <button
      v-if="showScrollTopBtn"
      class="scroll-top-button"
      @click="scrollToTop"
    >
      ⬆ 맨 위로
    </button>
    <PostDetailModal
      v-model="isDetailModalOpen"
      :post-id="selectedPostId"
    />
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
.scroll-top-button {
  position: fixed;
  bottom: 24px;
  left: 24px;
  background-color: #ed664d;
  color: white;
  padding: 10px 16px;
  border: none;
  border-radius: 24px;
  font-weight: bold;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(0,0,0,0.2);
  transition: background-color 0.3s ease;
  z-index: 1000;
}
.scroll-top-button:hover {
  background-color: #e85b3e;
}

/* 모바일 반응형 개선 */
@media (max-width: 768px) {
  .timeline-layout {
    background-color: white;
  }

  .main-content {
    padding: 0;
    margin-top: 0;
    gap: 0;
  }

  .feed-column {
    max-width: 100%;
    gap: 8px;
  }

  .post-list {
    gap: 8px;
  }

  /* 모바일에서 CreatePost 컴포넌트 최적화 */
  .feed-column :deep(.create-post-container) {
    border-radius: 0;
    border-left: none;
    border-right: none;
    margin-bottom: 8px;
  }

  /* 모바일에서 PostCard 최적화 */
  .feed-column :deep(.post-card) {
    border-radius: 0;
    border-left: none;
    border-right: none;
  }

  .loading-indicator {
    padding: 20px;
  }

  .spinner {
    width: 30px;
    height: 30px;
    border-width: 3px;
  }
}

/* 태블릿 반응형 */
@media (max-width: 1024px) and (min-width: 769px) {
  .main-content {
    gap: 20px;
    padding: 0 20px;
  }

  .feed-column {
    max-width: 580px;
  }
}

/* RightSidebar 숨기기 */
@media (max-width: 960px) {
  .main-content :deep(.sidebar-column) {
    display: none;
  }

  .main-content {
    justify-content: center;
  }
}

/* 대형 화면 */
@media (min-width: 1400px) {
  .main-content {
    max-width: 1200px;
    gap: 32px;
  }

  .feed-column {
    max-width: 680px;
  }
}

/* 터치 장치를 위한 최적화 */
@media (hover: none) and (pointer: coarse) {
  /* 터치 대상 크기 증가 */
  .feed-column :deep(button) {
    min-height: 44px;
    min-width: 44px;
  }

  /* 스크롤 성능 최적화 */
  .post-list {
    -webkit-overflow-scrolling: touch;
  }
}
</style>
