<script setup lang="ts">
import { ref, watch } from 'vue';

const props = defineProps<{
  attachedFiles: File[];
  attachedLink: string;
}>();

const emit = defineEmits([
  'update:attachedFiles',
  'update:attachedLink',
]);

const previewItems = ref<{ url: string, isVideo: boolean }[]>([]);

const handleFileChange = (event: Event) => {
  const files = (event.target as HTMLInputElement).files;
  if (!files) return;

  const newFiles = Array.from(files);
  emit('update:attachedFiles', newFiles);

  previewItems.value = [];
  for (let i = 0; i < newFiles.length; i++) {
    const file = newFiles[i];
    previewItems.value.push({
      url: URL.createObjectURL(file),
      isVideo: file.type.startsWith('video/'),
    });
  }
};

watch(() => props.attachedFiles, (newFiles) => {
  if (newFiles.length === 0) {
    previewItems.value = [];
  }
});
</script>

<template>
  <div>
    <div class="create-post-actions">
      <label class="action-btn">
        📷📹 파일 업로드
        <input type="file" accept="image/*,video/*" multiple @change="handleFileChange" hidden />
      </label>
      <input 
        :value="attachedLink"
        @input="$emit('update:attachedLink', ($event.target as HTMLInputElement).value)"
        placeholder="🔗 링크 붙여넣기" 
        class="link-input" 
      />
    </div>

    <div v-if="previewItems.length" class="preview-box">
      <div v-for="(item, idx) in previewItems" :key="idx" class="preview-item">
        <img v-if="!item.isVideo" :src="item.url" class="preview-image" />
        <video v-else controls class="preview-video">
          <source :src="item.url" />
          브라우저가 video 태그를 지원하지 않습니다.
        </video>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* CreatePost.vue에서 복사해온 스타일 */
.create-post-actions {
  display: flex;
  gap: 8px;
  padding: 12px 0;
  border-top: 1px solid #eee;
  border-bottom: 1px solid #eee;
  margin: 12px 0;
}

.action-btn {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 20px;
  background-color: transparent;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.9rem;
}

.action-btn:hover {
  background-color: #f8f9fa;
  border-color: #ed664d;
}

.link-input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 0.9rem;
  min-width: 200px;
  transition: border-color 0.2s;
}

.link-input:focus {
  outline: none;
  border-color: #ed664d;
}

.preview-box {
  margin-top: 8px;
  max-height: 400px;
  overflow: hidden;
  border-radius: 6px;
}

.preview-image, .preview-video {
  width: 100%;
  max-height: 400px;
  border-radius: 6px;
  object-fit: contain;
}

.preview-item {
  margin-bottom: 8px;
}
</style>