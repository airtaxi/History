<!--
 * ReactionPopup.vue
 *
 * 이 컴포넌트는 게시물에 반응을 선택할 수 있는 팝업 UI를 렌더링합니다.
 * 사용자가 반응 버튼을 롱 프레스했을 때 나타나며, 다양한 반응 옵션을 제공합니다.
 *
 * @props {
 *   show: boolean - 팝업의 표시 여부.
 *   position: { top: string; left: string; } - 팝업이 표시될 화면상의 위치.
 *   myReaction: string | null - 현재 사용자가 선택한 반응 타입 (선택된 반응을 시각적으로 표시).
 * }
 * @emits {
 *   select-reaction: 사용자가 특정 반응을 선택했을 때 발생. 선택된 반응 타입(string)을 페이로드로 전달.
 * }
-->
<template>
  <Teleport to="body">
    <div
      v-if="show"
      class="reaction-popup"
      :style="position"
      @click.stop
    >
      <button
        @click="$emit('select-reaction', 'Like')"
        class="popup-reaction-btn"
        :class="{ active: myReaction === 'Like' }"
        aria-label="좋아요 반응"
        :aria-pressed="myReaction === 'Like'"
      >
        <span class="popup-emoji" aria-hidden="true">👍</span>
        <span class="popup-label">좋아요</span>
      </button>
      <button
        @click="$emit('select-reaction', 'Awesome')"
        class="popup-reaction-btn"
        :class="{ active: myReaction === 'Awesome' }"
        aria-label="멋져요 반응"
        :aria-pressed="myReaction === 'Awesome'"
      >
        <span class="popup-emoji" aria-hidden="true">🔥</span>
        <span class="popup-label">멋져요</span>
      </button>
      <button
        @click="$emit('select-reaction', 'Happy')"
        class="popup-reaction-btn"
        :class="{ active: myReaction === 'Happy' }"
        aria-label="기뻐요 반응"
        :aria-pressed="myReaction === 'Happy'"
      >
        <span class="popup-emoji" aria-hidden="true">😄</span>
        <span class="popup-label">기뻐요</span>
      </button>
      <button
        @click="$emit('select-reaction', 'Sad')"
        class="popup-reaction-btn"
        :class="{ active: myReaction === 'Sad' }"
        aria-label="슬퍼요 반응"
        :aria-pressed="myReaction === 'Sad'"
      >
        <span class="popup-emoji" aria-hidden="true">😢</span>
        <span class="popup-label">슬퍼요</span>
      </button>
      <button
        @click="$emit('select-reaction', 'Support')"
        class="popup-reaction-btn"
        :class="{ active: myReaction === 'Support' }"
        aria-label="힘내요 반응"
        :aria-pressed="myReaction === 'Support'"
      >
        <span class="popup-emoji" aria-hidden="true">💪</span>
        <span class="popup-label">힘내요</span>
      </button>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';

const props = defineProps<{ // eslint-disable-line @typescript-eslint/no-unused-vars
  show: boolean;
  position: { top: string; left: string; };
  myReaction: string | null;
}>();

const emit = defineEmits(['select-reaction']); // eslint-disable-line @typescript-eslint/no-unused-vars
</script>

<style scoped>
.reaction-popup {
  position: fixed;
  transform: translateX(-50%);
  background: white;
  border-radius: 32px;
  padding: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  display: flex;
  gap: 4px;
  z-index: 9999;
  animation: popupSlideIn 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

@keyframes popupSlideIn {
  from { opacity: 0; transform: translateX(-50%) translateY(20px) scale(0.6); }
  to { opacity: 1; transform: translateX(-50%) translateY(0) scale(1); }
}

.popup-reaction-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  background: none;
  border: none;
  padding: 8px 12px;
  border-radius: 24px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.popup-reaction-btn:hover {
  background-color: #f8f9fa;
  transform: scale(1.1);
}

.popup-reaction-btn.active {
  background-color: #fef7f5;
}

.popup-emoji {
  font-size: 24px;
}

.popup-label {
  font-size: 11px;
  color: #6c757d;
  font-weight: 500;
}
</style>
