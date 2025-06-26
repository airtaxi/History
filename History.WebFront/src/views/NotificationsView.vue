<template>
    <div class="notifications-page">
      <h1>알림</h1>
      <div v-if="isLoading" class="loading">로딩 중...</div>
      <div v-else-if="notifications.length === 0" class="empty">알림이 없습니다.</div>
      <ul class="notifications-list">
        <li 
          v-for="noti in notifications" 
          :key="noti.id" 
          class="notification-item"
          @click="goToTarget(noti)"
        >
          <img 
            :src="noti.user.profileThumbnailMediaId ? `/api/Media/${noti.user.profileThumbnailMediaId}` : '/src/assets/images/default_profile_image.jpg'" 
            class="noti-avatar"
          />
          <div class="noti-info">
            <p v-html="noti.title.replace(noti.user.nickname, `<strong>${noti.user.nickname}</strong>`)"></p>
            <span class="noti-time">{{ new Date(noti.createdAt).toLocaleString() }}</span>
          </div>
        </li>
      </ul>
    </div>
  </template>
  
  <script setup lang="ts">
  import { ref, onMounted, onUnmounted } from 'vue';
  import type { NotificationResponseDto } from '@/types';
  import apiClient from '@/api';
  import { useRouter } from 'vue-router';
  
  const notifications = ref<NotificationResponseDto[]>([]);
  const isLoading = ref(false);
  const router = useRouter();
  const from = ref(0);
  const limit = 10;
  const hasMore = ref(true);
  const isFetchingMore = ref(false);

  const handleScroll = () => {
  const scrollPosition = window.innerHeight + window.scrollY;
  const threshold = document.body.offsetHeight - 100;

  if (scrollPosition >= threshold) {
    loadNotifications();
  }
};


  const loadNotifications = () => {
    if (!hasMore.value || isFetchingMore.value) return;

    isFetchingMore.value = true;
    apiClient.get(`/api/User/notifications?limit=${limit}&from=${from.value}`)
      .then(res => {
        console.log('[응답 받은 알림 목록]', res.data); 
        console.log('[응답 받은 개수]', res.data.length);

        if (res.data.length < limit) {
          hasMore.value = false;
        }
        notifications.value.push(...res.data);
        from.value += res.data.length;

        
      })
      .catch(err => {
        console.error('알림 로딩 실패', err);
      })
      .finally(() => {
        isFetchingMore.value = false;
        isLoading.value = false;
      });
  };

  onMounted(() => {
    isLoading.value = true;
    loadNotifications();
    window.addEventListener('scroll', handleScroll);
  });

  onUnmounted(() => {
    window.removeEventListener('scroll', handleScroll);
  });

  
  /**
   * 알림 클릭 시 관련 페이지로 이동하는 딥링킹 함수
   * 
   * 이 함수는 알림의 타입과 데이터를 분석하여 사용자를 적절한 페이지로 라우팅합니다.
   * 백엔드에서 제공하는 알림 데이터의 type과 data 필드를 기반으로 동작합니다.
   * 
   * 지원하는 알림 타입별 라우팅:
   * - 게시글 관련: Comment, CommentMention, CommentLike, Share, Repost, PostReaction, PostMention, FavoriteFriendNewPost, Birthday → /post/{PostId}
   * - 친구 요청: FriendRequest → /user/{UserId}  
   * - 제재 알림: Restriction → /settings
   * - 신고 알림: Report → /post/{PostId}
   * - 기본값: 알림 작성자 프로필 → /user/{userId}
   * 
   * @function goToTarget
   * @param {NotificationResponseDto} noti - 클릭된 알림 객체
   * @param {string} noti.type - 알림 타입 (Comment, FriendRequest 등)
   * @param {Object} noti.data - 라우팅에 필요한 메타데이터 (PostId, UserId 등)
   * @param {Object} noti.user - 알림을 발생시킨 사용자 정보
   * 
   * @example
   * // 댓글 알림 클릭 시
   * goToTarget({
   *   type: 'Comment',
   *   data: { PostId: 'abc123' },
   *   user: { userId: 'user456' }
   * })
   * // 결과: /post/abc123으로 이동
   * 
   * @example  
   * // 친구 요청 알림 클릭 시
   * goToTarget({
   *   type: 'FriendRequest', 
   *   data: { UserId: 'user789' },
   *   user: { userId: 'user789' }
   * })
   * // 결과: /user/user789로 이동
   */
  const goToTarget = (noti: NotificationResponseDto) => {
    try {
      const { type, data } = noti;
      
      console.log('[알림 페이지 클릭]', { type, data }); // 디버깅 로그
      
      // 게시글 관련 알림들 - PostDetailView로 이동
      if (['Comment', 'CommentMention', 'CommentLike', 'Share', 'Repost', 'PostReaction', 'PostMention', 'FavoriteFriendNewPost', 'Birthday'].includes(type)) {
        if (data.PostId) {
          console.log(`[게시글로 이동] /post/${data.PostId}`);
          router.push(`/post/${data.PostId}`);
          return;
        }
      }
      
      // 친구 요청 알림 - UserProfileView로 이동
      if (type === 'FriendRequest') {
        if (data.UserId) {
          console.log(`[유저 프로필로 이동] /user/${data.UserId}`);
          router.push(`/user/${data.UserId}`);
          return;
        }
      }
      
      // 제재 관련 알림 - 설정 페이지로 이동
      if (type === 'Restriction') {
        console.log('[설정 페이지로 이동] /settings');
        router.push('/settings');
        return;
      }
      
      // 신고 관련 알림 (관리자용) - 게시글이나 댓글로 이동
      if (type === 'Report') {
        if (data.PostId) {
          console.log(`[신고 게시글로 이동] /post/${data.PostId}`);
          router.push(`/post/${data.PostId}`);
          return;
        }
      }
      
      // 기본값: 사용자 프로필로 이동
      if (noti.user.userId) {
        console.log(`[기본: 유저 프로필로 이동] /user/${noti.user.userId}`);
        router.push(`/user/${noti.user.userId}`);
      } else {
        console.warn('알림 데이터가 부족합니다:', noti);
      }
      
    } catch (error) {
      console.error('알림 이동 중 오류:', error);
      // 오류 발생 시 홈으로 이동
      router.push('/');
    }
  };
  </script>
  
  <style scoped>
  .notifications-page {
    max-width: 640px;
    margin: 40px auto;
    padding: 0 20px;
  }
  .notifications-list {
    list-style: none;
    padding: 0;
  }
  .notification-item {
    display: flex;
    gap: 12px;
    padding: 16px;
    border-bottom: 1px solid #eee;
    cursor: pointer;
    transition: background-color 0.2s;
  }
  .notification-item:hover {
    background-color: #f8f9fa;
  }
  .noti-avatar {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    object-fit: cover;
  }
  .noti-info {
    flex: 1;
  }
  .noti-info p {
    margin: 0 0 6px 0;
    font-size: 0.95rem;
    line-height: 1.4;
  }
  .noti-time {
    font-size: 0.8rem;
    color: #888;
  }
  .loading, .empty {
    text-align: center;
    margin: 40px 0;
    color: #888;
  }
  </style>
  