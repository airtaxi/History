<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  discoveryOption: string;
  commentPermission: string | null;
  disallowShare: boolean;
  reservationTime: string | null;
}>();

const emit = defineEmits([
  'update:discoveryOption',
  'update:commentPermission',
  'update:disallowShare',
  'update:reservationTime',
  'discovery-option-change',
]);

const onDiscoveryOptionChange = (event: Event) => {
  const target = event.target as HTMLSelectElement;
  emit('update:discoveryOption', target.value);
  emit('discovery-option-change');
};
</script>

<template>
  <div class="footer-options-group">
    <div class="option-item">
      <label for="discovery-option">공개</label>
      <select 
        id="discovery-option" 
        :value="discoveryOption"
        @change="onDiscoveryOptionChange"
      >
        <option value="OnlyMe">나만 보기</option>
        <option value="SelectedUsers">특정 친구 공개</option>
        <option value="UnselectedUsers">특정 친구 비공개</option>
        <option value="Friends">친구 공개</option>
        <option value="FriendsOfFriends">친구의 친구까지</option>
        <option value="Everyone">전체 공개</option>
      </select>
    </div>
    <div class="option-item">
      <label for="comment-permission">댓글</label>
      <select 
        id="comment-permission" 
        :value="commentPermission"
        @input="$emit('update:commentPermission', ($event.target as HTMLSelectElement).value)"
      >
        <option :value="null">게시글 설정 따름</option>
        <option value="OnlyMe">나만</option>
        <option value="Friends">친구만</option>
        <option value="FriendsOfFriends">친구의 친구까지</option>
        <option value="Everyone">모든 사람</option>
      </select>
    </div>
    <div class="option-item checkbox-item">
      <input 
        type="checkbox" 
        id="disallow-share" 
        :checked="disallowShare"
        @change="$emit('update:disallowShare', ($event.target as HTMLInputElement).checked)"
      >
      <label for="disallow-share">공유 금지</label>
    </div>
    <div class="option-item">
      <label for="reservation-time">예약</label>
      <input 
        type="datetime-local" 
        id="reservation-time" 
        :value="reservationTime"
        @input="$emit('update:reservationTime', ($event.target as HTMLInputElement).value)"
      >
    </div>
  </div>
</template>

<style scoped>
/* CreatePost.vue에서 복사해온 스타일 */
.footer-options-group {
  display: flex;
  flex-wrap: wrap;
  gap: 24px; /* 그룹 간 간격 */
  align-items: flex-end;
}

.option-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

label {
  font-size: 0.9rem;
  font-weight: 500;
  color: #495057;
}

select,
input[type="datetime-local"] {
  padding: 8px 12px;
  border-radius: 6px;
  border: 1px solid #ddd;
  background-color: white;
  font-size: 0.9rem;
  height: 38px; 
}

input[type="checkbox"] {
  width: 16px;
  height: 16px;
  cursor: pointer;
}

.checkbox-item label {
  cursor: pointer;
}
</style>