import { ref, computed, watch } from 'vue';
import apiClient from '@/api';
import type { PostResponseDto, CommentResponseDto, UserDto } from '@/types';


type SortOrder = 'newest' | 'oldest';

export function useComments(post: PostResponseDto) {
  

  // --- 상태 변수 ---
  const allComments = ref<CommentResponseDto[]>([]);
  const displayedComments = ref<CommentResponseDto[]>([]);
  const profileImageMap = ref<Record<string, string>>({});
  
  const isLoading = ref(false);
  const isMoreCommentsLoading = ref(false);
  const commentsLimit = 20;
  const hasMoreComments = ref(true);
  const commentsCount = ref(post.commentsCount || 0); // post.commentsCount 값으로 초기화

  const sortOrder = ref<SortOrder>('newest');

  // --- Computed ---
  const processedComments = computed(() => {
    const commentsToProcess = [...allComments.value];
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
      const fetchedComments: CommentResponseDto[] = [];
      while (true) {
        const fromParam: string = lastCommentId ? `&from=${lastCommentId}` : '';
        const response = await apiClient.get<CommentResponseDto[]>(`/api/Comment/${post.id}?limit=100${fromParam}`);
        const newComments: CommentResponseDto[] = response.data;
        if (newComments.length === 0) break;
        
        fetchedComments.push(...newComments);
        lastCommentId = newComments[newComments.length - 1].id;
        if (newComments.length < 100) break;
      }
      allComments.value = fetchedComments;
      commentsCount.value = fetchedComments.length; // 모든 댓글을 가져온 후 한번에 업데이트
      
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
      const index = allComments.value.findIndex(c => c.id === commentId);
      if (index !== -1) {
        allComments.value.splice(index, 1);
        commentsCount.value--; // 전체 개수를 다시 세는 대신 1 감소
      }
      resetAndLoadFirstPage();
    } catch {
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
    commentsCount,
  };
}
