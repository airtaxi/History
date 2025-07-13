<!--
 * PostFooter.vue
 *
 * 이 컴포넌트는 게시물의 하단 인터랙션 영역을 렌더링합니다.
 * 반응, 댓글, 공유, 리포스트 버튼 및 관련 카운트를 표시하며,
 * 사용자의 인터랙션에 따라 적절한 이벤트를 발생시킵니다.
 *
 * @props {
 *   post: PostResponseDto - 현재 게시물 데이터 객체.
 *   myReaction: string | null - 현재 사용자가 누른 반응 타입.
 *   totalReactions: number - 게시물의 총 반응 수.
 * }
 * @emits {
 *   open-detail: 댓글 버튼 클릭 시 게시물 상세 페이지를 열기 위해 발생.
 *   handle-reaction-click: 반응 버튼 클릭 시 발생.
 *   start-long-press: 반응 버튼 롱 프레스 시작 시 발생.
 *   end-long-press: 반응 버튼 롱 프레스 종료 시 발생.
 *   open-share-editor: 공유 버튼 클릭 시 공유 에디터를 열기 위해 발생.
 *   handle-instant-repost: 리포스트 버튼 클릭 시 즉시 리포스트를 처리하기 위해 발생.
 *   show-shared-users-modal: 공유 버튼 롱 프레스 시 공유 사용자 목록 모달을 열기 위해 발생.
 *   show-reposted-users-modal: 리포스트 버튼 롱 프레스 시 리포스트 사용자 목록 모달을 열기 위해 발생.
 * }
-->
<template>
  <div class="post-footer">
    <button
      @click.stop="$emit('handle-reaction-click', $event)"
      @mousedown.stop="$emit('start-long-press', $event)"
      @mouseup.stop="$emit('end-long-press', $event)"
      @mouseleave.stop="$emit('end-long-press', $event)"
      @touchstart.stop="$emit('start-long-press', $event)"
      @touchend.stop="$emit('end-long-press', $event)"
      class="footer-btn"
      :class="{ active: myReaction }"
    >
      <span v-if="!myReaction">🤍</span>
      <span v-else-if="myReaction === 'Like'">❤️</span>
      <span v-else-if="myReaction === 'Awesome'">🔥</span>
      <span v-else-if="myReaction === 'Happy'">😄</span>
      <span v-else-if="myReaction === 'Sad'">😢</span>
      <span v-else-if="myReaction === 'Support'">💪</span>
      <span>{{ totalReactions }}</span>
    </button>
    <button @click.stop="$emit('open-comment-input')" class="footer-btn">
      <span>💬 {{ post.commentsCount || 0 }}</span>
    </button>
    <button
      @mousedown.stop="shareLongPress.start(() => { console.log('[PostFooter] Long press 콜백 실행됨'); emit('show-shared-users-modal'); })"
      @mouseup.stop="shareLongPress.end"
      @mouseleave.stop="shareLongPress.end"
      @touchstart.stop="shareLongPress.start(() => { console.log('[PostFooter] Long press 콜백 실행됨'); emit('show-shared-users-modal'); })"
      @touchend.stop="shareLongPress.end"
      @click.stop="handleShareClick"
      class="footer-btn"
      title="공유하기"
    >
      <i class="fa-solid fa-share-from-square"></i>
      <span v-if="(post.sharedAndRepostedUsers ?? []).filter(u => !u.isRepost).length > 0">
        {{ post.sharedAndRepostedUsers?.filter(u => !u.isRepost).length }}
      </span>
    </button>
    <button
      @mousedown.stop="repostLongPress.start(() => emit('show-reposted-users-modal'))"
      @mouseup.stop="repostLongPress.end"
      @mouseleave.stop="repostLongPress.end"
      @touchstart.stop="repostLongPress.start(() => emit('show-reposted-users-modal'))"
      @touchend.stop="repostLongPress.end"
      @click.stop="handleRepostClick"
      class="footer-btn repost-btn"
      title="리포스트하기"
    >
      <i class="fa-solid fa-circle-up"></i>
      <span v-if="(post.sharedAndRepostedUsers ?? []).filter(u => u.isRepost).length > 0" class="repost-count">
        {{ post.sharedAndRepostedUsers?.filter(u => u.isRepost).length }}
      </span>
    </button>
  </div>
</template>

<script setup lang="ts">
import { defineProps, defineEmits, ref } from 'vue';
import { useLongPress } from '@/components/src/composables/useLongPress';
import type { PostResponseDto } from '@/types';

const props = defineProps<{
  post: PostResponseDto;
  myReaction: string | null;
  totalReactions: number;
}>();

const emit = defineEmits([
  'open-comment-input',
  'handle-reaction-click',
  'start-long-press',
  'end-long-press',
  'open-share-editor',
  'handle-instant-repost',
  'show-shared-users-modal',
  'show-reposted-users-modal',
]);

const shareLongPress = useLongPress();
const repostLongPress = useLongPress();

const handleShareClick = () => {
  console.log(`[PostFooter] handleShareClick 호출됨. isLongPressing: ${shareLongPress.isLongPressing.value}`);
  if (shareLongPress.isLongPressing.value) {
    console.log('[PostFooter] Long press였으므로, open-share-editor 이벤트를 발생시키지 않음');
    return;
  }
  console.log('[PostFooter] open-share-editor 이벤트 발생!');
  emit('open-share-editor');
};

const handleRepostClick = (event: MouseEvent) => {
  if (repostLongPress.isLongPressing.value) {
    return;
  }
  emit('handle-instant-repost', event);
};
</script>

<style scoped>
.post-footer {
  display: flex;
  justify-content: space-around;
  border-top: 1px solid #eee;
  padding-top: 10px;
  margin-top: 16px;
}

.footer-btn {
  background: none;
  border: none;
  font-size: 14px;
  color: #666;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 8px 12px;
  border-radius: 20px;
  transition: background-color 0.2s ease;
}

.footer-btn:hover {
  background-color: #f8f9fa;
}

.footer-btn.active {
  color: #ed664d;
  font-weight: 600;
}

.repost-btn {
  position: relative;
}

.repost-btn .repost-icon {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.repost-btn:hover .repost-icon {
  stroke: #22c55e;
  transform: rotate(180deg);
}

.repost-btn.active .repost-icon {
  stroke: #22c55e;
}

.repost-count {
  font-size: 14px;
  font-weight: 600;
  margin-left: 2px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.footer-btn i.fa-share-from-square {
  font-size: 16px;
}

.footer-btn svg.repost-icon {
  width: 20px;
  height: 20px;
}
</style>
