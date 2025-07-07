<template>
  <div class="original-post-card" @click="$emit('navigate-to-original', post.id)">
    <div class="header">
      <img :src="profileBlobUrlMap[post.author.id]" alt="Author Avatar" class="avatar" />
      <span class="author-name">{{ post.author.nickname }}</span>
    </div>
    <PostContent :contents="post.contents" :mediaUrlMap="mediaUrlMap" />
  </div>
</template>

<script setup lang="ts">
import PostContent from './PostContent.vue';

const props = defineProps({
  post: {
    type: Object,
    required: true,
  },
  profileBlobUrlMap: {
    type: Object,
    required: true,
  },
  mediaUrlMap: {
    type: Object,
    required: true,
  },
});

const emit = defineEmits(['navigate-to-original']);
</script>

<style scoped>
.original-post-card {
  border: 1px solid #eee;
  border-radius: 8px;
  margin-top: 10px;
  padding: 10px;
  cursor: pointer;
  background-color: #f9f9f9;
}

.original-post-card:hover {
  background-color: #f0f0f0;
}

.header {
  display: flex;
  align-items: center;
  margin-bottom: 10px;
}

.avatar {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  margin-right: 8px;
}

.author-name {
  font-weight: bold;
  font-size: 0.9em;
}
</style>
