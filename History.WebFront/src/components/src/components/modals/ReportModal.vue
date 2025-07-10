<!--
 * ReportModal.vue
 *
 * 이 컴포넌트는 게시물을 신고하기 위한 모달 UI를 렌더링합니다.
 * 사용자가 미리 정의된 신고 사유를 선택하거나 추가적인 설명을 입력할 수 있도록 합니다.
 *
 * @props {
 *   show: boolean - 모달의 표시 여부.
 * }
 * @emits {
 *   close: 모달을 닫을 때 발생.
 *   submit: 사용자가 신고를 제출할 때 발생. 선택된 사유를 페이로드로 전달.
 * }
-->
<template>
  <Teleport to="body">
    <div v-if="show" class="report-modal-overlay" @click.self="$emit('close')">
      <div class="report-modal-content" @click.stop>
        <p>🚨 신고 사유를 선택해주세요:</p>
        <select v-model="selectedReason">
          <option value="ExplicitContent">성인물</option>
          <option value="CopyrightViolation">저작권 위반</option>
          <option value="IllegalContent">불법 콘텐츠</option>
          <option value="Other">기타</option>
        </select>
        <div class="report-actions">
          <button @click="$emit('submit', selectedReason)">신고하기</button>
          <button @click="$emit('close')">취소</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

const props = defineProps<{
  show: boolean;
}>();

const emit = defineEmits(['close', 'submit']);

const selectedReason = ref('ExplicitContent');

// 모달이 열릴 때마다 선택된 사유를 초기화
watch(() => props.show, (isShowing) => {
  if (isShowing) {
    selectedReason.value = 'ExplicitContent';
  }
});
</script>

<style scoped>
.report-modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 10000;
}

.report-modal-content {
  background: white;
  padding: 24px;
  border-radius: 12px;
  width: 320px;
  max-height: 80vh;
  overflow-y: auto;
  box-shadow: 0 4px 16px rgba(0,0,0,0.2);
}

.report-modal-content p {
  margin-bottom: 15px;
  font-weight: bold;
}

.report-modal-content select {
  width: 100%;
  padding: 10px;
  margin-bottom: 20px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 1rem;
}

.report-actions {
  display: flex;
  gap: 10px;
}

.report-actions button {
  flex: 1;
  padding: 10px;
  border-radius: 8px;
  border: none;
  font-weight: bold;
  cursor: pointer;
  transition: background-color 0.2s;
}

.report-actions button:first-child {
  background-color: #ed664d;
  color: white;
}

.report-actions button:first-child:hover {
  background-color: #d65c45;
}

.report-actions button:last-child {
  background-color: #f0f0f0;
  color: #333;
}

.report-actions button:last-child:hover {
  background-color: #e0e0e0;
}
</style>
