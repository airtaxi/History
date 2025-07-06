<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import apiClient from '@/api';

const props = defineProps<{
  originalPost: any;
}>();

const originalPostMediaUrls = ref<Record<string, string>>({});
const originalPostAuthorProfileUrl = ref<string>('');

const getMediaBlobUrl = async (mediaId: string | null | undefined) => {
  if (!mediaId) return '';
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, {
      responseType: 'blob'
    });
    const contentType = response.headers['content-type'];
    if (!contentType.startsWith('image') && !contentType.startsWith('video')) return '';
    return URL.createObjectURL(response.data);
  } catch {
    return '';
  }
};

const loadOriginalPostMedia = async () => {
  if (!props.originalPost) return;

  if (props.originalPost.user?.profileThumbnailMediaId) {
    try {
      originalPostAuthorProfileUrl.value = await getMediaBlobUrl(props.originalPost.user.profileThumbnailMediaId);
    } catch (error) {
      console.warn('원본 작성자 프로필 이미지 로딩 실패');
      originalPostAuthorProfileUrl.value = '/src/assets/images/default_profile_image.jpg';
    }
  } else {
    originalPostAuthorProfileUrl.value = '/src/assets/images/default_profile_image.jpg';
  }

  for (const content of props.originalPost.contents) {
    if ((content as any).$type === 'media' && ((content as any).mediaId || (content as any).thumbnailMediaId)) {
      const id = (content as any).mediaId || (content as any).thumbnailMediaId;
      try {
        originalPostMediaUrls.value[id] = await getMediaBlobUrl(id);
      } catch (err) {
        //
      }
    }
  }
};

watch(() => props.originalPost, (newPost) => {
  if (newPost) {
    loadOriginalPostMedia();
  }
}, { immediate: true, deep: true });

onMounted(() => {
  if (props.originalPost) {
    loadOriginalPostMedia();
  }
});
</script>

<template>
  <div v-if="originalPost" class="original-post-preview">
    <div class="original-post-card">
      <div class="original-post-author">
        <img :src="originalPostAuthorProfileUrl || '/src/assets/images/default_profile_image.jpg'"
          class="original-author-avatar">
        <div class="original-author-info">
          <div class="original-author-name">{{ originalPost.user.nickname }}</div>
          <div class="original-post-timestamp">{{ new Date(originalPost.createdAt).toLocaleString() }}</div>
        </div>
      </div>

      <div class="original-post-content">
        <div v-for="(content, index) in originalPost.contents" :key="index">
          <p v-if="(content as any).$type === 'text'">{{ (content as any).text }}</p>

          <div v-else-if="(content as any).$type === 'media' && ((content as any).mediaId || (content as any).thumbnailMediaId)">
            <template v-if="originalPostMediaUrls[(content as any).mediaId || (content as any).thumbnailMediaId]">
              <video v-if="(content as any).mimeType && (content as any).mimeType.startsWith('video/')" controls
                class="original-post-media">
                <source :src="originalPostMediaUrls[(content as any).mediaId || (content as any).thumbnailMediaId]"
                  :type="(content as any).mimeType" />
                브라우저가 video 태그를 지원하지 않습니다.
              </video>
              <img v-else :src="originalPostMediaUrls[(content as any).mediaId || (content as any).thumbnailMediaId]"
                :alt="(content as any).description || '게시물 이미지'" class="original-post-media" />
            </template>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* CreatePost.vue에서 복사해온 스타일 */
.original-post-preview {
  margin: 16px 0;
}

.original-post-card {
  border: 1px solid #e9ecef;
  border-radius: 8px;
  padding: 16px;
  background-color: #f8f9fa;
  transition: all 0.2s;
}

.original-post-card:hover {
  border-color: #ced4da;
  background-color: #f1f3f4;
}

.original-post-author {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.original-author-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  object-fit: cover;
}

.original-author-info {
  flex: 1;
}

.original-author-name {
  font-weight: 600;
  font-size: 0.9rem;
  color: #212529;
}

.original-post-timestamp {
  font-size: 0.8rem;
  color: #6c757d;
  margin-top: 2px;
}

.original-post-content {
  color: #495057;
  line-height: 1.5;
}

.original-post-content p {
  margin: 0 0 8px 0;
}

.original-post-media {
  max-width: 100%;
  max-height: 200px;
  border-radius: 6px;
  object-fit: contain;
  margin-top: 8px;
}
</style>