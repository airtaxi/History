import { ref } from 'vue';
import apiClient from '@/api';
import type { UserResponseDto } from '@/types';

export function useFriendData() {
  const friendsList = ref<UserResponseDto[]>([]);
  const myProfile = ref<any | null>(null);

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

  const loadFriends = async () => {
    try {
      if (!myProfile.value) {
        const profileRes = await apiClient.get('/api/User/me');
        myProfile.value = profileRes.data;
      }
      if (myProfile.value) {
        const response = await apiClient.get(`/api/Friendship/${myProfile.value.userId}`);
        const friends = response.data;

        for (const friend of friends) {
          if (friend.profileThumbnailMediaId) {
            const imageUrl = await getMediaBlobUrl(friend.profileThumbnailMediaId);
            friend.profileImageUrl = imageUrl || '/src/assets/images/default_profile_image.jpg';
          } else {
            friend.profileImageUrl = '/src/assets/images/default_profile_image.jpg';
          }
        }
        friendsList.value = friends;
      }
    } catch (error) {
      console.error('❌ 친구 목록 로드 실패:', error);
      friendsList.value = [];
    }
  };

  return {
    friendsList,
    myProfile,
    loadFriends,
    getMediaBlobUrl
  };
}
