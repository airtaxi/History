<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue';
import apiClient from '@/api';
import type { PostResponseDto, CommentResponseDto, UserDto } from '@/types';
import { useAuthStore } from '@/stores/auth';
import PostCard from '@/components/PostCard.vue';
import CommentItem from '@/components/CommentItem.vue';
import CreateComment from '@/components/CreateComment.vue';


const props = defineProps({
  postId: {
    type: String,
    required: true,
  },
  modelValue: { // v-model 바인딩에 사용됩니다.
    type: Boolean,
    required: true,
  }
});

const emit = defineEmits(['update:modelValue']);

// --- 상태 변수 (기존과 거의 동일) ---
const authStore = useAuthStore();
const post = ref<PostResponseDto | null>(null);
const isLoading = ref(true);

const allComments = ref<CommentResponseDto[]>([]);
const displayedComments = ref<CommentResponseDto[]>([]);
const commentsLimit = 20;
const isMoreCommentsLoading = ref(false);
const hasMoreComments = ref(true);

const profileImageMap = ref<Record<string, string>>({});
const createCommentRef = ref<InstanceType<typeof CreateComment> | null>(null);

// --- 정렬/필터링 상태 (기존과 동일) ---
type SortOrder = 'newest' | 'oldest';
type FilterMode = 'all' | 'friends';

const sortOrder = ref<SortOrder>('newest');
const filterMode = ref<FilterMode>('all');
const friendIds = ref<Set<string>>(new Set());

// --- 정렬/필터링된 댓글 목록 (Computed Property, 기존과 동일) ---
const processedComments = computed(() => {
  let commentsToProcess = [...allComments.value];
  if (filterMode.value === 'friends') {
    commentsToProcess = commentsToProcess.filter(comment =>
      friendIds.value.has(comment.user.userId)
    );
  }
  commentsToProcess.sort((a, b) => {
    const dateA = new Date(a.createdAt).getTime();
    const dateB = new Date(b.createdAt).getTime();
    return sortOrder.value === 'newest' ? dateB - dateA : dateA - dateB;
  });
  return commentsToProcess;
});

// --- 핵심 로직 함수 (모달 컨텍스트에 맞게 일부 수정) ---

const closeModal = () => {
  emit('update:modelValue', false);
};

const loadMoreComments = () => {
  if (isMoreCommentsLoading.value) return;
  isMoreCommentsLoading.value = true;
  const currentLength = displayedComments.value.length;
  const nextComments = processedComments.value.slice(currentLength, currentLength + commentsLimit);
  displayedComments.value.push(...nextComments);
  hasMoreComments.value = displayedComments.value.length < processedComments.value.length;
  isMoreCommentsLoading.value = false;
};

const resetAndLoadFirstPage = () => {
  displayedComments.value = [];
  loadMoreComments();
};

// --- 수정됨: 데이터 로딩이 props를 감시(watch)하여 트리거됩니다 ---
watch(() => props.postId, (newPostId) => {
  // 유효한 postId가 전달되고 모달이 열려있을 때 데이터를 가져옵니다.
  if (newPostId && props.modelValue) {
    fetchInitialData(newPostId);
  }
}, { immediate: true }); // `immediate: true`는 컴포넌트가 처음 생성될 때 즉시 실행되도록 보장합니다.

watch(() => props.modelValue, (isOpen) => {
    // 모달이 열릴 때, 현재 postId에 대한 데이터가 아직 로드되지 않았다면 가져옵니다.
    if (isOpen && props.postId && !post.value) {
        fetchInitialData(props.postId);
    }
    // 선택사항: 모달이 닫힐 때 메모리 확보를 위해 데이터를 비웁니다.
    if (!isOpen) {
        post.value = null;
        allComments.value = [];
        displayedComments.value = [];
        isLoading.value = true; // 다음 열기를 위해 로딩 상태 초기화
    }
});

watch(processedComments, () => {
  resetAndLoadFirstPage();
});

onMounted(() => {
  const friends = authStore.user?.friends ?? [];
  friendIds.value = new Set(friends.map((friend: any) => friend.userId));
  // props.postId에 대한 watcher가 초기 데이터 로딩을 처리합니다.
});

// --- 데이터 로딩 및 기타 메서드 (기존 코드와 대부분 동일) ---

const fetchInitialData = async (currentPostId: string) => {
  isLoading.value = true;
  try {
    // 상태 초기화
    post.value = null;
    allComments.value = [];
    displayedComments.value = [];
    hasMoreComments.value = true;

    const postResponse = await apiClient.get<PostResponseDto>(`/api/Post/${currentPostId}`);
    post.value = postResponse.data;

    let lastCommentId: string | null = null;
    while (true) {
      const fromParam = lastCommentId ? `&from=${lastCommentId}` : '';
      const requestUrl = `/api/Comment/${currentPostId}?limit=100${fromParam}`;
      const response = await apiClient.get<CommentResponseDto[]>(requestUrl);
      const newComments = response.data;
      if (newComments.length === 0) break;
      
      allComments.value = [...new Map([...allComments.value, ...newComments].map(c => [c.id, c])).values()];
      lastCommentId = newComments[newComments.length - 1].id;
      if (newComments.length < 100) break;
    }

    if (post.value) {
      const usersToLoad = new Set<UserDto>();
      usersToLoad.add(post.value.user);
      if ((post.value as any).isRepost && (post.value as any).parentPost?.user) {
        usersToLoad.add((post.value as any).parentPost.user);
      }
      allComments.value.forEach(comment => usersToLoad.add(comment.user));
      await prepareProfileImageMapForUsers(Array.from(usersToLoad));
    }
    // fetchInitialData가 끝나면 watch가 알아서 첫 페이지를 로드해줍니다.
  } catch (error) {
    console.error("초기 데이터 로딩 실패:", error);
    // 모달을 닫거나 에러 상태를 표시할 수 있습니다.
    // closeModal(); 
  } finally {
    isLoading.value = false;
  }
};

const refreshData = async () => {
    if(props.postId) {
        await fetchInitialData(props.postId);
    }
}


const getMediaBlobUrl = async (mediaId: string) => {
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, { responseType: 'blob' });
    const contentType = response.headers['content-type'];
    if (!contentType.startsWith('image')) return '';
    return URL.createObjectURL(response.data);
  } catch {
    return '';
  }
};

const prepareProfileImageMapForUsers = async (users: (UserDto | undefined)[]) => {
  const userIds = new Set<string>();
  users.forEach(user => {
    if (user?.profileThumbnailMediaId) userIds.add(user.userId);
  });
  for (const userId of userIds) {
    if (profileImageMap.value[userId] || !users.find(u => u?.userId === userId)) continue;
    const user = users.find(u => u?.userId === userId)!;
    const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId!);
    profileImageMap.value[userId] = blobUrl || '/src/assets/images/default_profile_image.jpg';
  }
};

const handleLikeComment = async (commentId: string) => {
  try {
    const response = await apiClient.post(`/api/Comment/${commentId}/like`);
    const updatedComment = response.data;
    const index = allComments.value.findIndex(c => c.id === commentId);
    if (index !== -1) allComments.value[index] = updatedComment;
    
    const displayIndex = displayedComments.value.findIndex(c => c.id === commentId);
    if (displayIndex !== -1) displayedComments.value[displayIndex] = updatedComment;
  } catch (error) {
    console.error('좋아요 처리 실패:', error);
    alert('좋아요 처리에 실패했습니다.');
  }
};

const handleUpdateComment = async ({ commentId, newText }: { commentId: string, newText: string }) => {
  try {
    const formData = new FormData();
    const jsonData = JSON.stringify([{ $type: 'text', Text: newText }]);
    formData.append('JsonData', jsonData);
    await apiClient.put(`/api/Comment/${commentId}`, formData);
    alert('댓글이 수정되었습니다.');
    await refreshData();
  } catch (error) {
    console.warn('PUT API 실패, 삭제 후 재등록 방식으로 시도:', error);
    try {
      await apiClient.delete(`/api/Comment/${commentId}`);
      const formData = new FormData();
      const jsonData = JSON.stringify([{ $type: 'text', Text: newText }]);
      formData.append('JsonData', jsonData);
      await apiClient.post(`/api/Comment/${props.postId}`, formData); // props.postId 사용
      alert('댓글이 수정되었습니다.');
      await refreshData();
    } catch (secondError) {
      console.error('삭제 후 재등록 실패:', secondError);
      alert('댓글 수정에 실패했습니다.');
    }
  }
};

const deleteMyComment = async (commentId: string) => {
  try {
    await apiClient.delete(`/api/Comment/${commentId}`);
    allComments.value = allComments.value.filter(c => c.id !== commentId);
  } catch (error) {
    alert('댓글 삭제에 실패했습니다.');
  }
};

const handleMentionUser = (nickname: string) => {
  createCommentRef.value?.addMention(nickname);
};

</script>

<template>
  <Teleport to="body">
    <div v-if="modelValue" class="modal-overlay" @click="closeModal">
      <div class="modal-container" @click.stop>
        <div class="modal-header">
          <button @click="closeModal" class="close-button">&times;</button>
        </div>
        <div class="modal-body">
          <main v-if="isLoading" class="loading-indicator">
            <div class="spinner"></div>
          </main>
          <main v-else-if="post" class="main-content">
            <div class="post-section">
              <PostCard :post="post" :show-actions="true" :profile-image-map="profileImageMap" />
            </div>
            <div class="comments-section">
              <h3>댓글 ({{ processedComments.length }})</h3>
              <div class="comment-controls">
                <div class="filter-group">
                  <button @click="filterMode = 'all'" :class="{ active: filterMode === 'all' }">전체</button>
                </div>
                <div class="sort-group">
                  <button @click="sortOrder = 'newest'" :class="{ active: sortOrder === 'newest' }">최신순</button>
                  <button @click="sortOrder = 'oldest'" :class="{ active: sortOrder === 'oldest' }">오래된순</button>
                </div>
              </div>
              <CommentItem 
                v-for="comment in displayedComments" 
                :key="comment.id" 
                :comment="comment"
                :profile-image-url="profileImageMap[comment.user.userId] || '/src/assets/images/default_profile_image.jpg'"
                @mention-user="handleMentionUser"
                @delete-comment="deleteMyComment"
                @like-comment="handleLikeComment"
                @update-comment="handleUpdateComment" />

              <div v-if="hasMoreComments" class="load-more-container">
                <button @click="loadMoreComments" :disabled="isMoreCommentsLoading" class="load-more-btn">
                  {{ isMoreCommentsLoading ? '로딩 중...' : '댓글 더보기' }}
                </button>
              </div>
              <CreateComment :post-id="postId" @comment-created="refreshData" ref="createCommentRef" />
            </div>
          </main>
          <main v-else class="error-indicator">
            게시물을 불러오지 못했습니다.
          </main>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
/* --- 모달 스타일 --- */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-container {
  background-color: #f9f9f9; /* 스크린샷의 어두운 테마 대신 밝은 테마 유지 */
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.2);
  width: 90%;
  max-width: 950px;
  height: 90vh;
  max-height: 900px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.modal-header {
  padding: 10px 16px;
  text-align: right;
  flex-shrink: 0;
}

.close-button {
  background: none;
  border: none;
  font-size: 2rem;
  font-weight: 300;
  color: #888;
  cursor: pointer;
  line-height: 1;
}

.modal-body {
  padding: 0 24px 24px 24px;
  overflow-y: auto;
  flex-grow: 1;
}

/* --- 기존 콘텐츠 스타일 (일부 수정) --- */
.main-content {
  width: 100%;
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
.loading-indicator, .error-indicator { 
  text-align: center; 
  padding: 60px 20px; 
  color: #666;
}

.spinner { 
  border: 4px solid rgba(0, 0, 0, 0.1);
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border-left-color: #09f;
  animation: spin 1s ease infinite;
  margin: 0 auto;
}
@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

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

.comment-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
  margin-bottom: 16px;
  border-bottom: 1px solid #eee;
}

.comment-controls button {
  background: none;
  border: none;
  padding: 6px 10px;
  cursor: pointer;
  color: #888;
  font-weight: 500;
  border-radius: 6px;
  transition: background-color 0.2s, color 0.2s;
}

.comment-controls button:hover {
  background-color: #f0f2f5;
}

.comment-controls button.active {
  color: #1877f2;
  font-weight: 700;
}

.filter-group, .sort-group {
  display: flex;
  gap: 8px;
}
</style>