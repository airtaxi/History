<script setup lang="ts">
import { defineProps, defineEmits, ref, onMounted } from 'vue';
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

const props = defineProps<{
  post: PostResponseDto;
  profileImageMap?: Record<string, string>;
  showActions?: boolean;
}>();

const emit = defineEmits(['open-detail']);

// Composables 초기화
const { mediaUrlMap, profileBlobUrlMap, getMediaBlobUrl } = useMediaLoader();
const { reactionMap, myReaction, reactionUsersMap, showReactionPopup, reactionPopupPosition, hoveredReaction, tooltipPosition, loadReactionData, postReaction, handleReactionClick, selectReaction, startLongPress, endLongPress, createFloatingEmoji } = useReactions(props.post);
const { canEdit, openShareEditor, handleInstantRepost, deleteMyPost, submitReport, navigateToProfile, goToOriginalPost, goToPostDetail, openReportDialog, cancelReport, showReportModal, selectedReason, showAccessDeniedModal, deniedUserId, deniedUserNickname } = usePostActions(props.post, emit);

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

// 초기 데이터 로드
onMounted(() => {
  loadReactionData();
});

</script>

<template>
  <OriginalPostCard
    v-if="post.isRepost"
    :post="post"
    :profileImageMap="profileBlobUrlMap"
    @navigate-to-original="goToOriginalPost"
  />

  <div v-else class="post-card" @click="requestOpenDetail">
    <PostHeader
      :user="post.user"
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
      @handle-reaction-click="handleReactionClick"
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
</style>
