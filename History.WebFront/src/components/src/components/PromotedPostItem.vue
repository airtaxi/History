<script setup lang="ts">
import type { PostResponseDto } from '@/types';
import PostHeader from '@/components/src/components/PostHeader.vue';
import PostContent from '@/components/src/components/PostContent.vue';
import defaultProfileImage from '@/components/src/assets/images/default_profile_image.jpg';

defineProps<{
  post: PostResponseDto;
  profileImageMap: Record<string, string>;
  mediaUrlMap: Record<string, string>;
}>();
</script>

<template>
  <RouterLink :to="`/user/${post.user.userId}`" class="promoted-item-link">
    <div class="promoted-post-item">
      <PostHeader
        :post="post"
        :profile-image-url="profileImageMap[post.user.userId] || defaultProfileImage"
        :can-edit="false" 
      />

      <div class="content-wrapper">
        <PostContent
          :contents="post.contents"
          :media-url-map="mediaUrlMap" 
        />
      </div>
    </div>
  </RouterLink>
</template>

<style scoped>
.promoted-item-link {
  display: block;
  text-decoration: none;
  color: inherit;
  border-radius: 8px;
  transition: box-shadow 0.2s ease-in-out;
}
.promoted-item-link:hover {
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}
.promoted-post-item {
  background: #fff;
  border: 1px solid #ddd;
  padding: 16px;
  border-radius: 8px;
}
.content-wrapper {
  margin-top: 12px;
}
</style>