<!--
 * OriginalPostCard.vue
 *
 * 이 컴포넌트는 리포스트된 원본 게시물 또는 인용된 원본 게시물의 UI를 렌더링합니다.
 * isEmbedded prop에 따라 리포스트 헤더 표시 여부를 제어합니다.
 *
 * @props {
 *   post: PostResponseDto - 리포스트 또는 인용 게시물 데이터. parentPost를 포함합니다.
 *   profileBlobUrlMap: Record<string, string> - 프로필 이미지 Blob URL 맵.
 *   mediaUrlMap: Record<string, string> - 미디어 콘텐츠 Blob URL 맵.
 *   isEmbedded: boolean - 인용 게시물 형태로 렌더링할지 여부. true이면 리포스트 헤더를 숨깁니다.
 * }
 * @emits {
 *   navigate-to-original: 사용자가 원본 게시물 영역을 클릭했을 때 발생.
 * }
-->
<template>
  <div :class="{ 'repost-wrapper': !isEmbedded, 'embedded-post': isEmbedded }">
    <div v-if="!isEmbedded" class="repost-label-standalone">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
        <path d="M23.77 15.67c-.292-.293-.767-.293-1.06 0l-2.22 2.22V7.65c0-2.068-1.683-3.75-3.75-3.75h-5.85c-.414 0-.75.336-.75.75s.336.75.75.75h5.85c1.24 0 2.25 1.01 2.25 2.25v10.24l-2.22-2.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.294-.292.294-.767.001-1.06zM3.5 16.44c.414 0 .75-.336.75-.75V5.44c0-1.24 1.01-2.25 2.25-2.25h5.85c.414 0 .75-.336.75-.75s-.336-.75-.75-.75H6.5c-2.068 0-3.75 1.682-3.75 3.75v10.24L.53 13.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.293-.292.293-.767 0-1.06s-.767-.293-1.06 0L3.5 15.44z"/>
      </svg>
      <span>{{ post.user.nickname }}님이 리포스트했습니다</span>
    </div>
    <template v-if="post.parentPost && post.parentPost.user">
      <div class="original-post-card" @click.stop="$emit('navigate-to-original')">
        <div class="original-post-author">
          <img :src="parentProfileImageUrl" class="original-author-avatar" @click.stop="goToUserProfile(post.parentPost.user.userId)" />
          <div class="original-author-info">
            <div class="original-author-name">{{ post.parentPost.user.nickname }}</div>
            <div class="original-post-timestamp">{{ formatRelativeTime(post.parentPost.createdAt) }}</div>
          </div>
        </div>
        <PostContent
          :contents="post.parentPost.contents"
          :media-url-map="mediaUrlMap"
          @open-media-modal="openImageModal"
          @navigate-to-profile="goToUserProfile"
        />
      </div>
    </template>
  </div>

  <ImageModal
    :show="showImageModal"
    :media-source="modalMediaSource"
    :initial-slide-index="initialSlideIndex"
    @close="closeImageModal"
  />
</template>

<script setup lang="ts">
import { defineProps, defineEmits, ref, onMounted, nextTick, watch } from 'vue';
import { useRouter } from 'vue-router';

import { useImageModal } from '@/components/src/composables/useImageModal';
import { formatRelativeTime } from '@/components/src/utils/timeUtils';
import defaultProfileImage from '@/assets/images/default_profile_image.jpg';
import ImageModal from '@/components/src/components/modals/ImageModal.vue';
import PostContent from './PostContent.vue';
import type { PostResponseDto } from '@/types';

const props = defineProps<{
  post: PostResponseDto;
  profileBlobUrlMap: Record<string, string>;
  mediaUrlMap: Record<string, string>;
  isEmbedded?: boolean;
}>();

defineEmits<(event: 'navigate-to-original') => void>();

const router = useRouter();
const { showImageModal, modalMediaSource, initialSlideIndex, openImageModal, closeImageModal } = useImageModal(props.mediaUrlMap);

const goToUserProfile = (userId: string) => {
  router.push(`/user/${userId}`);
};

const parentProfileImageUrl = ref(defaultProfileImage);

watch([() => props.profileBlobUrlMap, () => props.post.parentPost?.user?.userId], ([newMap, newUserId]) => {
  if (newMap && newUserId && newMap[newUserId]) {
    parentProfileImageUrl.value = newMap[newUserId];
  } else {
    parentProfileImageUrl.value = defaultProfileImage;
  }
}, { immediate: true });


</script>

<style scoped>
.repost-wrapper {
  background: white;
  border-radius: 8px;
  border: 1px solid #ddd;
  padding: 12px 16px;
}

.embedded-post {
  margin-top: 12px;
}

.repost-label-standalone {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6c757d;
  font-size: 0.85rem;
  font-weight: 500;
  margin-bottom: 8px;
}

.repost-label-standalone svg {
  color: #6c757d;
}

.original-post-card {
  border: 1px solid #e9ecef;
  border-radius: 8px;
  padding: 16px;
  margin-top: 8px;
  background-color: #fff;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 1rem;
  line-height: 1.6;
}

.original-post-card:hover {
  border-color: #ced4da;
  background-color: #f1f3f4;
}

.original-post-author {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
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
  font-size: 0.85rem;
  color: #212529;
}

.original-post-timestamp {
  font-size: 0.75rem;
  color: #6c757d;
  margin-top: 1px;
}

</style>
