<script setup lang="ts">
import { ref, onMounted, computed, nextTick } from 'vue';
import apiClient from '@/api';
import { useAuthStore } from '@/stores/auth';
import type { UserResponseDto } from '@/types';
import defaultProfile from '@/assets/images/default_profile_image.jpg';
import { format } from 'date-fns';

// --- 타입 정의 ---
interface Message {
  messageId: string;
  sender: UserResponseDto;
  receiver: UserResponseDto;
  content: string;
  sentAt: string;
  readAt: string | null;
}

interface Thread {
  otherUser: UserResponseDto;
  lastMessage: Message;
  unreadCount: number;
}

// --- 상태 변수 ---
const authStore = useAuthStore();
const myProfile = computed(() => authStore.user);
const threads = ref<Thread[]>([]);
const selectedThread = ref<Thread | null>(null);
const messagesInSelectedThread = ref<Message[]>([]);
const newMessageContent = ref('');
const profileImageMap = ref<Record<string, string>>({});
const messageContainer = ref<HTMLElement | null>(null);


// 미디어 ID로 이미지 Blob URL 가져오기
const getMediaBlobUrl = async (mediaId: string | null): Promise<string> => {
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

// 사용자 목록의 프로필 이미지 미리 준비하기
const prepareProfileImageMap = async (users: UserResponseDto[]) => {
  const newMap: Record<string, string> = {};
  for (const user of users) {
    if (!profileImageMap.value[user.userId]) { // 이미 있는 이미지는 다시 받지 않음
      newMap[user.userId] = await getMediaBlobUrl(user.profileThumbnailMediaId);
    }
  }
  profileImageMap.value = { ...profileImageMap.value, ...newMap };
};

// 스크롤을 맨 아래로 이동
const scrollToBottom = () => {
  nextTick(() => {
    const container = messageContainer.value;
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  });
};

// 대화 목록(스레드) 불러오기
const fetchThreads = async () => {
  try {
    const [sentRes, receivedRes] = await Promise.all([
      apiClient.get<Message[]>('/api/Message/sent'),
      apiClient.get<Message[]>('/api/Message/received'),
    ]);

    const allMessages = [...sentRes.data, ...receivedRes.data];
    if (allMessages.length === 0) return;

    // 다른 사용자를 기준으로 메시지 그룹화
    const threadsMap = new Map<string, { messages: Message[], unreadCount: number }>();

    for (const msg of allMessages) {
      const otherUserId = msg.sender.userId === myProfile.value?.userId ? msg.receiver.userId : msg.sender.userId;
      
      if (!threadsMap.has(otherUserId)) {
        threadsMap.set(otherUserId, { messages: [], unreadCount: 0 });
      }
      
      const threadData = threadsMap.get(otherUserId)!;
      threadData.messages.push(msg);

      // 내가 받은 읽지 않은 메시지 카운트
      if (msg.receiver.userId === myProfile.value?.userId && !msg.readAt) {
        threadData.unreadCount++;
      }
    }

    const finalThreads: Thread[] = [];
    for (const [userId, data] of threadsMap.entries()) {
      // 메시지를 시간순으로 정렬
      data.messages.sort((a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime());
      
      const lastMessage = data.messages[0];
      const otherUser = lastMessage.sender.userId === myProfile.value?.userId ? lastMessage.receiver : lastMessage.sender;

      finalThreads.push({
        otherUser,
        lastMessage,
        unreadCount: data.unreadCount
      });
    }

    threads.value = finalThreads.sort((a, b) => new Date(b.lastMessage.sentAt).getTime() - new Date(a.lastMessage.sentAt).getTime());

    await prepareProfileImageMap(threads.value.map(t => t.otherUser));

  } catch (error) {
    console.error("대화 목록 로딩 실패:", error);
  }
};

const selectThread = async (thread: Thread) => {
  selectedThread.value = thread;
  try {
    const [sentRes, receivedRes] = await Promise.all([
      apiClient.get<Message[]>('/api/Message/sent'),
      apiClient.get<Message[]>('/api/Message/received'),
    ]);

    const otherUserId = thread.otherUser.userId;

    const conversationMessages = [
      ...sentRes.data.filter(m => m.receiver.userId === otherUserId),
      ...receivedRes.data.filter(m => m.sender.userId === otherUserId)
    ]
    .filter(m => m && m.content && m.content.trim() !== '') 
    .sort((a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
    
    messagesInSelectedThread.value = conversationMessages;
    
    if (thread.unreadCount > 0) {
      const unreadMessageIds = conversationMessages
        .filter(m => m.receiver.userId === myProfile.value?.userId && !m.readAt)
        .map(m => m.messageId);
      
      if (unreadMessageIds.length > 0) {
        await Promise.all(unreadMessageIds.map(id => apiClient.post(`/api/Message/${id}/read`)));
      }
      
      thread.unreadCount = 0;
    }

    scrollToBottom();
  } catch (error) {
    console.error("메시지 내용 로딩 실패:", error);
  }
};

// 메시지 전송
const sendMessage = async () => {
  if (!newMessageContent.value.trim() || !selectedThread.value) return;

  const receiverId = selectedThread.value.otherUser.userId;
  const content = newMessageContent.value;
  
  try {
    await apiClient.post('/api/Message/send', { receiverId, content });
    newMessageContent.value = '';
    // 메시지 전송 후, 현재 대화 목록을 즉시 갱신
    await selectThread(selectedThread.value);
    // 전체 스레드 목록도 갱신하여 순서 변경
    await fetchThreads();
  } catch (error) {
    console.error("메시지 전송 실패:", error);
    alert("메시지 전송에 실패했습니다.");
  }
};

onMounted(() => {
  if (myProfile.value) {
    fetchThreads();
  }
});

const formatTimestamp = (dateString: string | null) => {

  if (!dateString) {
    return '시간 정보 없음'; // 빈 문자열이나 기본 텍스트를 반환
  }
  
  try {
    return format(new Date(dateString), 'yyyy-MM-dd HH:mm');
  } catch (error) {
    console.error("Invalid date value for formatting:", dateString);
    return '시간 형식 오류'; 
  }
};
</script>

<template>
  <div class="message-view">
    <div class="sidebar-card thread-list-container">
      <h2 class="thread-list-header">쪽지함</h2>
      <ul class="thread-list">
        <li v-for="thread in threads" :key="thread.otherUser.userId" 
            class="thread-item"
            :class="{ active: selectedThread?.otherUser.userId === thread.otherUser.userId }"
            @click="selectThread(thread)">
          
          <img :src="profileImageMap[thread.otherUser.userId] || defaultProfile" class="friend-avatar" />
          <div class="thread-info">
            <div class="thread-user">
              <span class="nickname">{{ thread.otherUser.nickname }}</span>
              <span v-if="thread.unreadCount > 0" class="unread-badge">{{ thread.unreadCount }}</span>
            </div>
            <p class="last-message">{{ thread.lastMessage.content }}</p>
          </div>
        </li>
      </ul>
      <p v-if="threads.length === 0" class="empty-message">아직 대화가 없습니다.</p>
    </div>

    <div class="sidebar-card chat-view">
      <template v-if="selectedThread">
        <div class="chat-header">
          <h3>{{ selectedThread.otherUser.nickname }}</h3>
        </div>
        <div class="message-container" ref="messageContainer">
          <div v-for="message in messagesInSelectedThread" :key="message.messageId" 
               class="message-bubble"
               :class="{ 'sent': message.sender.userId === myProfile?.userId, 'received': message.sender.userId !== myProfile?.userId }">
            <div class="message-content">{{ message.content }}</div>
            <div class="message-timestamp">{{ formatTimestamp(message.sentAt) }}</div>
          </div>
        </div>
        <form class="message-form" @submit.prevent="sendMessage">
          <textarea v-model="newMessageContent" placeholder="메시지를 입력하세요..." @keydown.enter.prevent="sendMessage"></textarea>
          <button type="submit">보내기</button>
        </form>
      </template>
      <div v-else class="no-selection">
        <p>대화를 선택하여<br>내용을 확인하세요.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.message-view {
  display: flex;
  gap: 16px;
  height: calc(100vh - 120px); 
}

.thread-list-container {
  width: 320px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
}

.chat-view {
  flex-grow: 1;
  display: flex;
  flex-direction: column;
}
.thread-list-header {
    font-size: 1.2rem;
    font-weight: 600;
    padding-bottom: 12px;
    margin: 0 0 8px 0;
    border-bottom: 1px solid #eee;
}
.thread-list {
  list-style: none;
  padding: 0;
  margin: 0;
  overflow-y: auto;
  flex: 1;
}

.thread-item {
  display: flex;
  align-items: center;
  padding: 12px;
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.thread-item:hover {
  background-color: #f9f9f9;
}

.thread-item.active {
  background-color: #f0f2f5;
}

.friend-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  object-fit: cover;
  margin-right: 12px;
}
.thread-info {
    flex: 1;
    overflow: hidden;
}
.thread-user {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 4px;
}
.nickname {
    font-weight: 600;
}
.unread-badge {
    background-color: #ed664d;
    color: white;
    font-size: 0.75rem;
    font-weight: bold;
    border-radius: 50%;
    padding: 2px 6px;
    min-width: 20px;
    text-align: center;
}
.last-message {
    margin: 0;
    font-size: 0.9rem;
    color: #666;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.chat-header {
  padding-bottom: 12px;
  border-bottom: 1px solid #eee;
  margin-bottom: 12px;
}
.chat-header h3 {
  margin: 0;
  font-size: 1.1rem;
}
.message-container {
  flex-grow: 1;
  overflow-y: auto;
  padding: 0 10px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.message-bubble {
  max-width: 70%;
  padding: 8px 12px;
  border-radius: 18px;
  display: flex;
  flex-direction: column;
}
.message-content {
  white-space: pre-wrap;
  word-wrap: break-word;
}
.message-timestamp {
  font-size: 0.75rem;
  color: #888;
  margin-top: 4px;
}

.sent {
  background-color: #ed664d;
  color: white;
  align-self: flex-end;
  border-bottom-right-radius: 4px;
}
.sent .message-timestamp {
  color: #f0f0f0;
  text-align: right;
}

.received {
  background-color: #f0f2f5;
  color: #333;
  align-self: flex-start;
  border-bottom-left-radius: 4px;
}

.message-form {
  display: flex;
  padding-top: 12px;
  border-top: 1px solid #eee;
  margin-top: auto;
}

.message-form textarea {
  flex-grow: 1;
  border: 1px solid #ddd;
  border-radius: 18px;
  padding: 10px 16px;
  resize: none;
  font-size: 0.95rem;
  min-height: 40px;
  max-height: 120px;
  line-height: 1.4;
}

.message-form button {
  margin-left: 8px;
  padding: 0 16px;
  border: none;
  border-radius: 18px;
  background-color: #ed664d;
  color: white;
  font-weight: 500;
  cursor: pointer;
}
.no-selection {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100%;
    text-align: center;
    color: #888;
    font-size: 1.1rem;
}

.empty-message { text-align: center; color: #888; padding: 20px 0; font-size: 0.9rem; }
.sidebar-card { background: white; border-radius: 8px; border: 1px solid #ddd; padding: 16px; }

</style>