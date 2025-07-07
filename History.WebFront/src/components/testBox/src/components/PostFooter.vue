<template>
  <div class="post-footer">
    <div class="actions">
      <button @click="handleReactionClick">
        Reaction ({{ totalReactions }})
      </button>
      <button @click="$emit('open-detail')">Comments ({{ post.commentCount }})</button>
      <button
        @mousedown="startShareLongPress"
        @mouseup="endShareLongPress"
        @mouseleave="endShareLongPress"
      >
        Share
      </button>
      <button
        @mousedown="startRepostLongPress"
        @mouseup="endRepostLongPress"
        @mouseleave="endRepostLongPress"
      >
        Repost ({{ post.repostCount }})
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useReactions } from '../composables/useReactions';
import { useLongPress } from '../composables/useLongPress';

const props = defineProps({
  post: {
    type: Object,
    required: true,
  },
  myReaction: {
    type: String,
    default: null,
  },
  totalReactions: {
    type: Number,
    default: 0,
  },
});

const emit = defineEmits(['open-detail']);

const { postReaction, showReactionPopup } = useReactions();

const handleReactionClick = () => {
  // This will eventually open the reaction popup or toggle a reaction
  console.log('Reaction button clicked');
  // For now, just a placeholder
  postReaction();
};

const { onLongPress: onShareLongPress } = useLongPress();
const { onLongPress: onRepostLongPress } = useLongPress();

const { start: startShareLongPress, end: endShareLongPress } = onShareLongPress(() => {
  console.log('Share long press activated');
  // Call openShareEditor from usePostActions here
}, 500);

const { start: startRepostLongPress, end: endRepostLongPress } = onRepostLongPress(() => {
  console.log('Repost long press activated');
  // Call handleInstantRepost from usePostActions here
}, 500);

</script>

<style scoped>
.post-footer {
  padding: 10px;
  border-top: 1px solid #eee;
}

.actions button {
  margin-right: 10px;
  padding: 8px 12px;
  border: 1px solid #ccc;
  border-radius: 5px;
  background-color: #f9f9f9;
  cursor: pointer;
}

.actions button:hover {
  background-color: #e9e9e9;
}
</style>
