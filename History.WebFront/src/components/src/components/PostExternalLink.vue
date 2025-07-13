<template>
  <div class="external-link-container">
    <a
      :href="content.sourceUrl || content.SourceUrl || content.url || content.Url"
      target="_blank"
      rel="noopener noreferrer"
      class="external-link"
      @click.stop
    >
      <div class="link-preview" :class="{ 'has-image': !!content.thumbnailImageUrl || !!content.ThumbnailImageUrl || !!content.image || !!content.Image }">
        <img
          v-if="content.thumbnailImageUrl || content.ThumbnailImageUrl || content.image || content.Image"
          :src="content.thumbnailImageUrl || content.ThumbnailImageUrl || content.image || content.Image"
          :alt="content.title || content.Title || '링크 미리보기'"
          class="link-preview-image"
          @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
        />
        <div class="link-info">
          <div v-if="content.title || content.Title" class="link-title">{{ content.title || content.Title }}</div>
          <div v-if="content.description || content.Description" class="link-description">{{ content.description || content.Description }}</div>
          <div class="link-url"><span class="link-icon">🔗</span><span class="link-text">{{ content.sourceUrl || content.SourceUrl || content.url || content.Url }}</span></div>
        </div>
      </div>
    </a>
  </div>
</template>

<script setup lang="ts">
import { defineProps } from 'vue';

defineProps<{
  content: any; // externalUrl 타입의 콘텐츠 객체
}>();
</script>

<style scoped>
.external-link-container {
  margin-top: 0.5rem;
  display: flex;
  justify-content: center;
}

.external-link {
  text-decoration: none;
  color: inherit;
  display: block;
  max-width: 100%;
  width: 100%;
}

.link-preview {
  border: 1px solid #e1e5e9;
  border-radius: 12px;
  background: white;
  transition: all 0.2s ease;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.link-preview:hover {
  border-color: #d1d5db;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

/* 이미지가 있는 링크 미리보기 레이아웃 */
.link-preview.has-image {
  display: flex;
}

/* 큰 이미지 (일반 포스트) */
.link-preview.has-image:not(.small) {
  flex-direction: column;
}
.link-preview.has-image:not(.small) .link-preview-image {
  width: 100%;
  height: 200px;
  object-fit: cover;
}
.link-preview.has-image:not(.small) .link-info {
  padding: 16px;
}

/* 작은 이미지 (리포스트 안의 원본글) */
.link-preview.small.has-image {
  flex-direction: row;
  align-items: stretch;
}
.link-preview.small.has-image .link-preview-image {
  width: 100px;
  height: auto;
  object-fit: cover;
  flex-shrink: 0;
  border-right: 1px solid #e1e5e9;
}
.link-preview.small.has-image .link-info {
  padding: 12px;
}

/* 이미지가 없는 링크 미리보기 */
.link-preview:not(.has-image) {
  padding: 16px;
  background: #f8f9fa;
}
.link-preview.small:not(.has-image) {
  padding: 12px;
}

.link-preview-image {
  background: #f5f5f5; /* 이미지 로딩 전 배경색 */
}

.link-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0; /* flex-shrink 방지 */
}

.link-title {
  font-weight: 600;
  color: #212529;
  font-size: 1rem;
  line-height: 1.3;
  /* 2줄 이상일 경우 말줄임표 */
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp:2;
  -webkit-box-orient: vertical;
}
.link-preview.small .link-title {
  font-size: 0.9rem;
  -webkit-line-clamp: 1;
  line-clamp:1; /* 작은 UI에선 1줄 */
}

.link-description {
  color: #495057;
  font-size: 0.875rem;
  line-height: 1.4;
  /* 3줄 이상일 경우 말줄임표 */
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  line-clamp:3;
  -webkit-box-orient: vertical;
  margin: 0;
}
.link-preview.small .link-description {
  font-size: 0.8rem;
  -webkit-line-clamp: 2;
  line-clamp:2; /* 작은 UI에선 2줄 */
}

.link-url {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6c757d;
  font-size: 0.85rem;
  margin-top: 8px;
}
.link-icon {
  flex-shrink: 0;
  font-size: 14px;
}
.link-text {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
