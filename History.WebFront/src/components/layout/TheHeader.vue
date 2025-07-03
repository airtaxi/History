<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, RouterLink } from 'vue-router';
import { useUiStore } from '@/stores/ui';
import type { UserResponseDto, NotificationResponseDto } from '@/types';
import apiClient from '@/api';
import { useAuthStore } from '@/stores/auth';
// import "./TheHeader.css" 전역 css 버그로 인한 주석처리

const uiStore = useUiStore();
const router = useRouter();

const { logout } = useAuthStore();

const goHome = () => {
  if (router.currentRoute.value.path === '/') {
    window.location.reload();
  } else {
    router.push('/');
  }
};

const handleLogout = () => {
  logout(); // 토큰 및 사용자 정보 초기화
  router.push('/login'); // 로그인 페이지로 이동
};

/**
 * 프로필 이미지 Blob URL 생성 함수 (공통 사용)
 */
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

// 검색
const searchQuery = ref('');
const searchResults = ref<UserResponseDto[]>([]);
const isSearchFocused = ref(false);
const searchProfileMap = ref<Record<string, string>>({});
let searchTimeout: number;

/**
 * 검색 결과의 프로필 이미지 준비
 */
const prepareSearchProfileImages = async (userList: UserResponseDto[]) => {
  for (const user of userList) {
    if (searchProfileMap.value[user.userId]) continue;
    
    if (user.profileThumbnailMediaId) {
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId);
      searchProfileMap.value[user.userId] = blobUrl || '/src/assets/images/default_profile_image.jpg';
    } else {
      searchProfileMap.value[user.userId] = '/src/assets/images/default_profile_image.jpg';
    }
  }
};

/**
 * 사용자 검색 입력 처리 함수 (디바운싱 적용)
 * 
 * 이 함수는 사용자가 검색창에 입력할 때마다 호출되며,
 * 300ms 디바운싱을 적용하여 과도한 API 호출을 방지합니다.
 * 
 * 동작 방식:
 * 1. 이전 타이머를 취소
 * 2. 입력값이 비어있으면 검색 결과 초기화
 * 3. 300ms 후 실제 검색 API 호출
 * 
 * @function onSearchInput
 * 
 * @example
 * // 검색 입력창에서 사용
 * <input @input="onSearchInput" v-model="searchQuery" />
 */
const onSearchInput = () => {
  clearTimeout(searchTimeout);
  if (!searchQuery.value.trim()) {
    searchResults.value = [];
    return;
  }
  searchTimeout = window.setTimeout(async () => {
    try {
      const response = await apiClient.get<UserResponseDto[]>(`/api/User/nickname-search/${searchQuery.value}`);
      searchResults.value = response.data;
      
      // 프로필 이미지 준비
      await prepareSearchProfileImages(searchResults.value);
    } catch (error) {
      console.error('Search error:', error);
      searchResults.value = [];
    }
  }, 300);
};

/**
 * 헤더 알림 드롭다운에서 알림 클릭 시 관련 페이지로 이동하는 함수
 * 
 * 이 함수는 NotificationsView.vue의 goToTarget과 동일한 로직을 사용하여
 * 헤더 드롭다운과 알림 페이지 간의 일관성을 보장합니다.
 * 
 * 특별 기능:
 * - 알림 드롭다운 자동 닫기 (showNotifications.value = false)
 * - 상세한 디버깅 로그 출력
 * - 에러 발생 시 안전한 홈페이지 리다이렉트
 * 
 * 지원하는 알림 타입별 라우팅:
 * - 게시글 관련: Comment, CommentMention, CommentLike, Share, Repost, PostReaction, PostMention, FavoriteFriendNewPost, Birthday → /post/{PostId}
 * - 친구 요청: FriendRequest → /user/{UserId}
 * - 제재 알림: Restriction → /settings  
 * - 신고 알림: Report → /post/{PostId}
 * - 기본값: 알림 작성자 프로필 → /user/{userId}
 * 
 * @function goToNotification
 * @param {NotificationResponseDto} noti - 클릭된 알림 객체
 * 
 * @example
 * // 헤더 드롭다운의 알림 아이템 클릭 시
 * <li @click="goToNotification(noti)">...</li>
 */
const goToNotification = (noti: NotificationResponseDto) => {
  showNotifications.value = false; // 드롭다운 닫기
  
  try {
    const { type, data } = noti;
    
    console.log('[헤더 알림 클릭]', { type, data }); // 디버깅 로그
    
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
    console.error('헤더 알림 이동 중 오류:', error);
    // 오류 발생 시 홈으로 이동
    router.push('/');
  }
};

/**
 * 검색 결과에서 사용자를 선택했을 때 해당 사용자 프로필로 이동하는 함수
 * 
 * 이 함수는 검색 상태를 초기화하고 사용자 프로필 페이지로 라우팅합니다.
 * 
 * @function goToUserPage
 * @param {string} userId - 이동할 사용자의 ID
 * 
 * @example
 * // 검색 결과 아이템 클릭 시
 * <div @click="goToUserPage(user.userId)">...</div>
 */
const goToUserPage = (userId: string) => {
  searchQuery.value = '';
  searchResults.value = [];
  isSearchFocused.value = false;
  router.push(`/user/${userId}`);
};

/**
 * 검색창에서 포커스가 해제될 때 검색 결과를 숨기는 함수
 * 
 * 200ms 지연을 두어 사용자가 검색 결과를 클릭할 수 있는 시간을 확보합니다.
 * 
 * @function hideResults
 */
const hideResults = () => {
  setTimeout(() => { isSearchFocused.value = false; }, 200);
};

// 알림
const showNotifications = ref(false);
const notifications = ref<NotificationResponseDto[]>([]);
const isLoadingNotifications = ref(false);
const notificationProfileMap = ref<Record<string, string>>({});

/**
 * 알림 목록의 프로필 이미지 준비
 */
const prepareNotificationProfileImages = async (notificationList: NotificationResponseDto[]) => {
  const userIds = new Set<string>();
  
  notificationList.forEach(noti => {
    if (noti.user && noti.user.userId) {
      userIds.add(noti.user.userId);
    }
  });
  
  for (const userId of userIds) {
    if (notificationProfileMap.value[userId]) continue;
    
    const user = notificationList.find(n => n.user.userId === userId)?.user;
    if (user?.profileThumbnailMediaId) {
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId);
      notificationProfileMap.value[userId] = blobUrl || '/src/assets/images/default_profile_image.jpg';
    } else {
      notificationProfileMap.value[userId] = '/src/assets/images/default_profile_image.jpg';
    }
  }
};

/**
 * 서버에서 알림 목록을 가져오는 함수
 * 
 * 이 함수는 force 파라미터가 true이거나 알림이 없을 때 서버에서 알림을 가져옵니다.
 * 
 * @async
 * @function fetchNotifications
 * @param {boolean} force - 강제로 새로고침할지 여부
 */
const fetchNotifications = async (force = false) => {
  if (!force && notifications.value.length > 0) return;
  isLoadingNotifications.value = true;
  try {
    const response = await apiClient.get<NotificationResponseDto[]>('/api/User/notifications', {
      params: { limit: 20 } // 헤더 드롭다운용으로 최대 20개까지 가져오기
    });
    notifications.value = response.data;
    
    // 프로필 이미지 준비
    await prepareNotificationProfileImages(notifications.value);
  } catch (error) {
    console.error("알림 로딩 실패:", error);
  } finally {
    isLoadingNotifications.value = false;
  }
};

/**
 * 알림 드롭다운을 토글하고 필요시 알림을 로드하는 함수
 * 
 * @function toggleNotifications
 */
const toggleNotifications = () => {
  showNotifications.value = !showNotifications.value;
  if (showNotifications.value) {
    // 드롭다운을 열 때마다 최신 알림을 가져오도록 force=true 설정
    fetchNotifications(true);
  }
};
</script>

<template>
  <header class="main-header">
    <div class="header-content">
      <RouterLink to="/" class="header-logo-link" @click.prevent="goHome">
        <img src="@/assets/images/icon_nobg_white.png" alt="History 로고" class="header-logo-image">
        <span class="header-title">History</span>
      </RouterLink>

      <div class="search-container">
        <div class="search-bar-wrapper">
          <input 
            type="text" 
            v-model="searchQuery" 
            @input="onSearchInput" 
            @focus="isSearchFocused = true" 
            @blur="hideResults"
            placeholder="친구, 채널, 태그, 장소 검색" 
            class="search-input">
          <div class="search-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" d="m19.6 21l-6.3-6.3q-.75.6-1.725.95T9.5 16q-2.725 0-4.612-1.888T3 9.5q0-2.725 1.888-4.612T9.5 3q2.725 0 4.612 1.888T16 9.5q0 1.1-.35 2.075T14.7 13.3l6.3 6.3zM9.5 14q1.875 0 3.188-1.313T14 9.5q0-1.875-1.313-3.188T9.5 5Q7.625 5 6.312 6.313T5 9.5q0 1.875 1.313 3.188T9.5 14"/></svg>
          </div>
        </div>
        <div v-if="isSearchFocused && searchQuery" class="search-results-dropdown">
          <div v-if="!searchResults || searchResults.length === 0" class="no-results">검색 결과가 없습니다.</div>
          <div v-else v-for="user in searchResults" :key="user.userId" @click="goToUserPage(user.userId)" class="search-result-item">
            <img :src="searchProfileMap[user.userId] || '/src/assets/images/default_profile_image.jpg'" class="result-avatar">
            <div class="result-info">
              <div class="result-name">{{ user.nickname }}</div>
              <div class="result-handle">@{{ user.handle }}</div>
            </div>
          </div>
        </div>
      </div>
      
      <div class="user-actions">
        <RouterLink to="/user-settings" class="action-btn header" title="설정">
          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
            <path fill="currentColor" d="M12 1a2 2 0 0 1 2 2v1.26a8.07 8.07 0 0 1 2.53.9l.89-.89a2 2 0 1 1 2.83 2.83l-.89.89a8.07 8.07 0 0 1 .9 2.53H21a2 2 0 1 1 0 4h-1.26a8.07 8.07 0 0 1-.9 2.53l.89.89a2 2 0 1 1-2.83 2.83l-.89-.89a8.07 8.07 0 0 1-2.53.9V21a2 2 0 1 1-4 0v-1.26a8.07 8.07 0 0 1-2.53-.9l-.89.89a2 2 0 1 1-2.83-2.83l.89-.89a8.07 8.07 0 0 1-.9-2.53H3a2 2 0 1 1 0-4h1.26a8.07 8.07 0 0 1 .9-2.53l-.89-.89a2 2 0 1 1 2.83-2.83l.89.89a8.07 8.07 0 0 1 2.53-.9V3a2 2 0 0 1 2-2z"/>
          </svg>
        </RouterLink>
        <button class="action-btn header" @click="uiStore.openPostEditor" title="글쓰기">
          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="m19.3 8.925l-4.25-4.2l1.4-1.4q.575-.575 1.413-.575t1.412.575l1.425 1.425q.575.575.575 1.413t-.575 1.412zm-5.725 5.725L3 21v-4.25l10.6-10.6z"/></svg>
        </button>
        <button class="action-btn header" @click="handleLogout" title="로그아웃">
          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
            <path fill="currentColor" d="M16 13v-2H7V8l-5 4l5 4v-3zm6-10H12v2h10v16H12v2h10q.825 0 1.413-.588T22 20V4q0-.825-.588-1.413T20 2z"/>
          </svg>
        </button>
        <div class="notification-wrapper">
          <button class="action-btn header" @click="toggleNotifications" title="알림">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 21q-1.875 0-3.35-1.213t-1.8-2.912q.975.35 1.95.525T10.8 17q2.15 0 3.85-1.1T16.5 12.5q0-.425-.062-.837T16.3 11q1.15.825 1.8 2.063T18.5 15.5q0 2.225-1.488 4.013T12 21m-1-4q-1.25 0-2.125-.875T8 14q0-1.25.875-2.125T11 11q1.25 0 2.125.875T14 14q0 1.25-.875 2.125T11 17M6 13q-.425 0-.838-.062T4.3 12.8q-.825-1.15-1.062-2.587T3.5 7.5q0-3.325 2.338-5.663T11.5 0q1.925 0 3.588.9t2.587 2.45q-.65-.3-1.375-.425T15 2.8q-2.575 0-4.438 1.863T8.7 9.1q0 .425.063.85T8.9 10.8q-1.125-.8-1.8-2.05t-.65-2.625q0-.425.038-.85t.112-.8q-1.325.8-2.137 2.175T4 9.3q0 .775.175 1.513T4.8 12.2q.55.725 1.2 1.225T7.35 14q-.65.025-1.25-.137T5 13.45V13z"/></svg>
          </button>
          <div v-if="showNotifications" class="notification-dropdown" @click.stop>
            <div class="notification-header">최근 알림</div>
            <div v-if="isLoadingNotifications" class="loading-item">로딩 중...</div>
            <div v-else-if="!notifications || notifications.length === 0" class="no-results">새로운 알림이 없습니다.</div>
            <ul v-else class="notification-list">
              <li 
                v-for="noti in notifications" 
                :key="noti.id" 
                class="notification-item"
                @click="goToNotification(noti)"
              >
                  <img :src="notificationProfileMap[noti.user.userId] || '/src/assets/images/default_profile_image.jpg'" class="result-avatar">
                  <div class="notification-content">
                    <p class="notification-title" v-html="noti.title.replace(noti.user.nickname, `<strong>${noti.user.nickname}</strong>`)"></p>
                    <span class="notification-time">{{ new Date(noti.createdAt).toLocaleTimeString() }}</span>
                  </div>
              </li>
            </ul>
            <RouterLink to="/notifications" class="view-all-notifications" @click="showNotifications = false">
              전체 알림 보기 →
            </RouterLink>
          </div>
        </div>
      </div>
    </div>
  </header>
</template>

<style scoped>
.main-header { 
    background-color: #ed664d; 
    padding: 0 24px; 
    height: 64px; 
    display: flex; 
    align-items: center; 
    position: sticky; 
    top: 0; 
    z-index: 100; 
    box-shadow: 0 1px 3px rgba(0,0,0,0.1); 
}

.header-content { 
    width: 100%; 
    display: flex; 
    align-items: center; 
    justify-content: space-between; 
}

.header-logo-link { 
    display: flex; 
    align-items: center; 
    gap: 10px; 
    text-decoration: none; 
}

.header-logo-image {
     width: 36px; 
     height: 36px; 
}

.header-title { 
    color: white; 
    font-size: 1.6rem; 
    font-weight: 700; 
}

.search-container { 
    position: relative; 
    width: 100%; 
    max-width: 500px; 
    margin: 0 auto; 
}

.search-bar-wrapper { 
    position: relative; 
}

.search-input { 
    width: 100%; 
    height: 40px; 
    border-radius: 20px; 
    border: none; 
    padding: 0 20px 0 45px; 
    background-color: rgba(255, 255, 255, 0.2); 
    color: white; 
    font-size: 0.95rem; 
}

.search-input::placeholder { 
    color: rgba(255, 255, 255, 0.7); 
}

.search-icon { 
    position: absolute; 
    left: 15px; 
    top: 50%; 
    transform: translateY(-50%); 
    color: rgba(255,255,255,0.7); 
    display: flex; 
}

.user-actions { 
    display: flex; 
    align-items: center; 
    gap: 8px; 
}

.action-btn { 
    background: none; 
    border: none; 
    color: white; 
    width: 40px; 
    height: 40px; 
    border-radius: 50%; 
    display: flex; 
    align-items: center; 
    justify-content: center; 
    cursor: pointer; 
    transition: background-color 0.2s; 
}

.action-btn:hover header { 
    background-color: rgba(255, 255, 255, 0.15); 
}

.action-btn svg  { 
    width: 24px; 
    height: 24px; 
}

.search-results-dropdown { 
    position: absolute; 
    top: calc(100% + 5px); 
    left: 0; right: 0; 
    background: white; 
    border-radius: 8px; 
    box-shadow: 0 4px 12px rgba(0,0,0,0.1); 
    max-height: 300px; 
    overflow-y: auto; 
    z-index: 101; 
    border: 1px solid #ddd; 
}

.search-result-item { 
    display: flex; 
    align-items: center; 
    padding: 10px 15px; 
    cursor: pointer; 
    transition: background-color 0.2s; 
}

.search-result-item:hover { 
    background-color: #f0f2f5; 
}

.result-avatar { 
    width: 36px; 
    height: 36px; 
    border-radius: 50%; 
    margin-right: 12px; 
}

.result-name { 
    font-weight: 600; 
}

.result-handle { 
    font-size: 0.85rem; 
    color: #666; 
}

.no-results, .loading-item { 
    padding: 20px; 
    text-align: center; 
    color: #888; 
}

.notification-wrapper { 
    position: relative; 
}

.notification-dropdown { 
  position: absolute; 
  top: 55px; 
  right: 0; 
  width: 380px; 
  background: white; 
  border-radius: 8px; 
  box-shadow: 0 5px 15px rgba(0,0,0,0.2); 
  border: 1px solid #ddd; 
  padding: 0; 
  max-height: 520px; /* 전체 드롭다운 최대 높이 설정 */
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.notification-header { 
  font-weight: 600; 
  padding: 12px 16px; 
  border-bottom: 1px solid #eee; 
  flex-shrink: 0; /* 헤더는 고정 */
  background: white;
  position: sticky;
  top: 0;
  z-index: 1;
}

.notification-list { 
  list-style: none; 
  margin: 0; 
  padding: 0; 
  max-height: 440px; /* 헤더를 제외한 리스트 높이 */
  overflow-y: auto; 
  overflow-x: hidden;
  flex: 1;
}

/* 스크롤바 스타일링 */
.notification-list::-webkit-scrollbar {
  width: 6px;
}

.notification-list::-webkit-scrollbar-track {
  background: #f1f1f1;
}

.notification-list::-webkit-scrollbar-thumb {
  background: #c0c0c0;
  border-radius: 3px;
}

.notification-list::-webkit-scrollbar-thumb:hover {
  background: #888;
}

.notification-item { 
  display: flex; 
  padding: 12px 16px; 
  border-bottom: 1px solid #eee; 
  gap: 12px; 
  cursor: pointer;
  transition: background-color 0.2s;
}

.notification-item:hover {
  background-color: #f8f9fa;
}

.notification-item:last-child { 
  border-bottom: none; 
}

.notification-content { 
  flex: 1; 
  min-width: 0; /* 긴 텍스트 오버플로우 방지 */
}

.notification-title { 
  margin: 0 0 4px 0; 
  font-size: 0.9rem; 
  line-height: 1.4; 
  word-break: break-word; /* 긴 단어 줄바꿈 */
}

.notification-time { 
  font-size: 0.75rem; 
  color: #888; 
}

/* 로딩 및 빈 상태 */
.loading-item, .no-results {
  padding: 40px 20px;
  text-align: center;
  color: #888;
}

/* 알림이 많을 때 하단 여백 */
.notification-list:not(:empty) {
  padding-bottom: 8px;
}

/* 전체 알림 보기 링크 */
.view-all-notifications {
  display: block;
  text-align: center;
  padding: 12px 16px;
  color: #ed664d;
  text-decoration: none;
  font-size: 0.9rem;
  font-weight: 500;
  border-top: 1px solid #eee;
  background: #fafafa;
  transition: all 0.2s;
}

.view-all-notifications:hover {
  background: #f0f0f0;
  color: #d54e37;
}

/* 모바일 반응형 */
@media (max-width: 768px) {
  .notification-dropdown {
    width: 320px;
    right: -10px;
    max-height: 70vh;
  }
  .notification-list {
    max-height: calc(70vh - 80px);
  }
}
</style>