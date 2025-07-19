<script setup lang="ts">


const props = defineProps<{ // eslint-disable-line @typescript-eslint/no-unused-vars
  attachedLink: string;
  previewItems: { url: string; isVideo: boolean; file: File }[];
}>();

const emit = defineEmits([
  'update:attachedLink',
  'add-files',
  'remove-file',
]);

const handleFileChange = (event: Event) => {
  const files = (event.target as HTMLInputElement).files;
  if (files) {
    emit('add-files', files);
    // 파일 선택 후 input 초기화 (동일 파일 재선택 가능하게)
    (event.target as HTMLInputElement).value = '';
  }
};

const removeFile = (index: number) => {
  emit('remove-file', index);
};
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

    <div v-if="previewItems.length" class="preview-grid">
      <div v-for="(item, idx) in previewItems" :key="item.url" class="preview-item">
        <img v-if="!item.isVideo" :src="item.url" class="preview-thumbnail" :alt="`미리보기 ${idx + 1}`" />
        <video v-else controls class="preview-thumbnail">
          <source :src="item.url" />
          브라우저가 video 태그를 지원하지 않습니다.
        </video>
        <button @click="removeFile(idx)" class="remove-preview-btn">×</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
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

.preview-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 10px;
}

.preview-item {
  position: relative;
  width: 100px; /* 썸네일 크기 고정 */
  height: 100px; /* 썸네일 크기 고정 */
  border: 1px solid #eee;
  border-radius: 6px;
  overflow: hidden;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #f0f2f5;
}

.preview-thumbnail {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain; /* 이미지가 잘리지 않고 전체가 보이도록 */
}

.remove-preview-btn {
  position: absolute;
  top: 5px;
  right: 5px;
  background-color: rgba(0, 0, 0, 0.6);
  color: white;
  border: none;
  border-radius: 50%;
  width: 20px;
  height: 20px;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.remove-preview-btn:hover {
  background-color: rgba(0, 0, 0, 0.8);
}
</style>