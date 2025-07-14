import { ref, computed, watch } from 'vue';
import apiClient from '@/api';
import type { PostResponseDto, CommentResponseDto, UserDto } from '@/types';
import { useAuthStore } from '@/stores/auth';

type SortOrder = 'newest' | 'oldest';

export function useComments(post: PostResponseDto) {
  const authStore = useAuthStore();

  // --- 상태 변수 ---
  const allComments = ref<CommentResponseDto[]>([]);
  const displayedComments = ref<CommentResponseDto[]>([]);
  const profileImageMap = ref<Record<string, string>>({});
  
  const isLoading = ref(false);
  const isMoreCommentsLoading = ref(false);
  const commentsLimit = 20;
  const hasMoreComments = ref(true);
  const commentsCount = ref(0); // 댓글 수 상태 추가

  const sortOrder = ref<SortOrder>('newest');

  // --- Computed ---
  const processedComments = computed(() => {
    let commentsToProcess = [...allComments.value];
    commentsToProcess.sort((a, b) => {
      const dateA = new Date(a.createdAt).getTime();
      const dateB = new Date(b.createdAt).getTime();
      return sortOrder.value === 'newest' ? dateB - dateA : dateA - dateB;
    });
    return commentsToProcess;
  });

  // --- 데이터 로딩 ---
  const fetchInitialData = async () => {
    if (isLoading.value) return;
    isLoading.value = true;
    try {
      allComments.value = [];
      displayedComments.value = [];
      hasMoreComments.value = true;

      let lastCommentId: string | null = null;
      while (true) {
        const fromParam = lastCommentId ? `&from=${lastCommentId}` : '';
        const response = await apiClient.get<CommentResponseDto[]>(`/api/Comment/${post.id}?limit=100${fromParam}`);
        const newComments = response.data; // API 응답 구조에 맞게 수정
        if (newComments.length === 0) break;
        
        allComments.value.push(...newComments);
        commentsCount.value = allComments.value.length; // 댓글 수 업데이트
        lastCommentId = newComments[newComments.length - 1].id;
        if (newComments.length < 100) break;
      }
      
      const usersToLoad = new Set<UserDto>();
      allComments.value.forEach(comment => usersToLoad.add(comment.user));
      await prepareProfileImageMapForUsers(Array.from(usersToLoad));

      resetAndLoadFirstPage();

    } catch (error) {
      console.error("댓글 데이터 로딩 실패:", error);
    } finally {
      isLoading.value = false;
    }
  };

  const refreshData = async () => {
    await fetchInitialData();
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

  const getMediaBlobUrl = async (mediaId: string) => {
    try {
      const response = await apiClient.get(`/api/Media/${mediaId}`, { responseType: 'blob' });
      return URL.createObjectURL(response.data);
    } catch {
      return '';
    }
  };

  const prepareProfileImageMapForUsers = async (users: UserDto[]) => {
    const userIds = new Set<string>();
    users.forEach(user => {
      if (user?.profileThumbnailMediaId) userIds.add(user.userId);
    });
    for (const userId of userIds) {
      if (profileImageMap.value[userId]) continue;
      const user = users.find(u => u.userId === userId)!;
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId!);
      profileImageMap.value[userId] = blobUrl || '/src/assets/images/default_profile_image.jpg';
    }
  };

  // --- 댓글 CRUD ---
  const handleLikeComment = async (commentId: string) => {
    try {
      const response = await apiClient.post(`/api/Comment/${commentId}/like`);
      const updatedComment = response.data;
      const index = allComments.value.findIndex(c => c.id === commentId);
      if (index !== -1) {
        const newAllComments = [...allComments.value];
        newAllComments[index] = updatedComment;
        allComments.value = newAllComments;
        // displayedComments도 업데이트되도록 강제
        resetAndLoadFirstPage();
      }
    } catch (error) {
      console.error('좋아요 처리 실패:', error);
      alert('좋아요 처리에 실패했습니다.');
    }
  };

  const deleteMyComment = async (commentId: string) => {
    try {
      await apiClient.delete(`/api/Comment/${commentId}`);
      allComments.value = allComments.value.filter(c => c.id !== commentId);
      commentsCount.value = allComments.value.length; // 댓글 수 업데이트
      resetAndLoadFirstPage();
    } catch (error) {
      alert('댓글 삭제에 실패했습니다.');
    }
  };
  
  const handleUpdateComment = async ({ commentId }: { commentId: string }) => {
      // 수정 후에는 전체 데이터를 새로고침하여 변경사항을 반영
      await refreshData();
  };


  watch(sortOrder, () => {
    resetAndLoadFirstPage();
  });

  return {
    // 상태
    allComments,
    displayedComments,
    profileImageMap,
    isLoading,
    isMoreCommentsLoading,
    hasMoreComments,
    sortOrder,

    // 함수
    fetchInitialData,
    refreshData,
    loadMoreComments,
    handleLikeComment,
    deleteMyComment,
    handleUpdateComment,
    commentsCount, // commentsCount 반환
  };
}