<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'; // computed 추가
import { useRoute, useRouter } from 'vue-router';
import apiClient from '@/api';
import type { PostResponseDto, CommentResponseDto, UserDto } from '@/types';
import { useAuthStore } from '@/stores/auth';
import PostCard from '@/components/PostCard.vue';
import CommentItem from '@/components/CommentItem.vue';
import CreateComment from '@/components/CreateComment.vue';

// --- 기본 상태 변수 (기존과 동일) ---
const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const postId = route.params.postId as string;

const post = ref<PostResponseDto | null>(null);
const isLoading = ref(true);

const allComments = ref<CommentResponseDto[]>([]);
const displayedComments = ref<CommentResponseDto[]>([]);
const commentsLimit = 20;
const isMoreCommentsLoading = ref(false);
const hasMoreComments = ref(true);

const profileImageMap = ref<Record<string, string>>({});
const createCommentRef = ref<InstanceType<typeof CreateComment> | null>(null);

// --- 추가: 정렬/필터링 상태 ---
type SortOrder = 'newest' | 'oldest';
type FilterMode = 'all' | 'friends';

const sortOrder = ref<SortOrder>('newest');
const filterMode = ref<FilterMode>('all');
const friendIds = ref<Set<string>>(new Set()); // 친구 ID 목록을 Set으로 관리하여 조회 성능 향상

// --- 추가: 정렬/필터링된 댓글 목록 (Computed Property) ---
// allComments, filterMode, sortOrder가 변경될 때마다 자동으로 다시 계산됩니다.
const processedComments = computed(() => {
  // 얕은 복사를 통해 원본 배열(allComments)의 순서를 변경하지 않도록 합니다.
  let commentsToProcess = [...allComments.value]; 

  // 1. 필터링 (친구 댓글만)
  if (filterMode.value === 'friends') {
    commentsToProcess = commentsToProcess.filter(comment => 
      friendIds.value.has(comment.user.userId)
    );
  }

  // 2. 정렬 (최신순/오래된순)
  commentsToProcess.sort((a, b) => {
    const dateA = new Date(a.createdAt).getTime();
    const dateB = new Date(b.createdAt).getTime();
    return sortOrder.value === 'newest' ? dateB - dateA : dateA - dateB;
  });

  return commentsToProcess;
});


// 변경: '더보기' 로직이 원본(allComments) 대신 처리된 목록(processedComments)을 사용하도록 수정
const loadMoreComments = () => {
  if (isMoreCommentsLoading.value) return;
  isMoreCommentsLoading.value = true;

  const currentLength = displayedComments.value.length;
  // processedComments에서 다음 20개를 가져옵니다.
  const nextComments = processedComments.value.slice(currentLength, currentLength + commentsLimit);
  displayedComments.value.push(...nextComments);
  
  // 더 보여줄 댓글이 있는지 여부도 processedComments 기준으로 판단합니다.
  hasMoreComments.value = displayedComments.value.length < processedComments.value.length;
  isMoreCommentsLoading.value = false;
};

// 추가: 필터/정렬 변경 시, 화면에 표시된 댓글 목록을 초기화하고 첫 페이지를 다시 로드하는 함수
const resetAndLoadFirstPage = () => {
    displayedComments.value = []; // 화면 초기화
    loadMoreComments();           // 처리된 목록의 첫 페이지 로드
};

// 추가: 정렬/필터링 조건이 바뀔 때마다(processedComments가 변경될 때마다) 페이지네이션을 리셋
watch(processedComments, () => {
    resetAndLoadFirstPage();
});


onMounted(() => {
  // 친구 목록 가져오기 (authStore에 친구 정보가 로드된 후를 가정)
  // authStore.user.friends가 없다면 빈 배열로 처리하여 오류 방지
  // 실제 친구 목록의 구조에 맞게 (e.g., authStore.friends) 수정이 필요할 수 있습니다.
  const friends = authStore.user?.friends ?? []; 
  friendIds.value = new Set(friends.map((friend: any) => friend.userId));
  
  // 기존 데이터 로딩 로직 호출
  fetchInitialData(postId);
});


// getMediaBlobUrl, prepareProfileImageMapForUsers, fetchInitialData, refreshData 등
// 나머지 함수들은 기존 코드를 그대로 사용하시면 됩니다.
// (단, fetchInitialData 내부의 로직은 allComments를 채우는 역할만 하고,
//  화면에 보여주는 것은 watch와 computed 속성이 알아서 처리해줍니다.)

// ...(기존의 나머지 script 코드)...
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

const fetchInitialData = async (currentPostId: string) => {
  isLoading.value = true;
  try {
    post.value = null;
    allComments.value = [];
    displayedComments.value = [];
    hasMoreComments.value = true;

    const postResponse = await apiClient.get<PostResponseDto>(`/api/Post/${currentPostId}`);
    post.value = postResponse.data;

    let lastCommentId: string | null = null;
    while (true) {
        const fromParam: string = lastCommentId ? `&from=${lastCommentId}` : '';
        const requestUrl: string = `/api/Comment/${currentPostId}?limit=100${fromParam}`;
        const response: { data: CommentResponseDto[] } = await apiClient.get(requestUrl);
        const newComments: CommentResponseDto[] = response.data;

      if (newComments.length === 0) {
        break;
      }
      
      allComments.value.push(...newComments);
      lastCommentId = newComments[newComments.length - 1].id;

      if (newComments.length < 100) {
        break;
      }
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
  } finally {
    isLoading.value = false;
  }
};

const refreshData = async () => {
    await fetchInitialData(postId);
}

const handleLikeComment = async (commentId: string) => {
  try {
    const response = await apiClient.post(`/api/Comment/${commentId}/like`);
    const updatedComment = response.data;

    const index = allComments.value.findIndex(c => c.id === commentId);
    if (index !== -1) {
      allComments.value[index] = updatedComment;
    }

    const displayIndex = displayedComments.value.findIndex(c => c.id === commentId);
    if (displayIndex !== -1) {
      displayedComments.value[displayIndex] = updatedComment;
    }

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
      await apiClient.post(`/api/Comment/${postId}`, formData);
      
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

watch(() => route.params.postId, (newPostId) => {
  if (newPostId && newPostId !== post.value?.id) {
    fetchInitialData(newPostId as string);
  }
});
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