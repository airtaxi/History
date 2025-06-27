<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import apiClient from '@/api';
import type { UserResponseDto, PostResponseDto } from '@/types';
import TheHeader from '@/components/layout/TheHeader.vue';
import ProfileEditView from '@/views/accounts/ProfileEditView.vue';
import PostCard from '@/components/PostCard.vue';
import { watchEffect } from 'vue'
import { watch } from 'vue';

const route = useRoute();
const routeUserId = computed(() => route.params.userId);
const me = ref<UserResponseDto | null>(null);
const user = ref<UserResponseDto | null>(null);
const posts = ref<PostResponseDto[]>([]);
const postCount = ref(0);
const friendCount = ref(0);
const isLoading = ref(true);
const isLoadingMore = ref(false);
const noMorePosts = ref(false);
const isEditModalOpen = ref(false);
const profileImageUrl = ref('');
const profileImageMap = ref<Record<string, string>>({});
const backgroundImageUrl = ref('');
const isMyProfile = computed(() => me.value?.userId === user.value?.userId);
const loadMoreSentinel = ref<HTMLElement | null>(null);
let observer: IntersectionObserver;

// 친구 관계 상태 관련 변수들
const isFriend = ref(false);
const hasPendingRequestFromUser = ref(false);
const hasPendingRequestToUser = ref(false);
const isBlocked = ref(false);
const isFavorite = ref(false);
const isIgnored = ref(false);

const getMediaBlobUrl = async (mediaId: string | null | undefined) => {
  if (!mediaId) return '';
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

const prepareProfileImageMap = async (postList: PostResponseDto[]) => {
  const map: Record<string, string> = {};
  const userIds = new Set<string>();
  
  // 게시글 작성자들의 ID 수집
  postList.forEach(p => {
    userIds.add(p.user.userId);
    
    // 리포스트인 경우 원본 게시글 작성자도 추가
    if ((p as any).isRepost && (p as any).parentPost?.user) {
      userIds.add((p as any).parentPost.user.userId);
    }
  });
  
  // 각 사용자의 프로필 이미지 처리
  for (const uid of userIds) {
    // 이미 처리된 사용자는 건너뛰기
    if (profileImageMap.value[uid]) continue;
    
    // 일반 게시글에서 사용자 찾기
    let user = postList.find(p => p.user.userId === uid)?.user;
    
    // 리포스트 원본에서 사용자 찾기
    if (!user) {
      for (const post of postList) {
        if ((post as any).isRepost && (post as any).parentPost?.user?.userId === uid) {
          user = (post as any).parentPost.user;
          break;
        }
      }
    }
    
    if (user?.profileThumbnailMediaId) {
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId);
      map[uid] = blobUrl || '/src/assets/images/default_profile_image.jpg';
    }
  }
  
  profileImageMap.value = { ...profileImageMap.value, ...map };
};

const sendFriendRequest = async () => {
  try {
    await apiClient.post(`/api/Friendship/request/${user.value?.userId}`);
    alert('친구 요청을 보냈습니다.');
    hasPendingRequestToUser.value = true;
  } catch (e) {
    console.error('친구 요청 실패:', e);
    alert('친구 요청에 실패했습니다.');
  }
};

// 친구 관계 상태 확인 함수
const checkFriendshipStatus = async () => {
  if (!user.value || !me.value || isMyProfile.value) return;
  
  try {
    // 병렬로 모든 상태 확인
    const [friendsRes, pendingRes, waitingRes, blockedRes, ignoredRes] = await Promise.all([
      apiClient.get<UserResponseDto[]>(`/api/Friendship/${me.value.userId}`),
      apiClient.get<UserResponseDto[]>('/api/Friendship/pending'),
      apiClient.get<UserResponseDto[]>('/api/Friendship/waiting'),
      apiClient.get<UserResponseDto[]>('/api/Friendship/blocked'),
      apiClient.get<UserResponseDto[]>('/api/Friendship/ignored')
    ]);
    
    isFriend.value = friendsRes.data.some(f => f.userId === user.value!.userId);
    hasPendingRequestFromUser.value = pendingRes.data.some(r => r.userId === user.value!.userId);
    hasPendingRequestToUser.value = waitingRes.data.some(r => r.userId === user.value!.userId);
    isBlocked.value = blockedRes.data.some(b => b.userId === user.value!.userId);
    isIgnored.value = ignoredRes.data.some(i => i.userId === user.value!.userId);
  } catch (e) {
    console.error('친구 관계 상태 확인 실패:', e);
  }
};

// 친구 요청 수락
const acceptFriendRequest = async () => {
  try {
    await apiClient.post(`/api/Friendship/request/${user.value?.userId}/accept`);
    alert('친구 요청을 수락했습니다.');
    hasPendingRequestFromUser.value = false;
    isFriend.value = true;
    friendCount.value += 1;
  } catch (e) {
    console.error('친구 요청 수락 실패:', e);
    alert('친구 요청 수락에 실패했습니다.');
  }
};

// 친구 요청 거절
const rejectFriendRequest = async () => {
  try {
    await apiClient.post(`/api/Friendship/request/${user.value?.userId}/decline`);
    alert('친구 요청을 거절했습니다.');
    hasPendingRequestFromUser.value = false;
  } catch (e) {
    console.error('친구 요청 거절 실패:', e);
    alert('친구 요청 거절에 실패했습니다.');
  }
};

// 친구 요청 무시
const ignoreFriendRequest = async () => {
  try {
    await apiClient.post(`/api/Friendship/ignore/${user.value?.userId}`);
    alert('친구 요청을 무시했습니다.');
    hasPendingRequestFromUser.value = false;
    isIgnored.value = true;
  } catch (e) {
    console.error('친구 요청 무시 실패:', e);
    alert('친구 요청 무시에 실패했습니다.');
  }
};

// 친구 삭제
const removeFriend = async () => {
  if (!confirm('정말로 친구를 삭제하시겠습니까?')) return;
  
  try {
    await apiClient.post(`/api/Friendship/remove/${user.value?.userId}`);
    alert('친구를 삭제했습니다.');
    isFriend.value = false;
    friendCount.value -= 1;
  } catch (e) {
    console.error('친구 삭제 실패:', e);
    alert('친구 삭제에 실패했습니다.');
  }
};

// 차단
const blockUser = async () => {
  if (!confirm('정말로 이 사용자를 차단하시겠습니까?')) return;
  
  if (!user.value?.userId) {
    alert('차단할 사용자 정보를 찾을 수 없습니다.');
    return;
  }
  
  try {
    await apiClient.post(`/api/Friendship/block/${user.value.userId}`);
    alert('사용자를 차단했습니다.');
    isBlocked.value = true;
    if (isFriend.value) {
      isFriend.value = false;
      friendCount.value -= 1;
    }
  } catch (e: any) {
    console.error('사용자 차단 실패:', e);
    const errorMessage = e.response?.data || '사용자 차단에 실패했습니다.';
    alert(errorMessage);
  }
};

// 차단 해제
const unblockUser = async () => {
  if (!user.value?.userId) {
    alert('차단 해제할 사용자 정보를 찾을 수 없습니다.');
    return;
  }
  
  try {
    await apiClient.delete(`/api/Friendship/block/${user.value.userId}`);
    alert('차단을 해제했습니다.');
    isBlocked.value = false;
  } catch (e: any) {
    console.error('차단 해제 실패:', e);
    const errorMessage = e.response?.data || '차단 해제에 실패했습니다.';
    alert(errorMessage);
  }
};

// 즐겨찾기 등록/해제
const toggleFavorite = async () => {
  try {
    await apiClient.post(`/api/Friendship/toggle-favorite/${user.value?.userId}`);
    isFavorite.value = !isFavorite.value;
    alert(isFavorite.value ? '즐겨찾기에 등록했습니다.' : '즐겨찾기를 해제했습니다.');
  } catch (e) {
    console.error('즐겨찾기 토글 실패:', e);
    alert('즐겨찾기 설정에 실패했습니다.');
  }
};

// 친구 요청 취소
const cancelFriendRequest = async () => {
  try {
    await apiClient.post(`/api/Friendship/request/${user.value?.userId}/cancel`);
    alert('친구 요청을 취소했습니다.');
    hasPendingRequestToUser.value = false;
  } catch (e) {
    console.error('친구 요청 취소 실패:', e);
    alert('친구 요청 취소에 실패했습니다.');
  }
};

// 무시 해제
const unignoreUser = async () => {
  try {
    await apiClient.delete(`/api/Friendship/ignore/${user.value?.userId}`);
    alert('무시를 해제했습니다.');
    isIgnored.value = false;
  } catch (e) {
    console.error('무시 해제 실패:', e);
    alert('무시 해제에 실패했습니다.');
  }
};

const fetchInitialData = async () => {
  isLoading.value = true;
  try {
    const [meRes, userRes, postCountRes, friendRes] = await Promise.all([
      apiClient.get<UserResponseDto>('/api/User/me'),
      apiClient.get<UserResponseDto>(`/api/User/${routeUserId.value}`),
      apiClient.get<{ count: number }>(`/api/Post/user/${routeUserId.value}/count`),
      apiClient.get<UserResponseDto[]>(`/api/Friendship/${routeUserId.value}`),
    ]);
    me.value = meRes.data;
    user.value = userRes.data;
    postCount.value = postCountRes.data.count;
    friendCount.value = friendRes.data.length;

    profileImageUrl.value = await getMediaBlobUrl(user.value.profileThumbnailMediaId);
    backgroundImageUrl.value = await getMediaBlobUrl(user.value.backgroundThumbnailMediaId);

    // 친구 관계 상태 확인
    await checkFriendshipStatus();

    await loadMorePosts(); // 첫 페이지 로딩
  } catch (e) {
    console.error('프로필 초기 로딩 실패:', e);
  } finally {
    isLoading.value = false;
  }
};

const loadMorePosts = async () => {
  if (isLoadingMore.value || noMorePosts.value) return;

  isLoadingMore.value = true;
  try {
    const lastPost = posts.value[posts.value.length - 1];
    const params = lastPost ? { from: lastPost.id } : {};
    const res = await apiClient.get<PostResponseDto[]>(`/api/Post/user/${routeUserId.value}`, { params });
    
    console.log('🔍 UserProfile API 응답:', res.data);
    
    // 받은 게시글 처리
    const allPosts: any[] = [];
    
    for (const post of res.data as any[]) {
      // 1. 원본 게시글 추가 (내가 작성한 것만)
      if (!post.isRepost && post.user.userId === routeUserId.value) {
        allPosts.push(post);
      }
      
      // 2. sharedAndRepostedUsers에서 현재 사용자의 리포스트 찾기
      if (post.sharedAndRepostedUsers && Array.isArray(post.sharedAndRepostedUsers)) {
        const myReposts = post.sharedAndRepostedUsers.filter(
          (item: any) => item.isRepost && item.user.userId === routeUserId.value
        );
        
        // 각 리포스트를 별도 게시글로 변환
        for (const repostInfo of myReposts) {
          try {
            // 리포스트의 상세 정보 가져오기 (contents 포함)
            const repostDetailRes = await apiClient.get(`/api/Post/${repostInfo.postId}`);
            const repostDetail = repostDetailRes.data;
            
            // 리포스트 게시글 생성 (상세 정보 포함)
            const repostPost = {
              ...repostDetail,
              isRepost: true,
              parentPost: post // 원본 게시글 객체로 교체
            };
            
            allPosts.push(repostPost);
          } catch (e) {
            console.warn(`리포스트 ${repostInfo.postId} 상세 정보 로드 실패:`, e);
            // 실패 시 기본 정보만으로 생성
            const repostPost = {
              id: repostInfo.postId,
              user: repostInfo.user,
              isRepost: true,
              parentPost: post,
              contents: [],
              createdAt: repostInfo.sharedAt,
              discoveryOption: post.discoveryOption,
              postReactions: [],
              comments: [],
              commentsCount: 0
            };
            allPosts.push(repostPost);
          }
        }
      }
    }
    
    // 시간순 정렬 (최신순)
    allPosts.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    
    // 테스트코드 제거하기!!!!!!!
    
    console.log('🔍 처리된 게시글 (리포스트 포함):', allPosts);

    if (allPosts.length === 0) {
      noMorePosts.value = true;
    } else {
      posts.value.push(...allPosts);
      await prepareProfileImageMap(allPosts);
    }
  } catch (e) {
    console.error('추가 게시글 로딩 실패:', e);
  } finally {
    isLoadingMore.value = false;
  }
};

const handleProfileUpdated = () => {
  isEditModalOpen.value = false;
  posts.value = [];
  profileImageMap.value = {};
  noMorePosts.value = false;
  // 친구 관계 상태 초기화
  isFriend.value = false;
  hasPendingRequestFromUser.value = false;
  hasPendingRequestToUser.value = false;
  isBlocked.value = false;
  isFavorite.value = false;
  isIgnored.value = false;
  fetchInitialData();
};

onMounted(() => {
  fetchInitialData();

  observer = new IntersectionObserver((entries) => {
    if (entries[0].isIntersecting) loadMorePosts();
  }, { rootMargin: '200px' });

  watchEffect(() => {
    if (loadMoreSentinel.value) {
      observer.observe(loadMoreSentinel.value);
    }
  });
});

onUnmounted(() => {
  observer?.disconnect();
});

watch(routeUserId, () => {
  posts.value = [];
  profileImageMap.value = {};
  noMorePosts.value = false;
  // 친구 관계 상태 초기화
  isFriend.value = false;
  hasPendingRequestFromUser.value = false;
  hasPendingRequestToUser.value = false;
  isBlocked.value = false;
  isFavorite.value = false;
  isIgnored.value = false;
  fetchInitialData();
});
</script>

<template>
  <div class="page-container">
    <TheHeader />

    <main v-if="isLoading" class="loading-content">
      <div class="spinner"></div>
    </main>

    <main v-else-if="user" class="profile-content">
      <div class="profile-page">
        <div class="profile-header">
          <div class="background-image-wrapper">
            <img :src="backgroundImageUrl || '/src/assets/images/default_background.jpg'" class="background-image" />
          </div>
          <div class="profile-info-bar">
            <div class="profile-avatar-wrapper">
              <img :src="profileImageUrl || '/src/assets/images/default_profile_image.jpg'" class="profile-avatar" />
            </div>
            <div class="profile-actions">
              <button v-if="isMyProfile" @click="isEditModalOpen = true" class="edit-profile-btn">프로필 수정</button>
              
              <!-- 친구 요청을 받은 상황 -->
              <template v-else-if="hasPendingRequestFromUser">
                <button @click="acceptFriendRequest" class="action-btn primary">친구 요청 수락</button>
                <button @click="rejectFriendRequest" class="action-btn secondary">거절</button>
                <button @click="ignoreFriendRequest" class="action-btn secondary">무시</button>
              </template>
              
              <!-- 무시한 상황 -->
              <template v-else-if="isIgnored">
                <button @click="unignoreUser" class="action-btn secondary">무시 해제</button>
              </template>
              
              <!-- 이미 친구인 상황 -->
              <template v-else-if="isFriend">
                <button @click="removeFriend" class="action-btn secondary">친구 삭제</button>
                <button @click="blockUser" class="action-btn danger">차단</button>
                <button @click="toggleFavorite" class="action-btn secondary">
                  {{ isFavorite ? '즐겨찾기 해제' : '즐겨찾기 등록' }}
                </button>
              </template>
              
              <!-- 차단한 상황 -->
              <template v-else-if="isBlocked">
                <button @click="unblockUser" class="action-btn secondary">차단 해제</button>
              </template>
              
              <!-- 친구 요청을 보낸 상황 -->
              <template v-else-if="hasPendingRequestToUser">
                <button @click="cancelFriendRequest" class="action-btn secondary">친구 요청 취소</button>
                <button @click="blockUser" class="action-btn danger">차단</button>
              </template>
              
              <!-- 친구가 아닌 상황 -->
              <template v-else>
                <button @click="sendFriendRequest" class="action-btn primary">친구 요청</button>
                <button @click="blockUser" class="action-btn danger">차단</button>
              </template>
            </div>
          </div>
        </div>

        <div class="profile-details">
          <h1 class="nickname">{{ user.nickname }}</h1>
          <p class="handle">@{{ user.handle }}</p>
          <p class="description">{{ user.description || '한 줄 소개가 없습니다.' }}</p>
          <div class="stats-container">
            <div class="stat">
              <span class="stat-value">{{ postCount }}</span>
              <span class="stat-label">게시물</span>
            </div>
            <div class="stat">
              <span class="stat-value">{{ friendCount }}</span>
              <span class="stat-label">친구</span>
            </div>
          </div>
        </div>

        <div class="content-tabs">
          <div class="tab active">글 목록</div>
        </div>

        <div class="my-posts-list">
          <PostCard
            v-for="post in posts"
            :key="post.id"
            :post="post"
            :profile-image-map="profileImageMap"
          />
          <div ref="loadMoreSentinel" class="sentinel"></div>
          <div v-if="isLoadingMore" class="spinner small-spinner"></div>
          <p v-if="noMorePosts" class="end-of-feed">모든 글을 불러왔습니다.</p>
        </div>
      </div>
    </main>

    <ProfileEditView v-if="isEditModalOpen" @close="isEditModalOpen = false" @profile-updated="handleProfileUpdated" />
  </div>

</template>

<style scoped>
header {
flex-shrink: 0;
height: auto; 
}

.page-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
}
.loading-content {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-grow: 1;
}
.spinner {
  border: 5px solid #f3f3f3;
  border-top: 5px solid #ed664d;
  border-radius: 50%;
  width: 50px;
  height: 50px;
  animation: spin 1s linear infinite;
}
@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}
.profile-content {
  background-color: #f0f2f5;
  flex-grow: 1;
  padding: 24px 0;
  overflow-y: auto;
}
.profile-page { max-width: 980px; margin: 0 auto; background-color: #fff; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;}
.profile-header { position: relative; }
.background-image-wrapper { height: 250px; background-color: #e9ecef; }
.background-image { width: 100%; height: 100%; object-fit: cover; }
.profile-info-bar { display: flex; justify-content: space-between; align-items: flex-end; padding: 0 24px; position: relative; top: -40px; margin-bottom: -40px; }
.profile-avatar-wrapper { border: 4px solid white; border-radius: 50%; width: 140px; height: 140px; background-color: white; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
.profile-avatar { width: 100%; height: 100%; border-radius: 50%; object-fit: cover; }
.profile-actions { padding-bottom: 12px; }
.edit-profile-btn { background-color: #ed664d; color: white; padding: 8px 16px; border-radius: 6px; text-decoration: none; font-weight: 600; border: none; cursor: pointer; }
.action-btn {
  padding: 8px 16px;
  border-radius: 6px;
  font-weight: 600;
  border: none;
  cursor: pointer;
  margin-right: 8px;
  transition: opacity 0.2s;
}
.action-btn:hover {
  opacity: 0.9;
}
.action-btn.primary {
  background-color: #ed664d;
  color: white;
}
.action-btn.secondary {
  background-color: #e0e0e0;
  color: #333;
}
.action-btn.danger {
  background-color: #dc3545;
  color: white;
}
.action-btn.disabled {
  background-color: #ccc;
  color: #666;
  cursor: not-allowed;
}
.action-btn:last-child {
  margin-right: 0;
}
.profile-details { padding: 0 24px 24px 24px; }
.nickname { font-size: 2rem; font-weight: 800; margin: 0 0 4px 0; }
.handle { font-size: 1rem; color: #666; margin-bottom: 16px; }
.description { font-size: 1rem; color: #333; margin-bottom: 16px; }
.stats-container { display: flex; gap: 24px; }
.stat { font-size: 1rem; }
.stat-value { font-weight: 600; margin-right: 4px; }
.stat-label { color: #666; }
.content-tabs { display: flex; border-top: 1px solid #eee; padding: 0 24px; }
.tab { padding: 16px 0; margin-right: 24px; font-weight: 600; cursor: pointer; border-bottom: 2px solid transparent; }
.tab.active { color: #ed664d; border-bottom-color: #ed664d; }
.my-posts-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px 24px 32px 24px;
}
.sentinel {
  height: 1px;
}
.spinner.small-spinner {
  width: 30px;
  height: 30px;
  border-width: 4px;
  margin: 16px auto;
}
.end-of-feed {
  text-align: center;
  color: #888;
  margin-top: 16px;
}
</style>
