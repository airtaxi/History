import apiClient from '@/api';

// 브라우저 콘솔에서 사용할 수 있는 Friendship API 테스트 함수들
const friendshipAPI = {
  // 친구 요청 보내기
  sendRequest: async (receiverId: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/request/${receiverId}`);
      console.log('✅ 친구 요청 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 친구 요청 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 친구 요청 수락
  acceptRequest: async (userIdToAccept: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/request/${userIdToAccept}/accept`);
      console.log('✅ 친구 요청 수락 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 친구 요청 수락 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 친구 요청 거절
  declineRequest: async (userIdToDecline: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/request/${userIdToDecline}/decline`);
      console.log('✅ 친구 요청 거절 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 친구 요청 거절 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 친구 요청 취소
  cancelRequest: async (userIdToCancel: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/request/${userIdToCancel}/cancel`);
      console.log('✅ 친구 요청 취소 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 친구 요청 취소 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 사용자 차단
  blockUser: async (userIdToBlock: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/block/${userIdToBlock}`);
      console.log('✅ 사용자 차단 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 사용자 차단 실패:', error.response?.data || error.message);
      console.error('상태 코드:', error.response?.status);
      console.error('전체 에러 객체:', error.response);
      throw error;
    }
  },

  // 사용자 무시
  ignoreUser: async (userIdToIgnore: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/ignore/${userIdToIgnore}`);
      console.log('✅ 사용자 무시 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 사용자 무시 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 친구 삭제
  removeFriend: async (userIdToRemove: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/remove/${userIdToRemove}`);
      console.log('✅ 친구 삭제 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 친구 삭제 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 차단 해제
  unblockUser: async (blockedUserId: string) => {
    try {
      const response = await apiClient.delete(`/api/Friendship/block/${blockedUserId}`);
      console.log('✅ 차단 해제 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 차단 해제 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 무시 해제
  unignoreUser: async (ignoredUserId: string) => {
    try {
      const response = await apiClient.delete(`/api/Friendship/ignore/${ignoredUserId}`);
      console.log('✅ 무시 해제 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 무시 해제 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 받은 친구 요청 목록
  getPendingRequests: async () => {
    try {
      const response = await apiClient.get('/api/Friendship/pending');
      console.log('✅ 받은 친구 요청 목록:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 받은 친구 요청 목록 조회 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 보낸 친구 요청 목록
  getWaitingRequests: async () => {
    try {
      const response = await apiClient.get('/api/Friendship/waiting');
      console.log('✅ 보낸 친구 요청 목록:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 보낸 친구 요청 목록 조회 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 차단한 사용자 목록
  getBlockedUsers: async () => {
    try {
      const response = await apiClient.get('/api/Friendship/blocked');
      console.log('✅ 차단한 사용자 목록:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 차단한 사용자 목록 조회 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 무시한 사용자 목록
  getIgnoredUsers: async () => {
    try {
      const response = await apiClient.get('/api/Friendship/ignored');
      console.log('✅ 무시한 사용자 목록:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 무시한 사용자 목록 조회 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 특정 사용자의 친구 목록
  getUserFriends: async (userId: string) => {
    try {
      const response = await apiClient.get(`/api/Friendship/${userId}`);
      console.log(`✅ 사용자 ${userId}의 친구 목록:`, response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 친구 목록 조회 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 즐겨찾기 토글
  toggleFavorite: async (userId: string) => {
    try {
      const response = await apiClient.post(`/api/Friendship/toggle-favorite/${userId}`);
      console.log('✅ 즐겨찾기 토글 성공:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('❌ 즐겨찾기 토글 실패:', error.response?.data || error.message);
      throw error;
    }
  },

  // 모든 친구 관계 상태 확인
  checkAllStatus: async (targetUserId: string) => {
    try {
      console.log(`🔍 사용자 ${targetUserId}와의 모든 관계 상태 확인 중...`);
      
      const [friends, pending, waiting, blocked, ignored] = await Promise.all([
        apiClient.get(`/api/Friendship/${targetUserId}`),
        apiClient.get('/api/Friendship/pending'),
        apiClient.get('/api/Friendship/waiting'),
        apiClient.get('/api/Friendship/blocked'),
        apiClient.get('/api/Friendship/ignored')
      ]);

      const status = {
        isFriend: friends.data.some((f: any) => f.userId === targetUserId),
        hasPendingFromUser: pending.data.some((r: any) => r.userId === targetUserId),
        hasPendingToUser: waiting.data.some((r: any) => r.userId === targetUserId),
        isBlocked: blocked.data.some((b: any) => b.userId === targetUserId),
        isIgnored: ignored.data.some((i: any) => i.userId === targetUserId)
      };

      console.log('✅ 관계 상태:', status);
      return status;
    } catch (error: any) {
      console.error('❌ 관계 상태 확인 실패:', error.response?.data || error.message);
      throw error;
    }
  }
};

// 브라우저 콘솔에서 사용할 수 있도록 window 객체에 추가
if (typeof window !== 'undefined') {
  (window as any).friendshipAPI = friendshipAPI;
  console.log('✨ friendshipAPI가 로드되었습니다!');
  console.log('사용 가능한 함수들:');
  console.log('- friendshipAPI.sendRequest(receiverId)');
  console.log('- friendshipAPI.acceptRequest(userIdToAccept)');
  console.log('- friendshipAPI.declineRequest(userIdToDecline)');
  console.log('- friendshipAPI.cancelRequest(userIdToCancel)');
  console.log('- friendshipAPI.blockUser(userIdToBlock)');
  console.log('- friendshipAPI.ignoreUser(userIdToIgnore)');
  console.log('- friendshipAPI.removeFriend(userIdToRemove)');
  console.log('- friendshipAPI.unblockUser(blockedUserId)');
  console.log('- friendshipAPI.unignoreUser(ignoredUserId)');
  console.log('- friendshipAPI.getPendingRequests()');
  console.log('- friendshipAPI.getWaitingRequests()');
  console.log('- friendshipAPI.getBlockedUsers()');
  console.log('- friendshipAPI.getIgnoredUsers()');
  console.log('- friendshipAPI.getUserFriends(userId)');
  console.log('- friendshipAPI.toggleFavorite(userId)');
  console.log('- friendshipAPI.checkAllStatus(targetUserId)');
}

export default friendshipAPI; 