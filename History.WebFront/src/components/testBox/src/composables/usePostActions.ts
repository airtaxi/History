/**
 * usePostActions
 *
 * 이 컴포저블은 게시물과 관련된 다양한 사용자 액션(공유, 리포스트, 삭제, 신고, 프로필 이동 등)을 처리하는 기능을 제공합니다.
 * Vue Router, 인증 스토어, UI 스토어, API 클라이언트 등 외부 의존성을 주입받아 사용합니다.
 *
 * @param {Object} options - 컴포저블 옵션 객체
 * @param {Router} options.router - Vue Router 인스턴스
 * @param {AuthStore} options.authStore - 인증(Auth) 스토어 인스턴스
 * @param {UiStore} options.uiStore - UI 스토어 인스턴스
 * @param {PostResponseDto} options.post - 현재 게시물 데이터 객체
 * @param {Function} options.emit - 컴포넌트 이벤트를 발생시키는 emit 함수
 *
 * @returns {Object} 게시물 액션 관련 함수들을 포함하는 객체
 * @property {Function} openShareEditor - 게시물 공유 에디터를 여는 함수.
 * @property {Function} handleInstantRepost - 즉시 리포스트를 실행하는 함수.
 * @property {Function} deleteMyPost - 게시물을 삭제하는 함수.
 * @property {Function} submitReport - 게시물을 신고하는 함수.
 * @property {Function} navigateToProfile - @멘션 클릭 시 사용자 프로필로 이동하는 함수.
 * @property {Function} goToOriginalPost - 리포스트된 원본 게시물로 이동하는 함수.
 * @property {Function} goToPostDetail - 게시물 상세 페이지로 이동하는 함수.
 * @property {Function} openReportDialog - 신고 다이얼로그를 여는 함수.
 * @property {Function} cancelReport - 신고를 취소하는 함수.
 * @property {Ref<boolean>} showReportModal - 신고 모달 표시 여부 상태.
 * @property {Ref<string>} selectedReason - 선택된 신고 사유 상태.
 * @property {Ref<boolean>} showAccessDeniedModal - 접근 거부 모달 표시 여부 상태.
 * @property {Ref<string>} deniedUserId - 접근이 거부된 사용자 ID 상태.
 * @property {Ref<string>} deniedUserNickname - 접근이 거부된 사용자 닉네임 상태.
 */
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

  /**
   * 게시글 편집 권한 확인
   * 현재 사용자가 이 게시글을 수정/삭제할 수 있는지 확인합니다.
   * @returns {boolean} 편집 권한 여부
   */
  const canEdit = computed(() => {
    return authStore.user && authStore.user.userId === post.user.userId;
  });

  /**
   * 게시물 공유 에디터를 엽니다.
   */
  const openShareEditor = () => {
    uiStore.openShareEditor(post);
  };

  /**
   * 즉시 리포스트를 실행합니다.
   * 확인창 후 바로 리포스트 API를 호출합니다.
   */
  const handleInstantRepost = async () => {
    if (confirm('이 게시물을 리포스트하시겠습니까?')) {
      try {
        await apiClient.post(`/api/Post/${post.id}/repost`);
        alert('게시물이 리포스트되었습니다.');
        // TODO: 리포스트 성공 후 UI 업데이트 로직 (예: 카운트 증가)
      } catch (error) {
        console.error('리포스트 실패:', error);
        alert('리포스트에 실패했습니다.');
      }
    }
  };

  /**
   * 내 게시글을 삭제합니다.
   * 사용자 확인 후 서버에서 게시글을 삭제하고 이전 페이지로 돌아갑니다.
   */
  const deleteMyPost = async () => {
    if (confirm('정말 삭제하시겠습니까?')) {
      try {
        await apiClient.delete(`/api/Post/${post.id}`);
        alert('삭제되었습니다.');
        router.back();
      } catch (error) {
        console.error('삭제 실패:', error);
        alert('삭제에 실패했습니다.');
      }
    }
  };

  /**
   * 게시물을 신고합니다.
   */
  const submitReport = async () => {
    if (!selectedReason.value) {
      alert('신고 사유를 선택해주세요.');
      return;
    }
    try {
      await apiClient.post(`/api/Post/${post.id}/report`, { reason: selectedReason.value });
      alert('게시물이 신고되었습니다. 검토 후 조치하겠습니다.');
      showReportModal.value = false;
      selectedReason.value = 'ExplicitContent'; // 기본값으로 초기화
    } catch (error) {
      console.error('신고 실패:', error);
      alert('신고에 실패했습니다.');
    }
  };

  /**
   * @멘션 클릭 시 사용자 프로필로 이동합니다.
   * @param {string} nickname - 멘션된 사용자의 닉네임 (실제로는 userId를 받아야 함)
   */
  const navigateToProfile = (nickname: string) => {
    // TODO: 닉네임으로 userId를 찾아 이동하는 로직 필요. 현재는 임시로 닉네임을 userId로 간주
    router.push(`/user/${nickname}`);
  };

  /**
   * 원본 게시글로 이동 (리포스트인 경우)
   * 현재 게시글이 리포스트인 경우, 원본 게시글의 상세 페이지로 이동합니다.
   * parentPost가 존재하는 경우에만 동작합니다.
   */
  const goToOriginalPost = async () => {
    const parentPost = (post as any).parentPost;

    if (!parentPost) return;

    const parentPostId = parentPost.id || parentPost;

    try {
      await apiClient.get(`/api/Post/${parentPostId}`); // 접근 권한 확인
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

  /**
   * 게시물 상세 페이지로 이동합니다.
   * @param {string} postId - 상세 페이지로 이동할 게시물의 ID.
   */
  const goToPostDetail = (postId: string) => {
    emit('open-detail', postId);
  };

  /**
   * 신고 다이얼로그를 엽니다.
   */
  const openReportDialog = () => {
    showReportModal.value = true;
  };

  /**
   * 신고를 취소합니다.
   */
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
  };
}