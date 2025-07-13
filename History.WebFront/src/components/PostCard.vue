<script setup lang="ts">
import { defineProps, defineEmits, ref, computed } from 'vue';
import type { PostResponseDto } from '@/types';

// Composables
import { useReactions } from '@/components/src/composables/useReactions';
import { useMediaLoader } from '@/components/src/composables/useMediaLoader';
import { usePostActions } from '@/components/src/composables/usePostActions';
import { useImageModal } from '@/components/src/composables/useImageModal';
import { useIntersectionObserver } from '@/components/src/composables/useIntersectionObserver';
import { useComments } from '@/components/src/composables/useComments';

// Components
import PostHeader from '@/components/src/components/PostHeader.vue';
import PostContent from '@/components/src/components/PostContent.vue';
import PostFooter from '@/components/src/components/PostFooter.vue';
import OriginalPostCard from '@/components/src/components/OriginalPostCard.vue';
import CommentItem from '@/components/CommentItem.vue';
import CreateComment from '@/components/CreateComment.vue';

// Modals
import ImageModal from '@/components/src/components/modals/ImageModal.vue';
import UserListModal from '@/components/src/components/modals/UserListModal.vue';
import ReactionPopup from '@/components/src/components/modals/ReactionPopup.vue';
import ReportModal from '@/components/src/components/modals/ReportModal.vue';
import AccessDeniedModal from '@/components/src/components/modals/AccessDeniedModal.vue';

import defaultProfileImage from '@/components/src/assets/images/default_profile_image.jpg';

const props = defineProps<{
  post: PostResponseDto;
  profileImageMap?: Record<string, string>; // Optional prop to accept the map
}>();

const emit = defineEmits<{
  (event: 'open-detail', ...args: any[]): void;
}>();

const isDeleted = ref(false); // 게시글 삭제 여부 상태

const postCardElement = ref(null);
const isDataLoaded = ref(false);
const isCommentsVisible = ref(false);
const createCommentRef = ref<InstanceType<typeof CreateComment> | null>(null);

const isRepost = computed(() => props.post.isRepost);
const isQuotePost = computed(() => !props.post.isRepost && props.post.parentPost);

// Composables 초기화
const { mediaUrlMap, profileBlobUrlMap, getMediaBlobUrl } = useMediaLoader();
const { reactionMap, myReaction, showReactionPopup, reactionPopupPosition, loadReactionData, selectReaction, startLongPress, endLongPress, handleReactionClick } = useReactions(props.post);
const { canEdit, openShareEditor, handleInstantRepost, deleteMyPost, submitReport, navigateToProfile, goToOriginalPost, openReportDialog, cancelReport, showReportModal, showAccessDeniedModal, deniedUserId, deniedUserNickname } = usePostActions(props.post, emit);

const { showImageModal, modalMediaSource, initialSlideIndex, openImageModal, closeImageModal } = useImageModal(mediaUrlMap.value);
const {
  displayedComments,
  profileImageMap: commentProfileImageMap,
  isLoading: isCommentsLoading,
  hasMoreComments,
  sortOrder,
  fetchInitialData,
  refreshData,
  loadMoreComments,
  handleLikeComment,
  deleteMyComment,
  handleUpdateComment
} = useComments(props.post);

// UserListModal 관련 상태
const showSharedUsersModal = ref(false);
const showRepostedUsersModal = ref(false);

const handleShowSharedUsersModal = () => {
  showSharedUsersModal.value = true;
};

const handleCommentIconClick = () => {
  isCommentsVisible.value = !isCommentsVisible.value;
  if (isCommentsVisible.value && displayedComments.value.length === 0) {
    fetchInitialData();
  }
};

const handleMentionUser = (nickname: string) => {
  createCommentRef.value?.addMention(nickname);
};

const handleDeletePost = async () => {
  if (await deleteMyPost()) {
    isDeleted.value = true;
  }
};

// 데이터 로드 함수
const loadPostData = async () => {
  if (isDataLoaded.value) return;
  isDataLoaded.value = true;

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
};

// Intersection Observer 설정
useIntersectionObserver(postCardElement, loadPostData);

</script>

<template>
  <!-- 순수 리포스트 -->
  <OriginalPostCard
    v-if="isRepost && !isDeleted"
    ref="postCardElement"
    :post="post"
    :profileBlobUrlMap="profileBlobUrlMap"
    :mediaUrlMap="mediaUrlMap"
    @navigate-to-original="goToOriginalPost"
  />

  <!-- 일반 게시물 또는 인용(공유) 게시물 -->
  <div v-else-if="!isDeleted" class="post-card" ref="postCardElement">
    <div class="post-main-content">
      <PostHeader
        :user="{
          userId: post.user.userId,
          nickname: post.user.nickname
        }"
        :profile-image-url="profileBlobUrlMap[post.user.userId] || defaultProfileImage"
        :created-at="post.createdAt"
        :can-edit="canEdit"
        @delete="handleDeletePost"
        @report="openReportDialog"
      />

      <PostContent
        :contents="post.contents"
        :media-url-map="mediaUrlMap"
        @open-media-modal="openImageModal"
        @navigate-to-profile="navigateToProfile"
      />

      <OriginalPostCard
        v-if="isQuotePost"
        :post="post"
        :profileBlobUrlMap="profileBlobUrlMap"
        :mediaUrlMap="mediaUrlMap"
        :is-embedded="true"
        @navigate-to-original="goToOriginalPost"
      />
    </div>

    <div class="post-footer-wrapper">
      <PostFooter
        :post="post"
        :my-reaction="myReaction"
        :total-reactions="reactionMap.Like || 0"
        @open-comment-input="handleCommentIconClick"
        @handle-reaction-click="handleReactionClick"
        @start-long-press="startLongPress($event)"
        @end-long-press="endLongPress"
        @open-share-editor="openShareEditor"
        @handle-instant-repost="handleInstantRepost"
        @show-shared-users-modal="handleShowSharedUsersModal"
        @show-reposted-users-modal="showRepostedUsersModal = true"
      />
    </div>

    <!-- 댓글 섹션 -->
    <div v-if="isCommentsVisible" class="comments-container" @click.stop>
      <div class="comment-controls">
        <div class="sort-group">
          <button @click="sortOrder = 'newest'" :class="{ active: sortOrder === 'newest' }">최신순</button>
          <button @click="sortOrder = 'oldest'" :class="{ active: sortOrder === 'oldest' }">오래된순</button>
        </div>
      </div>

      <div v-if="isCommentsLoading" class="loading-indicator">댓글 로딩 중...</div>

      <div style="min-height: 1rem" v-else>
        <CommentItem
          v-for="comment in displayedComments"
          :key="comment.id"
          :comment="comment"
          :profile-image-url="commentProfileImageMap[comment.user.userId] || defaultProfileImage"
          @mention-user="handleMentionUser"
          @delete-comment="deleteMyComment"
          @like-comment="handleLikeComment"
          @update-comment="handleUpdateComment" />

        <div v-if="hasMoreComments" class="load-more-container">
          <button @click="loadMoreComments">댓글 더보기</button>
        </div>
      </div>

      <CreateComment :post-id="post.id" @comment-created="refreshData" ref="createCommentRef" />
    </div>
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
  transition: background-color .2s;
  min-height: 150px;
}

.post-main-content {
  padding: 16px;
}

.post-footer-wrapper {
  padding: 0 16px;
}

.comments-container {
  border-top: 1px solid #eee;
}

.comment-controls {
  display: flex;
  justify-content: flex-end;
  padding: 16px 16px 0;
}

.sort-group button {
  margin-left: 8px;
  background: none;
  border: 1px solid #ccc;
  padding: 4px 8px;
  border-radius: 4px;
  cursor: pointer;
}

.sort-group button.active {
  background-color: #ed664d;
  color: white;
  border-color: #ed664d;
}

.loading-indicator {
  text-align: center;
  padding: 20px;
  color: #888;
}

.load-more-container {
  text-align: center;
  margin-top: 16px;
  padding: 0 16px 16px;
}

.load-more-container button {
  background-color: #f0f0f0;
  border: 1px solid #ddd;
  color: #333;
  padding: 10px 20px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
  width: 100%;
}
</style>
