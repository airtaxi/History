<script setup lang="ts">
import { defineProps, defineEmits, ref, onMounted, computed } from 'vue';
import type { PostResponseDto } from '@/types';

// Composables
import { useReactions } from '@/components/testBox/src/composables/useReactions';
import { useMediaLoader } from '@/components/testBox/src/composables/useMediaLoader';
import { usePostActions } from '@/components/testBox/src/composables/usePostActions';

// Components
import PostHeader from '@/components/testBox/src/components/PostHeader.vue';
import PostContent from '@/components/testBox/src/components/PostContent.vue';
import PostFooter from '@/components/testBox/src/components/PostFooter.vue';
import OriginalPostCard from '@/components/testBox/src/components/OriginalPostCard.vue';

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

const isRepost = computed(() => props.post.isRepost);
const isQuotePost = computed(() => !props.post.isRepost && props.post.parentPost);

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

const handleShowSharedUsersModal = () => {
  showSharedUsersModal.value = true;
};

// 게시물 상세 페이지로 이동 요청
const requestOpenDetail = () => {
  // 인용 게시물 자체를 클릭했을 때는 상세 페이지로 이동하지 않음 (내부 원본 게시물 클릭으로만 이동)
  if (isQuotePost.value) return;
  emit('open-detail', props.post.id);
};

// 초기 데이터 로드
onMounted(async () => {
  const allContents = [...props.post.contents];
  if (props.post.parentPost) {
    allContents.push(...props.post.parentPost.contents);
  }

  for (const content of allContents) {
    if (content?.$type === 'media' && (content.mediaId || content.thumbnailMediaId)) {
      const id = content.mediaId || content.thumbnailMediaId;
      if (!mediaUrlMap.value[id]) {
        mediaUrlMap.value[id] = await getMediaBlobUrl(id);
      }
    }
  }

  const usersToFetch = new Map<string, string>();
  if (props.post.user && props.post.user.profileThumbnailMediaId) {
    usersToFetch.set(props.post.user.userId, props.post.user.profileThumbnailMediaId);
  }
  const parentPost = props.post.parentPost;
  if (parentPost?.user?.profileThumbnailMediaId) {
    usersToFetch.set(parentPost.user.userId, parentPost.user.profileThumbnailMediaId);
  }

  await Promise.all(Array.from(usersToFetch.entries()).map(async ([userId, mediaId]) => {
    if (!profileBlobUrlMap.value[userId]) {
      const blobUrl = await getMediaBlobUrl(mediaId);
      profileBlobUrlMap.value[userId] = blobUrl;
    }
  }));

  loadReactionData();
});

</script>

<template>
  <!-- 순수 리포스트 -->
  <OriginalPostCard
    v-if="isRepost"
    :post="post"
    :profileBlobUrlMap="profileBlobUrlMap"
    :mediaUrlMap="mediaUrlMap"
    @navigate-to-original="goToOriginalPost"
  />

  <!-- 일반 게시물 또는 인용(공유) 게시물 -->
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

    <!-- 인용 게시물일 경우, 원본 게시물을 렌더링 -->
    <OriginalPostCard
      v-if="isQuotePost"
      :post="post" 
      :profileBlobUrlMap="profileBlobUrlMap"
      :mediaUrlMap="mediaUrlMap"
      :is-embedded="true"
      @navigate-to-original="goToOriginalPost"
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
      @show-shared-users-modal="handleShowSharedUsersModal"
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
</style>
