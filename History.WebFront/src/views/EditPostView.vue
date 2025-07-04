<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import apiClient from '@/api';
import type { PostResponseDto } from '@/types';

/**
 * @fileoverview EditPostView 컴포넌트
 *
 * @description
 * 기존 게시글을 수정하는 페이지 컴포넌트입니다.
 * 게시글의 모든 콘텐츠 블록(텍스트, 미디어, 멘션 등)을 불러와
 * 사용자가 수정할 수 있도록 합니다.
 */

const route = useRoute();
const router = useRouter();
const postId = route.params.postId;

// 이미지 불러오기
const getMediaBlobUrl = async (mediaId: string) => {
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, {
      responseType: 'blob',
    });
    return URL.createObjectURL(response.data);
  } catch (error) {
    console.warn('미디어 로딩 실패:', mediaId);
    return '';
  }
};

// 반응형 상태 변수
const originalPost = ref<PostResponseDto | null>(null);
const editableContents = ref<any[]>([]);
const previewUrlMap = ref<Record<string, string>>({});
const newFilesMap = ref<Record<string, File>>({});

/**
 * 컴포넌트 마운트 시 원본 게시글 데이터를 불러옵니다.
 */
onMounted(async () => {
  try {
    const res = await apiClient.get(`/api/Post/${postId}`);
    originalPost.value = res.data;

    // 서버 데이터 -> UI용으로 정규화 (대소문자 및 타입별 데이터 처리)
    editableContents.value = res.data.contents.map((content: any) => {
      const normalizedContent: any = {
        $type: content.$type,
        text: content.text || content.Text,
        mediaId: content.mediaId || content.MediaId,
        description: content.description || content.Description,
        nickname: content.nickname || content.Nickname,
        userId: content.userId || content.UserId,
      };

      // 링크 타입(externalUrl)의 모든 속성을 복사
      if (content.$type === 'externalUrl') {
        normalizedContent.sourceUrl = content.sourceUrl || content.SourceUrl || content.url || content.Url;
        normalizedContent.title = content.title || content.Title;
        normalizedContent.image = content.thumbnailImageUrl || content.ThumbnailImageUrl || content.image || content.Image;
      }

      return normalizedContent;
    });

    // 기존 미디어에 대한 미리보기 URL 생성
    for (const content of editableContents.value) {
      if ((content.$type === 'image' || content.$type === 'media') && content.mediaId) {
        previewUrlMap.value[content.mediaId] = await getMediaBlobUrl(content.mediaId);
      }
    }
  } catch (error) {
    console.error('❌ 게시글 로딩 실패:', error);
    alert('게시글 정보를 불러오지 못했습니다.');
    router.push('/');
  }
});

// 텍스트 블록이 없는 경우 빈 텍스트 블록 추가
const hasTextBlock = editableContents.value.some(content => content.$type === 'text');
if (!hasTextBlock) {
  editableContents.value.unshift({
    $type: 'text',
    text: ''
  });
}

/**
 * 새로운 파일을 선택했을 때 처리합니다.
 */
const handleFileChange = (event: Event) => {
  const fileInput = event.target as HTMLInputElement;
  if (!fileInput.files) return;

  for (const file of Array.from(fileInput.files)) {
    const tempId = `new_${Date.now()}_${file.name}`;
    newFilesMap.value[tempId] = file;
    const blobUrl = URL.createObjectURL(file);
    previewUrlMap.value[tempId] = blobUrl;
    editableContents.value.push({
      $type: 'upload',
      tempId: tempId,
      fileName: file.name,
      description: ''
    });
  }
  fileInput.value = '';
};

/**
 * 콘텐츠 블록(텍스트, 미디어 등)을 편집 목록에서 제거합니다.
 */
const removeContent = (index: number) => {
  const contentToRemove = editableContents.value[index];
  const key = contentToRemove.mediaId || contentToRemove.tempId;
  if (key) {
    const url = previewUrlMap.value[key];
    if (url && url.startsWith('blob:')) {
      URL.revokeObjectURL(url);
    }
    delete previewUrlMap.value[key];
    if (contentToRemove.tempId) {
      delete newFilesMap.value[contentToRemove.tempId];
    }
  }
  editableContents.value.splice(index, 1);
};

/**
 * 수정된 게시글 데이터를 서버에 제출합니다.
 */
const handleSubmit = async () => {
  if (editableContents.value.length === 0) {
    alert('내용을 입력해주세요.');
    return;
  }

  const formData = new FormData();
  const finalContents: any[] = [];

  for (const content of editableContents.value) {
    switch (content.$type) {
      case 'text':
        if (content.text?.trim()) {
          finalContents.push({ $type: 'text', Text: content.text });
        }
        break;
      case 'media':
      case 'image':
        finalContents.push({
          $type: 'media',
          MediaId: content.mediaId,
          Description: content.description || ''
        });
        break;
      case 'upload':
        const file = newFilesMap.value[content.tempId];
        if (file) {
          formData.append('Files', file, file.name);
          finalContents.push({
            $type: 'upload',
            FileName: file.name,
            Description: content.description || ''
          });
        }
        break;
      case 'externalUrl':
        finalContents.push({
          $type: 'externalUrl',
          SourceUrl: content.sourceUrl,
          Title: content.title,
          Image: content.image,
          Description: content.description
        });
        break;
      case 'profile':
      case 'mention':
        finalContents.push({
          $type: content.$type,
          UserId: content.userId,
          Nickname: content.nickname
        });
        break;
    }
  }

  const postDto = {
    DiscoveryOption: originalPost.value?.discoveryOption || 'Friends',
    Contents: finalContents,
    ParentPostId: originalPost.value?.parentPost?.id || null,
    DiscoveryOptionSelectedUserIds: []
  };

  formData.append('JsonData', JSON.stringify(postDto));

  try {
    await apiClient.put(`/api/Post/${postId}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    alert('수정이 완료되었습니다!');
    router.push(`/post/${postId}`);
  } catch (error: any) {
    console.error('❌ 게시글 수정 실패:', error);
    const errorMsg = error.response?.data?.title || error.response?.data || '알 수 없는 오류가 발생했습니다.';
    alert(`수정 실패: ${errorMsg}`);
  }
};
</script>

<template>
  <div class="edit-container">
    <h2 class="edit-title">게시글 수정</h2>

    <div v-for="(content, index) in editableContents" :key="content.mediaId || content.tempId || index" class="content-block">
      <template v-if="content.$type === 'text'">
        <textarea
          v-model="content.text"
          placeholder="내용을 입력하세요"
          class="edit-textarea"
          rows="4"
        />
      </template>

      <template v-else-if="content.$type === 'media' || content.$type === 'image' || content.$type === 'upload'">
        <div class="image-preview">
          <img :src="previewUrlMap[content.mediaId || content.tempId]" alt="이미지 미리보기" />
        </div>
      </template>
      
      <template v-else-if="content.$type === 'externalUrl'">
        <div class="link-preview-wrapper">
          <a :href="content.sourceUrl" target="_blank" rel="noopener noreferrer" class="link-preview">
            <img v-if="content.image" :src="content.image" alt="링크 썸네일" class="link-thumbnail" @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"/>
            <div class="link-info">
              <div class="link-title">{{ content.title }}</div>
              <div class="link-description">{{ content.description }}</div>
              <div class="link-url">{{ content.sourceUrl }}</div>
            </div>
          </a>
        </div>
      </template>

      <template v-else-if="content.$type === 'profile' || content.$type === 'mention'">
        <div class="mention-chip">
          @{{ content.nickname }}
        </div>
      </template>

      <button @click="removeContent(index)" class="remove-btn" title="콘텐츠 삭제">
        &times;
      </button>
    </div>

    <div class="actions-footer">
      <label class="file-upload">
        📁 이미지 추가
        <input type="file" @change="handleFileChange" accept="image/*" multiple hidden/>
      </label>
      <button class="submit-btn" @click="handleSubmit">
        수정 완료
      </button>
    </div>
  </div>
</template>

<style scoped>
/* 전체 컨테이너 및 제목 */
.edit-container {
  max-width: 600px;
  margin: 40px auto;
  padding: 24px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
  font-family: 'Pretendard', sans-serif;
}

.edit-title {
  font-size: 1.6rem;
  margin-bottom: 24px;
  color: #333;
  font-weight: 700;
  text-align: center;
}

/* 콘텐츠 블록 스타일 */
.content-block {
  position: relative;
  margin-bottom: 20px;
  padding: 16px;
  border: 1px solid #e1e5e9;
  border-radius: 8px;
  background-color: #f8f9fa;
}

.remove-btn {
  position: absolute;
  top: -10px;
  right: -10px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: none;
  background-color: #dc3545;
  color: white;
  font-size: 1.2rem;
  font-weight: bold;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 4px rgba(0,0,0,0.2);
  transition: all 0.2s ease;
  line-height: 1;
}

.remove-btn:hover {
  background-color: #c82333;
  transform: scale(1.1);
}

/* 텍스트 입력 영역 */
.edit-textarea {
  width: 100%;
  padding: 12px;
  font-size: 1rem;
  border-radius: 6px;
  border: 1px solid #ccc;
  resize: vertical;
  min-height: 100px;
  font-family: inherit;
  line-height: 1.5;
  transition: border-color 0.2s;
}

.edit-textarea:focus {
  outline: none;
  border-color: #ed664d;
  box-shadow: 0 0 0 3px rgba(237, 102, 77, 0.15);
}

/* 이미지 미리보기 */
.image-preview {
  border-radius: 8px;
  overflow: hidden;
}

.image-preview img {
  width: 100%;
  max-height: 350px;
  object-fit: contain;
  display: block;
}

/* 링크 미리보기 스타일 */
.link-preview-wrapper {
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #dee2e6;
}
.link-preview {
  display: flex;
  text-decoration: none;
  background-color: #fff;
  color: inherit;
}
.link-thumbnail {
  width: 100px;
  height: 100px;
  object-fit: cover;
  flex-shrink: 0;
  border-right: 1px solid #dee2e6;
}
.link-info {
  padding: 12px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  overflow: hidden;
}
.link-title {
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.link-description {
  font-size: 0.9em;
  color: #6c757d;
  margin: 4px 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.link-url {
  font-size: 0.8em;
  color: #868e96;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 멘션 칩 스타일 */
.mention-chip {
    display: inline-block;
    padding: 8px 12px;
    background-color: #fff0ed;
    color: #ed664d;
    font-weight: 600;
    border-radius: 16px;
    font-size: 0.9rem;
}

/* 하단 액션 버튼 영역 */
.actions-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 24px;
  padding-top: 20px;
  border-top: 1px solid #e9ecef;
}

.file-upload {
  padding: 10px 18px;
  background-color: #f1f3f5;
  border-radius: 8px;
  border: 1px solid #ccc;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}

.file-upload:hover {
  background-color: #e9ecef;
  border-color: #adb5bd;
}

.submit-btn {
  padding: 12px 24px;
  background-color: #ed664d;
  color: white;
  font-weight: bold;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 1rem;
  transition: background-color 0.2s;
}

.submit-btn:hover {
  background-color: #d04e38;
}

.submit-btn:active {
  transform: translateY(1px);
}
</style>