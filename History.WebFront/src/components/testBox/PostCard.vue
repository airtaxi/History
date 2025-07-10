<script setup lang="ts">
import { defineProps, defineEmits, ref, onMounted, computed, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';
import { Swiper, SwiperSlide } from 'swiper/vue';
import { Navigation, Pagination } from 'swiper/modules';
import type { PostResponseDto } from '@/types';

// Composables
import { useReactions } from '@/components/testBox/src/composables/useReactions';
import { useMediaLoader } from '@/components/testBox/src/composables/useMediaLoader';
import { usePostActions } from '@/components/testBox/src/composables/usePostActions';

// Components
import PostHeader from '@/components/testBox/src/components/PostHeader.vue';
import PostContent from '@/components/testBox/src/components/PostContent.vue';
import PostFooter from '@/components/testBox/src/components/PostFooter.vue';

// Modals
import ImageModal from '@/components/testBox/src/components/modals/ImageModal.vue';
import UserListModal from '@/components/testBox/src/components/modals/UserListModal.vue';
import ReactionPopup from '@/components/testBox/src/components/modals/ReactionPopup.vue';
import ReportModal from '@/components/testBox/src/components/modals/ReportModal.vue';
import AccessDeniedModal from '@/components/testBox/src/components/modals/AccessDeniedModal.vue';

import defaultProfileImage from '@/assets/images/default_profile_image.jpg';

const props = defineProps<{
  post: PostResponseDto;
  profileImageMap?: Record<string, string>;
  showActions?: boolean;
}>();

const emit = defineEmits<(event: 'open-detail', ...args: any[]) => void>();

const modules = [Navigation, Pagination];
const repostSwiperRef = ref(null);
const router = useRouter();

const isRepost = computed(() => Boolean(props.post.isRepost));

// Composables 초기화
const { mediaUrlMap, profileBlobUrlMap, getMediaBlobUrl } = useMediaLoader();
const { reactionMap, myReaction, showReactionPopup, reactionPopupPosition, loadReactionData, selectReaction, startLongPress, endLongPress } = useReactions(props.post);
const { canEdit, openShareEditor, handleInstantRepost, deleteMyPost, submitReport, navigateToProfile, goToOriginalPost, openReportDialog, cancelReport, showReportModal, showAccessDeniedModal, deniedUserId, deniedUserNickname } = usePostActions(props.post, emit);

// ImageModal 관련 상태 및 함수
const showImageModal = ref(false);
const modalMediaSource = ref<any[]>([]);
const initialSlideIndex = ref(0);

const openImageModal = (mediaList: any[], index: number) => {
  modalMediaSource.value = mediaList.map(content => {
    let src = '';
    let type = 'image';
    let mimeType = content.mimeType || '';

    if (content.isExternal) {
      src = content.mediaId;
    } else {
      const mediaId = content.mediaId || content.thumbnailMediaId;
      src = mediaUrlMap.value[mediaId];
    }
    if (mimeType.startsWith('video/')) {
      type = 'video';
    }
    return { src, type, mimeType };
  });

  initialSlideIndex.value = index;
  showImageModal.value = true;
};

const closeImageModal = () => {
  showImageModal.value = false;
};

// UserListModal 관련 상태
const showSharedUsersModal = ref(false);
const showRepostedUsersModal = ref(false);

// 게시물 상세 페이지로 이동 요청
const requestOpenDetail = () => {
  emit('open-detail', props.post.id);
};

const goToUserProfile = (userId: string) => {
  router.push(`/user/${userId}`);
};

const isImageUrl = (url: string): boolean => {
  if (!url || typeof url !== 'string') return false;
  const trimmedUrl = url.trim();
  const imageExtensions = /\.(jpg|jpeg|png|gif|webp|bmp|svg|ico|avif|tiff|tif)(\?.*)?$/i;
  if (imageExtensions.test(trimmedUrl)) return true;
  const imageServices = [
    'dribbble.com', 'imgur.com', 'cloudinary.com', 'unsplash.com', 'pexels.com',
    'instagram.com', 'pinimg.com', 'googleusercontent.com', 'githubusercontent.com',
    'flickr.com', 'staticflickr.com', 'photobucket.com', 'imageshack.com',
    'tinypic.com', 'deviantart.net', 'twimg.com', 'discordapp.com', 'discord.com',
    'ibb.co', 'imgbb.com', 'i.imgur.com', 'prnt.sc', 'gyazo.com'
  ];
  const lowerUrl = trimmedUrl.toLowerCase();
  if (imageServices.some(service => lowerUrl.includes(service))) return true;
  const imageKeywords = ['/image/', '/img/', '/photo/', '/picture/', '/media/', '/upload/', '/file/original'];
  if (imageKeywords.some(keyword => lowerUrl.includes(keyword))) return true;
  if (!trimmedUrl.startsWith('http://') && !trimmedUrl.startsWith('https://')) return false;
  return false;
};

const addStopPropagationToSwiperNav = (swiperInstanceRef: any) => {
  if (!swiperInstanceRef) return;
  const swiperEl = swiperInstanceRef.value?.$el;
  if (!swiperEl) return;
  const nextBtn = swiperEl.querySelector('.swiper-button-next');
  const prevBtn = swiperEl.querySelector('.swiper-button-prev');
  if (nextBtn) {
    nextBtn.addEventListener('click', (e: Event) => e.stopPropagation());
  }
  if (prevBtn) {
    prevBtn.addEventListener('click', (e: Event) => e.stopPropagation());
  }
};

// 초기 데이터 로드
onMounted(async () => {
  // 게시물 미디어 로드
  for (const content of props.post.contents) {
    if (content.$type === 'media' && (content.mediaId || content.thumbnailMediaId)) {
      const id = content.mediaId || content.thumbnailMediaId;
      mediaUrlMap.value[id] = await getMediaBlobUrl(id);
    }
  }

  // 부모 게시물 미디어 로드 (리포스트인 경우)
  if ((props.post as any).parentPost) {
    const parentPost = (props.post as any).parentPost;
    if (parentPost.contents && Array.isArray(parentPost.contents)) {
      for (const content of parentPost.contents) {
        if ((content as any).$type === 'media' && ((content as any).mediaId || (content as any).thumbnailMediaId)) {
          const id = (content as any).mediaId || (content as any).thumbnailMediaId;
          mediaUrlMap.value[id] = await getMediaBlobUrl(id);
        }
      }
    }
  }

  // 프로필 이미지 로드
  const usersToFetch = new Map<string, string>();
  if (props.post.user && props.post.user.profileThumbnailMediaId) {
    usersToFetch.set(props.post.user.userId, props.post.user.profileThumbnailMediaId);
  }
  const parentPost = (props.post as any).parentPost;
  if (parentPost?.user?.profileThumbnailMediaId) {
    usersToFetch.set(parentPost.user.userId, parentPost.user.profileThumbnailMediaId);
  }

  await Promise.all(Array.from(usersToFetch.entries()).map(async ([userId, mediaId]) => {
    if (!profileBlobUrlMap.value[userId]) {
      const blobUrl = await getMediaBlobUrl(mediaId);
      profileBlobUrlMap.value[userId] = blobUrl;
    }
  }));

  // 반응 데이터 로드
  loadReactionData();

  await nextTick();
  addStopPropagationToSwiperNav(repostSwiperRef);
});
</script>

<template>
  <div v-if="isRepost" class="repost-wrapper">
    <div class="repost-label-standalone">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
        <path d="M23.77 15.67c-.292-.293-.767-.293-1.06 0l-2.22 2.22V7.65c0-2.068-1.683-3.75-3.75-3.75h-5.85c-.414 0-.75.336-.75.75s.336.75.75.75h5.85c1.24 0 2.25 1.01 2.25 2.25v10.24l-2.22-2.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.294-.292.294-.767.001-1.06zM3.5 16.44c.414 0 .75-.336.75-.75V5.44c0-1.24 1.01-2.25 2.25-2.25h5.85c.414 0 .75-.336.75-.75s-.336-.75-.75-.75H6.5c-2.068 0-3.75 1.682-3.75 3.75v10.24L.53 13.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.293-.292.293-.767 0-1.06s-.767-.293-1.06 0L3.5 15.44z"/>
      </svg>
      <span>{{ post.user.nickname }}님이 리포스트했습니다</span>
    </div>
    <template v-if="post.parentPost && post.parentPost.user">
      <div class="original-post-card" @click.stop="goToOriginalPost">
        <div class="original-post-author">
          <img :src="profileBlobUrlMap[post.parentPost.user.userId] || defaultProfileImage" class="original-author-avatar" @click.stop="goToUserProfile(post.parentPost.user.userId)" />
          <div class="original-author-info">
            <div class="original-author-name">{{ post.parentPost.user.nickname }}</div>
            <div class="original-post-timestamp">{{ new Date(post.parentPost.createdAt).toLocaleString() }}</div>
          </div>
        </div>
        <div class="original-post-content">
          <Swiper
            ref="repostSwiperRef" v-if="post.parentPost.contents?.some(c => c.$type === 'media')"
            class="media-swiper original-swiper"
            :spaceBetween="10"
            :slidesPerView="1"
            :loop="post.parentPost.contents?.filter(c => c.$type === 'media').length > 1"
            :navigation="true"
            :pagination="{ clickable: true }"
            :modules="modules">
            <SwiperSlide v-for="(content, index) in post.parentPost.contents?.filter(c => c.$type === 'media')" :key="index">
              <div v-if="mediaUrlMap[content.mediaId || content.thumbnailMediaId]">
                <video v-if="content.mimeType?.startsWith('video/')" controls class="original-post-media" @click.stop="openImageModal(post.parentPost.contents.filter(c => c.$type === 'media'), index)"><source :src="mediaUrlMap[content.mediaId || content.thumbnailMediaId]" :type="content.mimeType" /></video>
                <img v-else :src="mediaUrlMap[content.mediaId || content.thumbnailMediaId]" alt="게시물 이미지" class="original-post-media" @click.stop="openImageModal(post.parentPost.contents.filter(c => c.$type === 'media'), index)"/>
              </div>
            </SwiperSlide>
          </Swiper>
          <div v-for="(content, index) in post.parentPost.contents" :key="'parent-extra-' + index">
            <template v-if="content.$type !== 'media'">
              <template v-if="content.$type === 'text'">
                <template v-if="isImageUrl(content.text)">
                  <img :src="content.text.trim()" alt="이미지" class="original-post-media external-image" @click.stop="openImageModal([{ $type: 'media', mediaId: content.text, isExternal: true }], 0)" @error="(e) => (e.target as HTMLImageElement).style.display = 'none'" />
                </template>
                <p v-else style="white-space: pre-wrap;">{{ content.text }}</p>
              </template>
              <div v-else-if="content.$type === 'externalUrl'" class="external-link-container">
                <a :href="content.sourceUrl || content.SourceUrl || content.url || content.Url" target="_blank" rel="noopener noreferrer" class="external-link" @click.stop>
                  <div class="link-preview small" :class="{ 'has-image': !!content.thumbnailImageUrl || !!content.ThumbnailImageUrl || !!content.image || !!content.Image }">
                    <img v-if="content.thumbnailImageUrl || content.ThumbnailImageUrl || content.image || content.Image" :src="content.thumbnailImageUrl || content.ThumbnailImageUrl || content.image || content.Image" :alt="content.title || content.Title || '링크 미리보기'" class="link-preview-image" @error="(e) => (e.target as HTMLImageElement).style.display = 'none'" />
                    <div class="link-info">
                      <div v-if="content.title || content.Title" class="link-title">{{ content.title || content.Title }}</div>
                      <div v-if="content.description || content.Description" class="link-description">{{ content.description || content.Description }}</div>
                      <div class="link-url"><span class="link-icon">🔗</span><span class="link-text">{{ content.sourceUrl || content.SourceUrl || content.url || content.Url }}</span></div>
                    </div>
                  </div>
                </a>
              </div>
            </template>
          </div>
        </div>
      </div>
    </template>
  </div>

  <div v-else class="post-card" @click="requestOpenDetail">
    <PostHeader
      :user="{
        userId: post.user.userId,
        nickname: post.user.nickname
      }"
      :profile-image-url="profileBlobUrlMap[post.user.userId] || defaultProfileImage"
      :created-at="post.createdAt"
      :can-edit="canEdit"
      @delete="deleteMyPost"
      @report="openReportDialog"
    />

    <PostContent
      :contents="post.contents"
      :media-url-map="mediaUrlMap"
      @open-media-modal="openImageModal"
      @navigate-to-profile="navigateToProfile"
    />

    <PostFooter
      :post="post"
      :my-reaction="myReaction"
      :total-reactions="reactionMap.Like || 0"
      @open-detail="requestOpenDetail"
      @handle-reaction-click="selectReaction('Like')"
      @start-long-press="startLongPress"
      @end-long-press="endLongPress"
      @open-share-editor="openShareEditor"
      @handle-instant-repost="handleInstantRepost"
      @show-shared-users-modal="showSharedUsersModal = true"
      @show-reposted-users-modal="showRepostedUsersModal = true"
    />
  </div>

  <ImageModal
    :show="showImageModal"
    :media-source="modalMediaSource"
    :initial-slide-index="initialSlideIndex"
    @close="closeImageModal"
  />

  <UserListModal
    :show="showSharedUsersModal"
    :users="post.sharedAndRepostedUsers?.filter(u => !u.isRepost) || []"
    title="이 게시글을 공유한 사람"
    @close="showSharedUsersModal = false"
  />

  <UserListModal
    :show="showRepostedUsersModal"
    :users="post.sharedAndRepostedUsers?.filter(u => u.isRepost) || []"
    title="이 게시글을 리포스트한 사람"
    @close="showRepostedUsersModal = false"
  />

  <ReactionPopup
    :show="showReactionPopup"
    :position="reactionPopupPosition"
    @select-reaction="selectReaction"
    :my-reaction="myReaction"
  />

  <ReportModal
    :show="showReportModal"
    @close="cancelReport"
    @submit="submitReport"
  />

  <AccessDeniedModal
    :show="showAccessDeniedModal"
    :denied-user-nickname="deniedUserNickname"
    :denied-user-id="deniedUserId"
    @close="showAccessDeniedModal = false"
  />
</template>

<style scoped>
.post-card {
  background: #fff;
  border-radius: 8px;
  border: 1px solid #ddd;
  padding: 16px;
  cursor: pointer;
  transition: background-color .2s;
}
.media-swiper {
  width: 100%;
  height: auto;
}

.swiper-slide {
  display: flex;
  justify-content: center;
  align-items: center;
}
.post-content-area video,
.original-post-content video {
  display: block;
  margin: 12px auto;
  max-width: 100%;
  max-height: 400px;
  object-fit: contain;
  border-radius: 8px;
}

.external-link-container {
  display: flex;
  justify-content: center;
}

.external-link {
  text-decoration: none;
  color: inherit;
  display: block;
  max-width: 100%;
  width: 100%;
}

.link-preview {
  border: 1px solid #e1e5e9;
  border-radius: 12px;
  background: white;
  transition: all 0.2s ease;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.link-preview:hover {
  border-color: #d1d5db;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

.link-preview.has-image {
  display: flex;
}

.link-preview.has-image:not(.small) {
  flex-direction: column;
}
.link-preview.has-image:not(.small) .link-preview-image {
  width: 100%;
  height: 200px;
  object-fit: cover;
}
.link-preview.has-image:not(.small) .link-info {
  padding: 16px;
}

.link-preview.small.has-image {
  flex-direction: row;
  align-items: stretch;
}
.link-preview.small.has-image .link-preview-image {
  width: 100px;
  height: auto;
  object-fit: cover;
  flex-shrink: 0;
  border-right: 1px solid #e1e5e9;
}
.link-preview.small.has-image .link-info {
  padding: 12px;
}

.link-preview:not(.has-image) {
  padding: 16px;
  background: #f8f9fa;
}
.link-preview.small:not(.has-image) {
  padding: 12px;
}

.link-preview-image {
  background: #f5f5f5;
}

.link-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.link-title {
  font-weight: 600;
  color: #212529;
  font-size: 1rem;
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp:2;
  -webkit-box-orient: vertical;
}
.link-preview.small .link-title {
  font-size: 0.9rem;
  -webkit-line-clamp: 1;
  line-clamp:1;
}

.link-description {
  color: #495057;
  font-size: 0.875rem;
  line-height: 1.4;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  line-clamp:3;
  -webkit-box-orient: vertical;
  margin: 0;
}
.link-preview.small .link-description {
  font-size: 0.8rem;
  -webkit-line-clamp: 2;
  line-clamp:2;
}

.link-url {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6c757d;
  font-size: 0.85rem;
  margin-top: 8px;
}
.link-icon {
  flex-shrink: 0;
  font-size: 14px;
}
.link-text {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.repost-wrapper {
  background: white;
  border-radius: 8px;
  border: 1px solid #ddd;
  padding: 12px 16px;
  margin-bottom: 12px;
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
  width: 24px;
  height: 24px;
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

.original-post-content {
  color: #495057;
  line-height: 1.4;
  font-size: 0.9rem;
}

.original-post-content p {
  white-space: pre-wrap;
  margin: 0 0 6px 0;
}

.original-post-media {
  display: block;
  max-width: 100%;
  max-height: 400px;
  border-radius: 4px;
  object-fit: contain;
  margin: 12px auto;
}

.external-image {
  border: 1px solid #e9ecef;
  transition: all 0.2s ease;
}

.external-image:hover {
  border-color: #ed664d;
  transform: scale(1.02);
  box-shadow: 0 4px 12px rgba(237, 102, 77, 0.15);
}
</style>
<style>
.swiper-button-next, .swiper-button-prev {
  color: #ed664d !important;
  background-color: rgba(255, 255, 255, 0.7);
  border-radius: 50%;
  width: 32px !important;
  height: 32px !important;
  box-shadow: 0 2px 8px rgba(0,0,0,0.15);
  transition: all 0.2s ease;
}
.swiper-button-next:hover, .swiper-button-prev:hover {
  background-color: rgba(255, 255, 255, 0.9);
  transform: scale(1.1);
}
.swiper-button-next::after, .swiper-button-prev::after {
  font-size: 16px !important;
  font-weight: bold !important;
}
.swiper-pagination-bullet-active {
  background: #ed664d !important;
}
</style>
