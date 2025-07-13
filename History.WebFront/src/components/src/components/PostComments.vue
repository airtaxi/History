<template>
  <div class="post-comments-section">
    <!-- 댓글 목록 -->
    <div v-if="visibleComments.length > 0" class="comment-list">
      <div v-for="comment in visibleComments" :key="comment.id" class="comment-item">
        <span class="comment-author">{{ comment.user.nickname }}</span>
        <p class="comment-content">{{ comment.content }}</p>
      </div>
    </div>

    <!-- 댓글 더보기/접기 버튼 -->
    <div class="comment-controls">
      <button v-if="!isExpanded && totalCommentCount > 0" @click="onToggleExpand">
        {{ totalCommentCount }}개의 댓글 모두 보기
      </button>
      <button v-if="hasMoreComments && isExpanded" @click="onLoadMore">
        이전 댓글 더 보기
      </button>
    </div>

    <!-- 댓글 입력창 (기본 숨김, 푸터의 댓글 아이콘 클릭 시 표시) -->
    <div v-if="isCommentInputVisible" class="comment-input-wrapper">
      <textarea placeholder="댓글을 입력하세요..."></textarea>
      <button>등록</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';
import type { CommentDto } from '@/types';

defineProps<{
  visibleComments: CommentDto[];
  totalCommentCount: number;
  isExpanded: boolean;
  hasMoreComments: boolean;
  isCommentInputVisible: boolean;
}>();

const emit = defineEmits<{
  (e: 'toggle-expand'): void;
  (e: 'load-more'): void;
}>();

const onToggleExpand = () => {
  emit('toggle-expand');
};

const onLoadMore = () => {
  emit('load-more');
};
</script>

<style scoped>
.post-comments-section {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px solid #eee;
}

.comment-item {
  margin-bottom: 8px;
  font-size: 14px;
}

.comment-author {
  font-weight: bold;
  margin-right: 8px;
}

.comment-content {
  display: inline;
  color: #333;
}

.comment-controls button {
  background: none;
  border: none;
  color: #888;
  cursor: pointer;
  font-size: 14px;
  padding: 4px 0;
}

.comment-input-wrapper {
  display: flex;
  margin-top: 12px;
}

.comment-input-wrapper textarea {
  flex-grow: 1;
  border: 1px solid #ddd;
  border-radius: 4px;
  padding: 8px;
  resize: vertical;
}

.comment-input-wrapper button {
  margin-left: 8px;
  padding: 8px 12px;
  border: none;
  background-color: #1da1f2;
  color: white;
  border-radius: 4px;
  cursor: pointer;
}
</style>
