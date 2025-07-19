import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import apiClient from '@/api'; 
import type { PostResponseDto } from '@/types';

export function usePostActions(post: PostResponseDto, emit: (event: 'open-detail', ...args: any[]) => void) {
  const router = useRouter();
  const authStore = useAuthStore();
  const uiStore = useUiStore();

  const showReportModal = ref(false);
  const selectedReason = ref('ExplicitContent');
  const showAccessDeniedModal = ref(false);
  const deniedUserId = ref('');
  const deniedUserNickname = ref('');

  const canEdit = computed(() => {
    return authStore.user && authStore.user.userId === post.user.userId;
  });

  const openShareEditor = () => {
    console.log('[usePostActions] openShareEditor 호출됨');
    uiStore.openShareEditor(post);
  };

  const handleInstantRepost = async () => {
    if (confirm('이 게시물을 리포스트하시겠습니까?')) {
      try {
        await apiClient.post(`/api/Post/${post.id}/repost`);
        alert('게시물이 리포스트되었습니다.');
      } catch (error) {
        console.error('리포스트 실패:', error);
        alert('리포스트에 실패했습니다.');
      }
    }
  };

  const deleteMyPost = async (): Promise<boolean> => {
    if (confirm('정말 삭제하시겠습니까?')) {
      try {
        await apiClient.delete(`/api/Post/${post.id}`);
        alert('삭제되었습니다.');
        return true;
      } catch (error) {
        console.error('삭제 실패:', error);
        alert('삭제에 실패했습니다.');
        return false;
      }
    }
    return false;
  };

  const submitReport = async () => {
    if (!selectedReason.value) {
      alert('신고 사유를 선택해주세요.');
      return;
    }
    try {
      await apiClient.post(`/api/Post/${post.id}/report`, { reason: selectedReason.value });
      alert('게시물이 신고되었습니다. 검토 후 조치하겠습니다.');
      showReportModal.value = false;
      selectedReason.value = 'ExplicitContent';
    } catch (error) {
      console.error('신고 실패:', error);
      alert('신고에 실패했습니다.');
    }
  };

  /**
   * 게시글 홍보
   */
  const promotePost = async () => {
    if (confirm("...")) {
      try {
        await apiClient.post(`/api/post/public-post/${post.id}`);
        alert('게시글이 성공적으로 홍보되었습니다! ...');
      } catch (error: any) { // error 타입을 any로 지정하여 response에 접근
        console.error('게시글 홍보 실패:', error);
  
        // 429 에러(Too Many Requests)가 오면 시간제한 메시지를 보여줌
        if (error.response && error.response.status === 429) {
          alert('게시글 홍보는 24시간에 한 번만 가능합니다.');
        } else {
          alert('게시글 홍보에 실패했습니다.');
        }
      }
    }
  };

  /**
   * 공개범위 변경
   */
  const changeDiscoveryOption = async (option: 'Public' | 'FriendsOnly' | 'Private') => {
    try {
      await apiClient.patch(`/api/Post/${post.id}/discovery`, { option });
      alert('공개범위가 성공적으로 변경되었습니다!');
    } catch (error) {
      console.error('공개범위 변경 실패:', error);
      alert('공개범위 변경에 실패했습니다.');
    }
  };

  /**
   * 프로필에 고정/고정 해제
   */
  const togglePinPost = async () => {
    if (confirm('프로필의 고정된 게시글을 이 게시물로 변경(또는 해제)하시겠습니까?')) {
      try {
        await apiClient.post(`/api/User/me/pinned-post`, { postId: post.id });
        alert('프로필 고정 상태가 변경되었습니다!');
      } catch (error) {
        console.error('프로필 고정 실패:', error);
        alert('프로필 고정 상태 변경에 실패했습니다.');
      }
    }
  };

  /**
   * 관심글로 저장/해제 (북마크)
   */
  const toggleBookmark = async () => {
    try {
      await apiClient.post(`/api/Post/${post.id}/bookmark`);
      alert('관심글 상태가 변경되었습니다!');
    } catch (error) {
      console.error('관심글 저장/해제 실패:', error);
      alert('관심글 저장/해제에 실패했습니다.');
    }
  };

  /**
   * 이 글 알림 끄기/켜기
   */
  const toggleNotifications = async () => {
    try {
      await apiClient.post(`/api/Post/${post.id}/notifications/toggle`);
      alert('이 글의 알림 설정이 변경되었습니다!');
    } catch (error) {
      console.error('알림 설정 변경 실패:', error);
      alert('알림 설정 변경에 실패했습니다.');
    }
  };
  
  const navigateToProfile = (nickname: string) => {
    router.push(`/user/${nickname}`);
  };

  const goToOriginalPost = async () => {
    const parentPost = (post as any).parentPost;

    if (!parentPost) return;

    const parentPostId = parentPost.id || parentPost;

    try {
      await apiClient.get(`/api/Post/${parentPostId}`); 
      router.push(`/post/${parentPostId}`);
    } catch (err: any) {
      if (err.response && err.response.status === 403) {
        deniedUserId.value = parentPost.user?.userId || '';
        deniedUserNickname.value = parentPost.user?.nickname || '작성자';
        showAccessDeniedModal.value = true;
      } else {
        alert('게시글을 불러오는 중 문제가 발생했습니다.');
      }
    }
  };

  const handleSendFriendRequest = async (userId: string) => {
    try {
      alert(`${deniedUserNickname.value}님에게 친구 신청을 보냈습니다.`);
      showAccessDeniedModal.value = false; 
    } catch (error) {
      console.error('친구 신청 실패:', error);
      alert('친구 신청에 실패했습니다. 다시 시도해주세요.');
    }
  };

  const goToPostDetail = (postId: string) => {
    emit('open-detail', postId);
  };

  const openReportDialog = () => {
    showReportModal.value = true;
  };

  const cancelReport = () => {
    showReportModal.value = false;
    selectedReason.value = 'ExplicitContent';
  };

  return {
    canEdit,
    openShareEditor,
    handleInstantRepost,
    deleteMyPost,
    submitReport,
    navigateToProfile,
    goToOriginalPost,
    goToPostDetail,
    openReportDialog,
    cancelReport,
    showReportModal,
    selectedReason,
    showAccessDeniedModal,
    deniedUserId,
    deniedUserNickname,
    handleSendFriendRequest,
    promotePost,
    changeDiscoveryOption,
    togglePinPost,
    toggleBookmark,
    toggleNotifications,
  };
}