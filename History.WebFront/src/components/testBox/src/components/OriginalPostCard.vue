<template>
  <div class="original-post-card" @click="$emit('navigate-to-original', post.id)">
    <PostHeader
      :user="post.author || { userId: '', nickname: 'Unknown User', profileThumbnailMediaId: '' }"
      :profile-image-url="profileBlobUrlMap[post.author?.id] || defaultProfileImage"
      :created-at="post.createdAt"
      :can-edit="false"
    />
    <PostContent :contents="post.contents || []" :mediaUrlMap="mediaUrlMap" />
  </div>
</template>

<script setup lang="ts">
import PostContent from './PostContent.vue';
import PostHeader from './PostHeader.vue'; // PostHeader 컴포넌트 import
import defaultProfileImage from '@/assets/images/default_profile_image.jpg';

const props = defineProps({
  post: {
    type: Object,
    required: true,
  },
  profileBlobUrlMap: {
    type: Object,
    required: false, // required를 false로 변경
    default: () => ({}), // 기본값으로 빈 객체 설정
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

/* 기존 .header, .avatar, .author-name 스타일은 PostHeader.vue에서 관리 */
</style>
