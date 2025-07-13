/**
 * useReactions
 *
 * 이 컴포저블은 게시물 반응(좋아요, 멋져요 등)과 관련된 모든 상태와 로직을 캡슐화합니다.
 * 반응 데이터 로딩, 반응 추가/변경/삭제, 롱 프레스 이벤트 처리, 반응 선택 팝업 관리 등을 담당합니다.
 *
 * @param {Object} post - 현재 게시물 데이터 객체 (PostResponseDto 타입)
 * @returns {Object} 반응 관련 상태 및 함수들을 포함하는 객체
 * @property {Ref<Record<string, number>>} reactionMap - 반응 타입별 개수 (예: { "Like": 10, "Awesome": 5 }).
 * @property {Ref<string | null>} myReaction - 현재 사용자가 누른 반응 (예: "Like", "Awesome" 또는 null).
 * @property {Ref<Record<string, Array<any>>>} reactionUsersMap - 반응별 사용자 정보 (툴팁용).
 * @property {Ref<boolean>} showReactionPopup - 반응 선택 팝업 표시 여부.
 * @property {Ref<{ top: string, left: string }>} reactionPopupPosition - 반응 선택 팝업의 위치.
 * @property {Ref<string | null>} hoveredReaction - 현재 마우스 오버된 반응 (툴팁용).
 * @property {Ref<{ top: string, string }>} tooltipPosition - 반응 툴팁의 위치.
 * @property {Function} loadReactionData - 서버에서 반응 데이터를 로드하는 함수.
 * @property {Function} postReaction - 반응을 추가/변경/삭제하는 함수 (Optimistic Update 적용).
 * @property {Function} handleReactionClick - 반응 버튼 클릭 핸들러 (롱 프레스와 연동).
 * @property {Function} selectReaction - 팝업에서 특정 반응을 선택하는 함수.
 * @property {Function} startLongPress - 반응 버튼 롱 프레스 시작 핸들러.
 * @property {Function} endLongPress - 반응 버튼 롱 프레스 종료 핸들러.
 * @property {Function} createFloatingEmoji - 반응 선택 시 이모지가 떠오르는 애니메이션 생성 함수.
 */
import { ref, type Ref, onMounted, onUnmounted } from 'vue';
import apiClient from '@/api';
import { useLongPress } from './useLongPress';
import { useUiStore } from '@/stores/ui';
import { useAuthStore } from '@/stores/auth';
import type { PostResponseDto } from '@/types';

export function useReactions(post: PostResponseDto) {
  const reactionMap: Ref<Record<string, number>> = ref({});
  const myReaction: Ref<string | null> = ref(null);
  const reactionUsersMap: Ref<Record<string, Array<any>>> = ref({});
  const showReactionPopup: Ref<boolean> = ref(false);
  const reactionPopupPosition: Ref<{ top: string, left: string }> = ref({ top: '0px', left: '0px' });
  const hoveredReaction: Ref<string | null> = ref(null);
  const tooltipPosition: Ref<{ top: string, left: string }> = ref({ top: '0px', left: '0px' });

  const uiStore = useUiStore();
  const authStore = useAuthStore();

  // useLongPress 컴포저블 인스턴스 생성
  const longPress = useLongPress(500);

  /**
   * 서버에서 게시물의 반응 데이터를 로드하고 상태를 업데이트합니다.
   */
  const loadReactionData = async () => {
    try {
      const response = await apiClient.get(`/api/Post/${post.id}`);
      const postData = response.data;

      const postReactions = postData.postReactions || [];
      const counts: Record<string, number> = {};
      const usersMap: Record<string, Array<any>> = {};
      let currentUserReaction: string | null = null;

      postReactions.forEach((reaction: any) => {
        const reactionType = reaction.type || reaction.reactionType;
        const user = reaction.user;

        if (reactionType && user) {
          counts[reactionType] = (counts[reactionType] || 0) + 1;

          if (!usersMap[reactionType]) {
            usersMap[reactionType] = [];
          }
          usersMap[reactionType].push({
            userId: user.userId,
            nickname: user.nickname || user.handle || 'Unknown',
            profileImageUrl: user.profileThumbnailMediaId // Assuming this is handled by PostCard or parent
          });

          if (user.userId === authStore.user?.userId) { // Assuming authStore has current user info
            currentUserReaction = reactionType;
          }
        }
      });

      reactionMap.value = counts;

      myReaction.value = currentUserReaction;
      reactionUsersMap.value = usersMap;

    } catch (error) {
      console.error('반응 데이터를 로드하는 데 실패했습니다:', error);
    }
  };

  /**
   * 게시물에 반응을 추가, 변경 또는 삭제합니다.
   * 낙관적 업데이트를 적용하여 사용자 경험을 향상시킵니다.
   * @param {string | null} reactionType - 설정할 반응 타입 (예: 'Like', 'Awesome'). null이면 반응 삭제.
   */
  const postReaction = async (newType: string) => {
    const previousReaction = myReaction.value;
    const originalReactionMap = { ...reactionMap.value };



    try {
      if (previousReaction === newType) {

        // === 시나리오 1: 같은 반응 재클릭 → 해제 ===
        reactionMap.value[newType] = Math.max((reactionMap.value[newType] || 1) - 1, 0);
        myReaction.value = null;

        await apiClient.post(`/api/Post/${post.id}/reaction/${newType}`);

      } else if (previousReaction && previousReaction !== newType) {
        // === 시나리오 2: 다른 반응으로 변경 ===
        reactionMap.value[previousReaction] = Math.max((reactionMap.value[previousReaction] || 1) - 1, 0);
        reactionMap.value[newType] = (reactionMap.value[newType] || 0) + 1;
        myReaction.value = newType;

        // 1차: 기존 반응 제거 (서버의 토글 방식 때문에 필요)
        await apiClient.post(`/api/Post/${post.id}/reaction/${previousReaction}`);

        // 2차: 새 반응 추가
        await apiClient.post(`/api/Post/${post.id}/reaction/${newType}`);

      } else {
        // === 시나리오 3: 새로운 반응 추가 ===
        reactionMap.value[newType] = (reactionMap.value[newType] || 0) + 1;
        myReaction.value = newType;

        await apiClient.post(`/api/Post/${post.id}/reaction/${newType}`);
      }
      // 최종 서버 데이터로 동기화 (실제 데이터와 일치 보장)
      await loadReactionData();

    } catch (err: any) {
      console.error('반응 처리 실패:', err);
      console.error('에러 응답:', err.response?.data);

      // 실패 시 원래 상태로 롤백 (사용자가 혼란스럽지 않도록)
      reactionMap.value = originalReactionMap;
      myReaction.value = previousReaction;

      alert('요청 처리에 실패했습니다. 잠시 후 다시 시도해 주세요.');
    }
  };

  /**
   * 반응 버튼 클릭 핸들러.
   * 현재 반응이 없으면 'Like'로 반응하고, 이미 반응이 있으면 반응 선택 팝업을 토글합니다.
   * @param {Event} event - 클릭 이벤트 객체.
   */
  const handleReactionClick = (event: MouseEvent) => {
    // Always attempt to post/toggle 'Like' reaction when the main button is clicked.
    // The postReaction function will handle the logic of adding, removing, or changing.
    postReaction('Like');
  };

  /**
   * 반응 선택 팝업을 토글하고 위치를 설정합니다.
   * @param {Event} event - 이벤트 객체.
   */
  const toggleReactionPopup = (event: Event) => {
    const target = event.currentTarget as HTMLElement;
    if (!target) return;

    showReactionPopup.value = !showReactionPopup.value;
    if (showReactionPopup.value) {
      const rect = target.getBoundingClientRect();
      reactionPopupPosition.value = {
        top: `${rect.top - 60}px`, // 버튼 위쪽에 위치
        left: `${rect.left + rect.width / 2}px`, // 버튼 중앙에 위치
      };
    }
  };

  /**
   * 팝업에서 특정 반응을 선택합니다.
   * @param {string} reactionType - 선택된 반응 타입.
   */
  const selectReaction = (reactionType: string) => {
    postReaction(reactionType);
    showReactionPopup.value = false;
    createFloatingEmoji(reactionType); // 이모지 애니메이션
  };

  /**
   * 반응 버튼 롱 프레스 시작 핸들러.
   * 롱 프레스 감지 시 반응 선택 팝업을 표시합니다.
   * @param {Event} event - 이벤트 객체.
   */
  const startLongPress = (event: Event) => {
    const targetElement = event.currentTarget as HTMLElement;
    if (!targetElement) return;

    longPress.start(() => {
      showReactionPopup.value = true;
      const rect = targetElement.getBoundingClientRect();
      reactionPopupPosition.value = {
        top: `${rect.top - 60}px`,
        left: `${rect.left + rect.width / 2}px`,
      };
    });
  };

  /**
   * 반응 버튼 롱 프레스 종료 핸들러.
   * 롱 프레스 타이머를 중지합니다.
   */
  const endLongPress = () => {
    longPress.end();
  };

  /**
   * 반응 선택 시 이모지가 떠오르는 애니메이션을 생성합니다.
   * (현재는 콘솔 로그로 대체)
   * @param {string} emoji - 띄울 이모지 문자열.
   */
  const createFloatingEmoji = (emoji: string) => {
    // 실제 구현에서는 DOM 요소를 생성하고 CSS 애니메이션을 적용합니다.
    // 예:
    // const emojiEl = document.createElement('div');
    // emojiEl.innerText = emoji;
    // emojiEl.classList.add('floating-emoji');
    // emojiEl.style.left = `${reactionPopupPosition.value.left}`;
    // emojiEl.style.top = `${reactionPopupPosition.value.top}`;
    // document.body.appendChild(emojiEl);
    // emojiEl.addEventListener('animationend', () => {
    //   emojiEl.remove();
    // });
  };

  /**
   * 팝업 외부 클릭 시 팝업을 닫는 핸들러.
   * @param {Event} event - 클릭 이벤트 객체.
   */
  const handleClickOutside = (event: Event) => {
    const target = event.target as HTMLElement;
    // 팝업 또는 반응 버튼 클릭이 아닌 경우 팝업 닫기
    if (showReactionPopup.value && !target.closest('.reaction-popup') && !target.closest('.footer-btn')) {
      showReactionPopup.value = false;
    }
  };

  onMounted(() => {
    loadReactionData();
    document.addEventListener('click', handleClickOutside);
  });

  onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside);
  });

  return {
    reactionMap,
    myReaction,
    reactionUsersMap,
    showReactionPopup,
    reactionPopupPosition,
    hoveredReaction,
    tooltipPosition,
    loadReactionData,
    postReaction,
    handleReactionClick,
    selectReaction,
    startLongPress,
    endLongPress,
    createFloatingEmoji,
  };
}
