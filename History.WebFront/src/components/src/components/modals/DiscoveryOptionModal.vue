<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue';
import apiClient from '@/api';
import { useFriendData } from '@/components/composables/useFriendData'; 
import FriendSelector from '@/components/CreatePostComponent/FriendSelector.vue'

const props = defineProps<{
  show: boolean;
  postId: string;
  initialDiscoveryOption: string;
  initialSelectedUserIds: string[];
}>();

const emit = defineEmits(['close', 'update-success']);

// 모달 내부에서 사용할 상태
const currentOption = ref('');
const currentSelectedIds = ref<string[]>([]);
const isLoading = ref(false);

// 친구 목록 로드 (CreatePost에서 사용한 것과 동일한 composable)
const { friendsList, loadFriends } = useFriendData();

// 모달이 열릴 때 props로 받은 초기값으로 내부 상태를 설정
watch(() => props.show, (newVal) => {
  if (newVal) {
    currentOption.value = props.initialDiscoveryOption;
    currentSelectedIds.value = [...props.initialSelectedUserIds];
    loadFriends(); // 친구 목록 불러오기
  }
});

// 특정 친구 선택이 필요한 옵션인지 확인
const needsFriendSelection = computed(() => {
  return ['SelectedUsers', 'UnselectedUsers'].includes(currentOption.value);
});

async function saveSettings() {
  isLoading.value = true;
  try {
    const payload = {
      newDiscoveryOption: currentOption.value,
      selectedUserIds: currentSelectedIds.value
    };
    
    // API 호출
    await apiClient.put(`/api/Post/${props.postId}/discovery-option`, payload);

    alert('공개범위가 성공적으로 변경되었습니다.');
    emit('update-success'); // 부모에게 성공 알림
    emit('close'); // 모달 닫기
  } catch (error) {
    console.error('공개범위 변경 실패:', error);
    alert('공개범위 변경에 실패했습니다.');
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div v-if="show" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content">
      <h3 class="modal-title">공개범위 설정</h3>

      <div class="options-group">
        <label>
          <input type="radio" v-model="currentOption" value="Everyone" /> 전체 공개
        </label>
        <label>
          <input type="radio" v-model="currentOption" value="FriendsOfFriends" /> 친구의 친구
        </label>
        <label>
          <input type="radio" v-model="currentOption" value="Friends" /> 친구만
        </label>
        <label>
          <input type="radio" v-model="currentOption" value="OnlyMe" /> 나만 보기
        </label>
        <label>
          <input type="radio" v-model="currentOption" value="SelectedUsers" /> 특정 친구에게만 공개
        </label>
        <label>
          <input type="radio" v-model="currentOption" value="UnselectedUsers" /> 특정 친구 숨기기
        </label>
      </div>

      <FriendSelector
        v-if="needsFriendSelection"
        v-model="currentSelectedIds"
        :discovery-option="currentOption"
        :friends-list="friendsList"
      />

      <div class="modal-footer">
        <button @click="emit('close')" class="btn-cancel" :disabled="isLoading">취소</button>
        <button @click="saveSettings" class="btn-submit" :disabled="isLoading">
          {{ isLoading ? '저장 중...' : '저장' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0; left: 0;
  width: 100%; height: 100%;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}
.modal-content {
  background: white;
  padding: 24px;
  border-radius: 12px;
  width: 90%;
  max-width: 500px;
}
.modal-title {
  margin-top: 0;
  margin-bottom: 24px;
  font-size: 1.5rem;
  font-weight: 600;
}
.options-group {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 24px;
}
.options-group label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}
.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
}
.btn-cancel, .btn-submit {
  padding: 8px 24px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
}
.btn-cancel { background-color: #e9ecef; color: #495057; }
.btn-submit { background-color: #ed664d; color: white; }
</style>