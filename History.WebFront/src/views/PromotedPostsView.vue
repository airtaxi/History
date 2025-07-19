<script setup lang="ts">
import { ref, onMounted } from 'vue';
import apiClient from '@/api';
import type { PostResponseDto } from '@/types';
import PromotedPostItem from '@/components/PromotedPostItem.vue'; 
import RightSidebar from '@/components/layout/RightSidebar.vue';

const posts = ref<PostResponseDto[]>([]);
const isLoading = ref(true);

const mediaUrlMap = ref<Record<string, string>>({});
const profileImageMap = ref<Record<string, string>>({});

const getMediaBlobUrl = async (mediaId: string): Promise<string> => {
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, { responseType: 'blob' });
    return URL.createObjectURL(response.data);
  } catch {
    return '';
  }
};

async function fetchPromotedPosts() {
  isLoading.value = true;
  try {
    const response = await apiClient.get('/api/post/public-post', {
      params: { limit: 20 },
    });
    
    const newPosts: PostResponseDto[] = response.data;
    posts.value = newPosts;

    // [추가] 받아온 게시물들의 미디어 URL을 모두 준비
    for (const post of newPosts) {
      // 프로필 이미지 준비
      if (post.user.profileThumbnailMediaId && !profileImageMap.value[post.user.userId]) {
        profileImageMap.value[post.user.userId] = await getMediaBlobUrl(post.user.profileThumbnailMediaId);
      }
      
      // 게시물 콘텐츠 미디어 준비
      for (const content of post.contents) {
        if (content.$type === 'media' && (content.mediaId || content.thumbnailMediaId)) {
          const id = content.mediaId || content.thumbnailMediaId;
          if (!mediaUrlMap.value[id]) {
            mediaUrlMap.value[id] = await getMediaBlobUrl(id);
          }
        }
      }
    }

  } catch (error) {
    console.error('홍보된 글을 불러오는 데 실패했습니다:', error);
    alert('데이터를 불러오는 중 오류가 발생했습니다.');
  } finally {
    isLoading.value = false;
  }
}

onMounted(() => {
  fetchPromotedPosts();
});
</script>

<template>
  <div class="promoted-layout">
    <main class="main-content">
      <div class="feed-column">
        <h1 class="view-title">발견</h1>
        
        <div v-if="isLoading" class="loading-indicator">
          데이터를 불러오는 중...
        </div>

        <div v-else-if="posts.length === 0" class="empty-message">
          추천 게시글이 없습니다.
        </div>

        <div v-else class="post-list">
          <PromotedPostItem 
            v-for="post in posts" 
            :key="post.id" 
            :post="post"
            :profile-image-map="profileImageMap"
            :media-url-map="mediaUrlMap"
          />
        </div>
      </div>

      <RightSidebar />
    </main>
  </div>
</template>

<style scoped>
.view-title {
  font-size: 2rem;
  font-weight: 700;
  margin-bottom: 24px;
}
.loading-indicator, .empty-message {
  text-align: center;
  padding: 48px;
  color: #888;
}
.post-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.promoted-layout {
  background-color: #f8f9fa;
  min-height: 100vh;
}
.main-content {
  display: flex;
  justify-content: center;
  gap: 24px;
  width: 100%;
  max-width: 1024px;
  margin: 24px auto;
  padding: 0 24px;
}
.feed-column {
  flex: 1;
  max-width: 620px;
}

@media (max-width: 960px) {
  .main-content :deep(.sidebar-column) {
    display: none;
  }
  .main-content {
    justify-content: center;
  }
}

</style>