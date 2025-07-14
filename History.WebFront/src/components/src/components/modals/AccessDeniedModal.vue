<!--
 * AccessDeniedModal.vue
 *
 * 이 컴포넌트는 사용자에게 게시물 접근이 거부되었음을 알리는 모달입니다.
 * 주로 비공개 게시물에 접근하려 할 때 표시되며, 접근 거부 사유와 함께
 * 해당 사용자의 프로필로 이동할 수 있는 옵션을 제공합니다.
 *
 * @props {
 *   show: boolean - 모달의 표시 여부.
 *   deniedUserNickname: string - 접근이 거부된 게시물 작성자의 닉네임.
 *   deniedUserId: string - 접근이 거부된 게시물 작성자의 사용자 ID.
 * }
 * @emits {
 *   close: 모달을 닫을 때 발생.
 * }
-->
<template>
  <Teleport to="body">
    <div v-if="show" class="access-denied-modal">
      <div class="access-denied-overlay" @click="$emit('close')"></div>
      <div class="access-denied-content">
        <p class="modal-text">
          친구공개 스토리입니다.<br />
          {{ deniedUserNickname }}님과 친구를 맺으면 스토리를 확인할 수 있습니다.
        </p>
        <div class="modal-actions">
          <button class="modal-cancel" @click="$emit('close')">취소</button>
          <button class="modal-visit" @click="router.push(`/user/${deniedUserId}`)">스토리 방문</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';

const props = defineProps<{
  show: boolean;
  deniedUserNickname: string;
  deniedUserId: string;
}>();

const emit = defineEmits(['close', 'send-friend-request']);
</script>

<style scoped>
.access-denied-modal {
  position: fixed;
  z-index: 9999;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.access-denied-overlay {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
}

.access-denied-content {
  position: relative; /* 오버레이 위에 오도록 */
  background: white;
  padding: 24px;
  border-radius: 12px;
  width: 320px;
  text-align: center;
  box-shadow: 0 4px 16px rgba(0,0,0,0.2);
}

.access-denied-content .modal-text {
  margin-bottom: 20px;
}
.access-denied-content .modal-actions {
  display: flex;
  gap: 10px;
}
.access-denied-content .modal-actions button {
  flex: 1;
  padding: 10px;
  border-radius: 8px;
  border: none;
  font-weight: bold;
  cursor: pointer;
}
.access-denied-content .modal-cancel {
  background-color: #f0f0f0;
}
.access-denied-content .modal-visit {
  background-color: #ed664d;
  color: white;
}
</style>
