<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import apiClient from '@/api';
import type { PostResponseDto, CommentResponseDto, UserDto } from '@/types'; // UserDto 추가
import { useAuthStore } from '@/stores/auth';
import PostCard from '@/components/PostCard.vue';
import CommentItem from '@/components/CommentItem.vue';
import CreateComment from '@/components/CreateComment.vue';
// import "./PostDetailView.css" 전역 css 버그로 인한 일시 주석처리

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const postId = route.params.postId as string;

const post = ref<PostResponseDto | null>(null);
const comments = ref<CommentResponseDto[]>([]);
const isLoading = ref(true); // 페이지 전체의 초기 로딩 상태

// --- 페이지네이션을 위한 상태 변수 ---
const commentsNextFrom = ref(0); // 다음에 요청할 댓글의 시작 위치 (from)
const commentsLimit = 20;        // 한 번에 불러올 댓글 수 (limit)
const isCommentsLoading = ref(false); // '더보기' 버튼 클릭 시 로딩 상태
const hasMoreComments = ref(true);    // 더 불러올 댓글이 있는지 여부
// ------------------------------------

const profileImageMap = ref<Record<string, string>>({});
const createCommentRef = ref<InstanceType<typeof CreateComment> | null>(null);

// 프로필 이미지 blob URL 변환 함수
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

// 사용자 목록을 받아 프로필 이미지 맵에 추가하는 함수 (재사용성 개선)
const prepareProfileImageMapForUsers = async (users: (UserDto | undefined)[]) => {
  const userIds = new Set<string>();
  users.forEach(user => {
    if (user?.profileThumbnailMediaId) {
      userIds.add(user.userId);
    }
  });

  for (const userId of userIds) {
    // 이미 맵에 있거나 user 정보가 없으면 건너뜀
    if (profileImageMap.value[userId] || !users.find(u => u?.userId === userId)) continue;

    const user = users.find(u => u?.userId === userId)!;
    const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId!);
    profileImageMap.value[userId] = blobUrl || '/src/assets/images/default_profile_image.jpg';
  }
};

// 댓글 좋아요 처리
const handleLikeComment = async (commentId: string) => {
  try {
    // API 호출
    const response = await apiClient.post(`/api/Comment/${commentId}/like`);
    const updatedComment = response.data;

    // 화면의 댓글 데이터를 업데이트하여 '좋아요' 상태와 카운트를 즉시 반영
    const index = comments.value.findIndex(c => c.id === commentId);
    if (index !== -1) {
      comments.value[index] = updatedComment;
    }
  } catch (error) {
    console.error('좋아요 처리 실패:', error);
    alert('좋아요 처리에 실패했습니다.');
  }
};

// 댓글 수정 처리
const handleUpdateComment = async ({ commentId, newText }: { commentId: string, newText: string }) => {
  try {
    // 첫 번째 시도: PUT API로 수정 (CreateComment와 동일한 형식 사용)
    const formData = new FormData();
    const jsonData = JSON.stringify([{ $type: 'text', Text: newText }]); // 대문자 Text 사용
    formData.append('JsonData', jsonData);

    const response = await apiClient.put(`/api/Comment/${commentId}`, formData);
    const updatedComment = response.data;

    // 화면의 댓글 데이터를 업데이트
    const index = comments.value.findIndex(c => c.id === commentId);
    if (index !== -1) {
      comments.value[index] = updatedComment;
    }
  } catch (error) {
    console.warn('PUT API 수정 실패, 삭제 후 재등록 방식으로 시도:', error);
    
    // 두 번째 시도: 삭제 후 재등록 방식 (update가 없어서 이렇게 해놨습니다)
    try {
      // 1. 기존 댓글 삭제
      await apiClient.delete(`/api/Comment/${commentId}`);
      
      // 2. 새 댓글 등록
      const formData = new FormData();
      const jsonData = JSON.stringify([{ $type: 'text', Text: newText }]);
      formData.append('JsonData', jsonData);
      
      const createResponse = await apiClient.post(`/api/Comment/${postId}`, formData);
      const newComment = createResponse.data;
      
      // 3. 기존 댓글을 삭제하고 새 댓글로 교체
      const index = comments.value.findIndex(c => c.id === commentId);
      if (index !== -1) {
        comments.value[index] = newComment;
      }
      
      // 4. 새로 생성된 댓글 작성자의 프로필 이미지 준비
      await prepareProfileImageMapForUsers([newComment.user]);
      
    } catch (secondError) {
      console.error('삭제 후 재등록도 실패:', secondError);
      alert('댓글 수정에 실패했습니다.');
    }
  }
};


// 댓글을 추가로 불러오는 함수 ('더보기' 클릭 시 실행)
const fetchMoreComments = async () => {
  // 이미 로딩 중이거나 더 이상 댓글이 없으면 실행하지 않음
  if (isCommentsLoading.value || !hasMoreComments.value) return;

  isCommentsLoading.value = true;
  try {
    const response = await apiClient.get<CommentResponseDto[]>(
      `/api/Comment/${postId}?from=${commentsNextFrom.value}&limit=${commentsLimit}`
    );
    const newComments = response.data;

    if (newComments.length > 0) {
      comments.value.push(...newComments); // 기존 배열에 새 댓글을 추가
      commentsNextFrom.value += newComments.length; // 다음 시작 위치 업데이트

      // 새로 추가된 댓글 작성자들의 프로필 이미지를 준비
      await prepareProfileImageMapForUsers(newComments.map(c => c.user));
    }

    // 서버에서 받은 댓글 수가 요청한 limit보다 적으면, 더 이상 댓글이 없는 것으로 간주
    if (newComments.length < commentsLimit) {
      hasMoreComments.value = false;
    }

  } catch (error) {
    console.error("댓글 추가 로딩 실패:", error);
    hasMoreComments.value = false; // 에러 발생 시 더 이상 시도하지 않음
  } finally {
    isCommentsLoading.value = false;
  }
};

// 새 댓글 작성 후 목록을 새로고침하는 함수
const refreshComments = async () => {
  // 상태 변수들을 모두 초기화
  comments.value = [];
  commentsNextFrom.value = 0;
  hasMoreComments.value = true;
  // 댓글 목록을 처음부터 다시 불러옴
  await fetchMoreComments();
};

watch(() => route.params.postId, async (newPostId) => {
  if (!newPostId) return;
  // postId 업데이트
  const newId = newPostId as string;

  // 상태 초기화
  post.value = null;
  comments.value = [];
  commentsNextFrom.value = 0;
  hasMoreComments.value = true;
  isLoading.value = true;

  try {
    const postResponse = await apiClient.get<PostResponseDto>(`/api/Post/${newId}`);
    post.value = postResponse.data;

    // 프로필 이미지 준비
    const users = [post.value.user];
    if ((post.value as any).isRepost && (post.value as any).parentPost?.user) {
      users.push((post.value as any).parentPost.user);
    }
    await prepareProfileImageMapForUsers(users);

    // 댓글 다시 불러오기
    await fetchMoreComments();

  } catch (error) {
    console.error("라우트 변경 시 포스트 로딩 실패:", error);
  } finally {
    isLoading.value = false;
  }
});


// 페이지가 처음 로드될 때 실행
onMounted(async () => {
  isLoading.value = true;
  try {
    // 1. 게시물 정보 먼저 가져오기
    const postResponse = await apiClient.get<PostResponseDto>(`/api/Post/${postId}`);
    post.value = postResponse.data;

    // 게시물 작성자의 프로필 이미지 준비
    if (post.value) {
      const users = [post.value.user];
      // 리포스트인 경우 원본 게시글 작성자도 추가

      if ((post.value as any).isRepost && (post.value as any).parentPost?.user) {
        users.push((post.value as any).parentPost.user);
      }
      await prepareProfileImageMapForUsers(users);
    }

    // 2. 첫 페이지 댓글 불러오기
    await fetchMoreComments();

  } catch (error) {
    console.error("초기 데이터 로딩 실패:", error);
  } finally {
    isLoading.value = false;
  }
});

const handleMentionUser = (nickname: string) => {
  createCommentRef.value?.addMention(nickname);
};

const deleteMyPost = async () => {
  if (confirm('정말로 이 게시글을 삭제하시겠습니까?')) {
    try {
      await apiClient.delete(`/api/Post/${postId}`);
      alert('게시글이 삭제되었습니다.');
      router.push('/'); // 타임라인으로 이동
    } catch (error) {
      alert('게시글 삭제에 실패했습니다.');
    }
  }
};

const deleteMyComment = async (commentId: string) => {
  try {
    await apiClient.delete(`/api/Comment/${commentId}`);
    // 화면에서 즉시 댓글 제거
    comments.value = comments.value.filter(c => c.id !== commentId);
  } catch (error) {
    alert('댓글 삭제에 실패했습니다.');
  }
};
</script>

<template>
  <div class="detail-view-layout">
    <main v-if="isLoading" class="loading-indicator">
      <div class="spinner"></div>
    </main>
    <main v-else-if="post" class="main-content">
      <div class="post-section">
        <PostCard :post="post" :show-actions="true" :profile-image-map="profileImageMap" />
      </div>
      <div class="comments-section">
        <h3>댓글 ({{ comments.length }})</h3>
        <CommentItem 
          v-for="comment in comments" 
          :key="comment.id" 
          :comment="comment"
          :profile-image-url="profileImageMap[comment.user.userId] || '/src/assets/images/default_profile_image.jpg'"
          @mention-user="handleMentionUser"
          @delete-comment="deleteMyComment"
          @like-comment="handleLikeComment"     @update-comment="handleUpdateComment" />

        <div v-if="hasMoreComments" class="load-more-container">
          <button @click="fetchMoreComments" :disabled="isCommentsLoading" class="load-more-btn">
            {{ isCommentsLoading ? '로딩 중...' : '댓글 더보기' }}
          </button>
        </div>

        <CreateComment :post-id="postId" @comment-created="refreshComments" ref="createCommentRef" />
      </div>
    </main>
  </div>
</template>

<style scoped>
.main-content {
  max-width: 800px;
  margin: 40px auto;
  padding: 0 24px;
}
.post-section { 
  margin-bottom: 24px; 
}
.comments-section {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 12px 16px;
}
.comments-section h3 {
  margin-bottom: 16px;
  font-size: 1.1rem;
  font-weight: 600;
}
.loading-indicator { 
  text-align: center; 
  padding: 40px; 
}

.spinner { /* 스피너 스타일 */ }

.load-more-container {
  text-align: center;
  padding: 16px;
  border-top: 1px solid #eee;
}
.load-more-btn {
  background-color: #f0f0f0;
  border: 1px solid #ddd;
  color: #333;
  padding: 10px 20px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
  width: 100%;
}
.load-more-btn:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.post-section :deep(.post-card) {
  max-width: none;
  margin: 0;
  border: none;
  box-shadow: none;
  border-radius: 0;
  padding: 0;
  background: transparent;
}

.post-section :deep(.post-card .content-text) {
  white-space: pre-wrap; /* 줄바꿈 반영 */
  font-size: 1rem;
  line-height: 1.6;
}

.post-section :deep(.original-post-content p) {
  white-space: pre-wrap;
  line-height: 1.6;
  margin-bottom: 8px;
}

.post-section :deep(.original-post-card) {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 12px 16px;
  background: #fafafa;
  margin-top: 8px;
}

</style>