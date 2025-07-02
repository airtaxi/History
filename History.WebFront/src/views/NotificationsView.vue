<template>
  <div class="notifications-page">
    <h1>알림</h1>
    <div v-if="isLoading && notifications.length === 0" class="loading">로딩 중...</div>
    <div v-else-if="!isLoading && notifications.length === 0" class="empty">알림이 없습니다.</div>
    <div class="notifications-container" @scroll="handleScroll">
      <ul class="notifications-list">
        <li 
          v-for="noti in notifications" 
          :key="noti.id" 
          class="notification-item"
          @click="goToTarget(noti)"
        >
          <img 
            :src="profileImageMap[noti.user.userId] || '/src/assets/images/default_profile_image.jpg'" 
            class="noti-avatar"
          />
          <div class="noti-info">
            <p v-html="noti.user && noti.user.nickname ? noti.title.replace(noti.user.nickname, `<strong>${noti.user.nickname}</strong>`) : noti.title"></p>
            <span class="noti-time">{{ new Date(noti.createdAt).toLocaleString() }}</span>
            <p v-if="noti.body" class="noti-body">{{ noti.body }}</p>
          </div>
        </li>
      </ul>
      <div v-if="isLoadingMore" class="loading-more">더 불러오는 중...</div>
      <div v-if="!hasMore && notifications.length > 0" class="no-more">모든 알림을 불러왔습니다.</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import type { NotificationResponseDto } from '@/types';
import apiClient from '@/api';
import { useRouter } from 'vue-router';

const notifications = ref<NotificationResponseDto[]>([]);
const isLoading = ref(false);
const isLoadingMore = ref(false);
const hasMore = ref(true);
const lastNotificationId = ref<string | null>(null);
const profileImageMap = ref<Record<string, string>>({});
const router = useRouter();

// 프로필 이미지 Blob URL 생성 함수
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

// 알림 목록의 프로필 이미지 준비
const prepareProfileImages = async (notificationList: NotificationResponseDto[]) => {
  const userIds = new Set<string>();
  
  // 중복되지 않은 사용자 ID 수집
  notificationList.forEach(noti => {
    if (noti.user && noti.user.userId) {
      userIds.add(noti.user.userId);
    }
  });
  
  // 각 사용자의 프로필 이미지 처리
  for (const userId of userIds) {
    // 이미 처리된 사용자는 건너뛰기
    if (profileImageMap.value[userId]) continue;
    
    const user = notificationList.find(n => n.user.userId === userId)?.user;
    if (user?.profileThumbnailMediaId) {
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId);
      profileImageMap.value[userId] = blobUrl || '/src/assets/images/default_profile_image.jpg';
    } else {
      profileImageMap.value[userId] = '/src/assets/images/default_profile_image.jpg';
    }
  }
};

// 알림 목록 가져오기
const fetchNotifications = async (from?: string) => {
  if (isLoading.value || isLoadingMore.value || !hasMore.value) return;
  
  const isInitialLoad = !from;
  if (isInitialLoad) {
    isLoading.value = true;
  } else {
    isLoadingMore.value = true;
  }
  
  try {
    const params: any = { limit: 20 };
    if (from) {
      params.from = from;
    }
    
    const response = await apiClient.get('/api/User/notifications', { params });
    const newNotifications = response.data;
    
    // 디버깅을 위한 알림 데이터 확인
    console.log('받은 알림 데이터:', newNotifications);
    
    if (newNotifications.length === 0) {
      hasMore.value = false;
    } else {
      // 프로필 이미지 준비
      await prepareProfileImages(newNotifications);
      
      if (isInitialLoad) {
        notifications.value = newNotifications;
      } else {
        notifications.value.push(...newNotifications);
      }
      
      // 마지막 알림 ID 저장
      if (newNotifications.length > 0) {
        lastNotificationId.value = newNotifications[newNotifications.length - 1].id;
      }
      
      // 20개 미만이면 더 이상 없는 것으로 간주
      if (newNotifications.length < 20) {
        hasMore.value = false;
      }
    }
  } catch (err) {
    console.error('알림 로딩 실패', err);
  } finally {
    isLoading.value = false;
    isLoadingMore.value = false;
  }
};

// 스크롤 이벤트 처리
const handleScroll = (event: Event) => {
  const container = event.target as HTMLElement;
  const scrollBottom = container.scrollHeight - container.scrollTop - container.clientHeight;
  
  // 스크롤이 바닥에서 100px 이내일 때 추가 로드
  if (scrollBottom < 100 && hasMore.value && !isLoadingMore.value && lastNotificationId.value) {
    fetchNotifications(lastNotificationId.value);
  }
};

onMounted(() => {
  fetchNotifications();
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
  height: calc(100vh - 120px);
  display: flex;
  flex-direction: column;
}

.notifications-page h1 {
  margin-bottom: 20px;
  flex-shrink: 0;
}

.notifications-container {
  flex: 1;
  overflow-y: auto;
  background: white;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.notifications-list {
  list-style: none;
  padding: 0;
  margin: 0;
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

.notification-item:last-child {
  border-bottom: none;
}

.noti-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.noti-info {
  flex: 1;
  min-width: 0;
}

.noti-info p {
  margin: 0 0 6px 0;
  font-size: 0.95rem;
  line-height: 1.4;
  word-break: break-word;
}

.noti-time {
  font-size: 0.8rem;
  color: #888;
}

.noti-body {
  margin: 4px 0 0;
  font-size: 0.9rem;
  color: #666;
  line-height: 1.3;
}

.loading, .empty {
  text-align: center;
  margin: 40px 0;
  color: #888;
}

.loading-more, .no-more {
  text-align: center;
  padding: 20px;
  color: #888;
  font-size: 0.9rem;
}

/* 스크롤바 스타일 */
.notifications-container::-webkit-scrollbar {
  width: 8px;
}

.notifications-container::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 8px;
}

.notifications-container::-webkit-scrollbar-thumb {
  background: #888;
  border-radius: 8px;
}

.notifications-container::-webkit-scrollbar-thumb:hover {
  background: #555;
}

/* 모바일 반응형 */
@media (max-width: 768px) {
  .notifications-page {
    margin: 20px auto;
    padding: 0 16px;
    height: calc(100vh - 80px);
  }
  
  .notification-item {
    padding: 12px;
  }
  
  .noti-avatar {
    width: 40px;
    height: 40px;
  }
}
</style>
