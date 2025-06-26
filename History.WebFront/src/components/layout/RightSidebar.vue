<script setup lang="ts">
import { ref, onMounted } from 'vue';
import apiClient from '@/api';
import { useAuthStore } from '@/stores/auth';
import type { UserResponseDto } from '@/types';
import defaultProfile from '@/assets/images/default_profile_image.jpg';

const authStore = useAuthStore();
const myProfile = ref<UserResponseDto | null>(null);
const friends = ref<UserResponseDto[]>([]);
const pendingRequests = ref<UserResponseDto[]>([]);
const profileImageMap = ref<Record<string, string>>({});
const activeTab = ref<'friends' | 'requests'>('friends');
const getProfileUrl = (mediaId: string | null) =>
  mediaId ? `/api/Media/${mediaId}` : defaultProfile;

const getMediaBlobUrl = async (mediaId: string | null) => {
  if (!mediaId) return defaultProfile;
  try {
    const res = await apiClient.get(`/api/Media/${mediaId}`, { responseType: 'blob' });
    const type = res.headers['content-type'];
    if (!type.startsWith('image')) return defaultProfile;
    return URL.createObjectURL(res.data);
  } catch {
    return defaultProfile;
  }
};

const prepareProfileImageMap = async (users: UserResponseDto[]) => {
  const map: Record<string, string> = {};
  for (const user of users) {
    map[user.userId] = await getMediaBlobUrl(user.profileThumbnailMediaId);
  }
  profileImageMap.value = map;
};

// 친구 요청 수락
const acceptFriendRequest = async (userId: string) => {
  try {
    await apiClient.post(`/api/Friendship/request/${userId}/accept`);
    // 수락 후 해당 요청을 목록에서 제거하고 친구 목록 새로고침
    pendingRequests.value = pendingRequests.value.filter(r => r.userId !== userId);
    await refreshFriendsList();
    alert('친구 요청을 수락했습니다.');
  } catch (error) {
    console.error('친구 요청 수락 실패:', error);
    alert('친구 요청 수락에 실패했습니다.');
  }
};

// 친구 요청 거절
const rejectFriendRequest = async (userId: string) => {
  try {
    await apiClient.post(`/api/Friendship/request/${userId}/decline`);
    // 거절 후 해당 요청을 목록에서 제거
    pendingRequests.value = pendingRequests.value.filter(r => r.userId !== userId);
    alert('친구 요청을 거절했습니다.');
  } catch (error) {
    console.error('친구 요청 거절 실패:', error);
    alert('친구 요청 거절에 실패했습니다.');
  }
};

// 친구 요청 무시
const ignoreFriendRequest = async (userId: string) => {
  try {
    await apiClient.post(`/api/Friendship/ignore/${userId}`);
    // 무시 후 해당 요청을 목록에서 제거
    pendingRequests.value = pendingRequests.value.filter(r => r.userId !== userId);
    alert('친구 요청을 무시했습니다.');
  } catch (error) {
    console.error('친구 요청 무시 실패:', error);
    alert('친구 요청 무시에 실패했습니다.');
  }
};

// 친구 목록 새로고침
const refreshFriendsList = async () => {
  if (!myProfile.value) return;
  try {
    const friendsRes = await apiClient.get(`/api/Friendship/${myProfile.value.userId}`);
    friends.value = friendsRes.data;
    await prepareProfileImageMap(friends.value);
  } catch (error) {
    console.error('친구 목록 새로고침 실패:', error);
  }
};

onMounted(async () => {
  try {
    const profileRes = await apiClient.get('/api/User/me');
    myProfile.value = profileRes.data;

    // 내 프로필을 성공적으로 가져온 후에 친구 목록과 신청 목록을 가져옵니다.
    if (myProfile.value) {
        const [friendsRes, pendingRes] = await Promise.all([
            apiClient.get(`/api/Friendship/${myProfile.value.userId}`),
            apiClient.get('/api/Friendship/pending')
        ]);
        friends.value = friendsRes.data;
        pendingRequests.value = pendingRes.data;

        await prepareProfileImageMap([myProfile.value, ...friends.value, ...pendingRequests.value]);
    }
  } catch (error) {
    console.error('사이드바 정보 로딩 실패:', error);
  }
});
</script>

<template>
  <div class="sidebar-column">
    <div v-if="myProfile" class="sidebar-card profile-summary-card">
      <RouterLink :to="`/user/${myProfile.userId}`">
        <img :src="profileImageMap[myProfile.userId] || defaultProfile" class="my-avatar" />
      </RouterLink>
      <div class="my-name">{{ myProfile.nickname }}</div>
      <div class="my-handle">@{{ myProfile.handle }}</div>
    </div>

    <div class="sidebar-card friends-card">
      <div class="tabs">
        <button :class="{ active: activeTab === 'friends' }" @click="activeTab = 'friends'">친구</button>
        <button :class="{ active: activeTab === 'requests' }" @click="activeTab = 'requests'">신청</button>
        <button>쪽지</button>
      </div>
      
      <div v-if="activeTab === 'friends'">
        <ul class="friend-list">
          <li v-for="friend in friends" :key="friend.userId" class="friend-item">
            <RouterLink :to="`/user/${friend.userId}`" class="friend-link">
              <img :src="profileImageMap[friend.userId] || defaultProfile" class="friend-avatar" />
              <span>{{ friend.nickname }}</span>
            </RouterLink>
          </li>
        </ul>
        <p v-if="friends.length === 0" class="empty-message">아직 친구가 없습니다.</p>
      </div>
      <div v-if="activeTab === 'requests'">
         <ul class="friend-list">
            <li v-for="request in pendingRequests" :key="request.userId" class="friend-request-item">
              <RouterLink :to="`/user/${request.userId}`" class="request-info">
                <img :src="profileImageMap[request.userId] || defaultProfile" class="friend-avatar" />
                <span>{{ request.nickname }}</span>
              </RouterLink>
              <div class="request-actions">
                <button @click="acceptFriendRequest(request.userId)" class="accept-btn">수락</button>
                <button @click="rejectFriendRequest(request.userId)" class="reject-btn">거절</button>
                <button @click="ignoreFriendRequest(request.userId)" class="ignore-btn">무시</button>
              </div>
            </li>
        </ul>
        <p v-if="pendingRequests.length === 0" class="empty-message">받은 친구 요청이 없습니다.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sidebar-column {
  width: 320px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.sidebar-card {
  background: white;
  border-radius: 8px;
  border: 1px solid #ddd;
  padding: 16px;
}
.profile-summary-card { text-align: center; }
.my-avatar { width: 72px; height: 72px; border-radius: 50%; margin-bottom: 12px; }
.my-name { font-weight: 600; font-size: 1.1rem; }
.my-handle { color: #666; font-size: 0.9rem; }
.tabs { display: flex; border-bottom: 1px solid #eee; margin-bottom: 8px; }
.tabs button { flex: 1; padding: 10px; border: none; background: none; cursor: pointer; font-weight: 500; color: #888; border-bottom: 2px solid transparent; transition: color 0.2s, border-color 0.2s; }
.tabs button.active { color: #ed664d; border-bottom-color: #ed664d; }
.friend-list { list-style: none; padding: 0; margin: 0; }
.friend-item { padding: 8px 0; }
.friend-link { display: flex; align-items: center; gap: 12px; text-decoration: none; color: inherit; padding: 4px; border-radius: 6px; transition: background-color 0.2s; }
.friend-link:hover { background-color: #f0f2f5; }
.friend-avatar { width: 36px; height: 36px; border-radius: 50%; }
.empty-message { text-align: center; color: #888; padding: 20px 0; font-size: 0.9rem; }

.friend-request-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
}

.request-info {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: inherit;
  flex: 1;
  padding: 4px;
  border-radius: 6px;
  transition: background-color 0.2s;
}

.request-info:hover {
  background-color: #f0f2f5;
}

.request-actions {
  display: flex;
  gap: 4px;
}

.accept-btn, .reject-btn, .ignore-btn {
  padding: 4px 8px;
  border: none;
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 500;
  cursor: pointer;
  transition: opacity 0.2s;
}

.accept-btn {
  background-color: #ed664d;
  color: white;
}

.accept-btn:hover {
  opacity: 0.9;
}

.reject-btn {
  background-color: #e0e0e0;
  color: #333;
}

.reject-btn:hover {
  opacity: 0.9;
}

.ignore-btn {
  background-color: #f0f0f0;
  color: #666;
}

.ignore-btn:hover {
  opacity: 0.9;
}
</style>