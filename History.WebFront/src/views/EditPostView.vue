<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import apiClient from '@/api';
import type { PostResponseDto } from '@/types';

/**
 * EditPostView 컴포넌트
 * 
 * @description
 * 기존 게시글을 수정하는 페이지 컴포넌트입니다.
 * 게시글의 텍스트 내용과 이미지를 수정할 수 있으며,
 * 작성자만 수정할 수 있도록 권한이 제한됩니다.
 * 
 * @features
 * - 게시글 내용 수정 (텍스트 + 이미지)
 * - 기존 내용 미리보기
 * - 새로운 이미지 파일 업로드
 * - 실시간 이미지 미리보기
 * - 권한 기반 접근 제어
 */

const route = useRoute();
const router = useRouter();

/**
 * URL 파라미터에서 추출한 게시글 ID
 * @type {string | string[]}
 * @description 수정할 게시글의 고유 식별자
 */
const postId = route.params.postId;

// ==================== 반응형 상태 변수들 ====================

/**
 * 수정할 원본 게시글 데이터
 * @type {import('vue').Ref<PostResponseDto | null>}
 * @description 서버에서 불러온 원본 게시글의 전체 정보를 저장합니다.
 */
const originalPost = ref<PostResponseDto | null>(null);

/**
 * 수정 중인 게시글의 텍스트 내용
 * @type {import('vue').Ref<string>}
 * @description 사용자가 편집하고 있는 게시글의 텍스트 내용입니다.
 */
const textContent = ref('');

/**
 * 사용자가 선택한 새로운 이미지 파일
 * @type {import('vue').Ref<File | null>}
 * @description 게시글에 첨부할 새로운 이미지 파일입니다. null이면 기존 이미지 유지됩니다.
 */
const selectedFile = ref<File | null>(null);

/**
 * 이미지 미리보기를 위한 URL
 * @type {import('vue').Ref<string>}
 * @description 기존 이미지의 API URL 또는 새로 선택한 파일의 Blob URL을 저장합니다.
 */
const previewUrl = ref('');

// ==================== 생명주기 및 초기화 ====================

/**
 * 컴포넌트 마운트 시 게시글 데이터를 로드하는 함수
 * 
 * @async
 * @function loadOriginalPost
 * @returns {Promise<void>}
 * 
 * @description
 * 1. URL 파라미터의 postId로 게시글 데이터를 API에서 조회합니다.
 * 2. 텍스트 콘텐츠를 textContent에 설정합니다.
 * 3. 이미지/미디어 콘텐츠가 있으면 미리보기 URL을 설정합니다.
 * 4. 로딩 실패 시 사용자에게 알림하고 홈으로 리다이렉트합니다.
 * 
 * @throws {Error} 게시글 로딩 실패 시 에러를 throw하고 홈으로 이동
 * 
 * @example
 * // 컴포넌트 마운트 시 자동 실행됨
 * onMounted(() => { loadOriginalPost(); });
 */
onMounted(() => {
  console.log('📋 게시글 수정 페이지 로딩 시작...');
  console.log('🆔 수정할 게시글 ID:', postId);
  
  apiClient.get(`/api/Post/${postId}`)
    .then((res) => {
      console.log('✅ 원본 게시글 로딩 성공:', res.data);
      originalPost.value = res.data;
      
      // 텍스트 콘텐츠 추출
      const textContentItem = res.data.contents.find((c: any) => c.$type === 'text');
      textContent.value = textContentItem?.text || textContentItem?.Text || '';
      console.log('📝 추출된 텍스트 내용:', textContent.value);
      
      // 이미지/미디어 콘텐츠 추출
      const mediaContentItem = res.data.contents.find((c: any) => 
        c.$type === 'image' || c.$type === 'media'
      );
      
      if (mediaContentItem?.mediaId || mediaContentItem?.thumbnailMediaId) {
        const mediaId = mediaContentItem.mediaId || mediaContentItem.thumbnailMediaId;
        previewUrl.value = `/api/Media/${mediaId}`;
        console.log('🖼️ 기존 이미지 미리보기 URL 설정:', previewUrl.value);
      }
    })
    .catch((error) => {
      console.error('❌ 게시글 로딩 실패:', error);
      console.error('📊 에러 상세정보:', {
        message: error.message,
        response: error.response?.data,
        status: error.response?.status,
        statusText: error.response?.statusText
      });
      
      alert('게시글을 불러오지 못했습니다.');
      router.push('/');
    });
});

// ==================== 이벤트 핸들러 함수들 ====================

/**
 * 파일 선택 시 처리하는 함수
 * 
 * @function handleFileChange
 * @param {Event} event - input file 요소의 change 이벤트
 * @returns {void}
 * 
 * @description
 * 1. 사용자가 선택한 파일을 selectedFile에 저장합니다.
 * 2. 선택한 파일의 Blob URL을 생성하여 즉시 미리보기를 제공합니다.
 * 3. 기존 이미지를 새로운 이미지로 교체하는 역할을 합니다.
 * 
 * @example
 * // template에서 사용
 * <input type="file" @change="handleFileChange" accept="image/*" />
 */
const handleFileChange = (event: Event) => {
  const fileInput = event.target as HTMLInputElement;
  
  if (fileInput.files && fileInput.files[0]) {
    const file = fileInput.files[0];
    selectedFile.value = file;
    
    // 기존 Blob URL 정리 (메모리 누수 방지)
    if (previewUrl.value && previewUrl.value.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl.value);
    }
    
    // 새로운 미리보기 URL 생성
    previewUrl.value = URL.createObjectURL(file);
    
    console.log('🖼️ 새로운 파일 선택됨:', {
      name: file.name,
      size: file.size,
      type: file.type,
      previewUrl: previewUrl.value
    });
  }
};

/**
 * 게시글 수정 제출을 처리하는 함수
 * 
 * @async
 * @function handleSubmit
 * @returns {Promise<void>}
 * 
 * @description
 * 수정된 게시글 데이터를 서버에 전송하는 함수입니다.
 * 
 * **처리 과정:**
 * 1. **입력 검증**: 텍스트 내용이 비어있지 않은지 확인
 * 2. **데이터 구성**: API 요구 형식에 맞게 postDto 객체 생성
 * 3. **FormData 생성**: 텍스트와 파일을 함께 전송하기 위한 FormData 구성
 * 4. **API 호출**: PUT 메서드로 게시글 수정 API 호출
 * 5. **결과 처리**: 성공 시 게시글 상세 페이지로 이동, 실패 시 에러 처리
 * 
 * **에러 처리:**
 * - 500 에러: 서버 내부 오류 메시지 표시
 * - 기타 에러: API 응답 데이터 또는 일반 오류 메시지 표시
 * 
 * @throws {Error} 게시글 수정 실패 시 에러를 catch하고 사용자에게 알림
 * 
 * @example
 * // template에서 사용
 * <button @click="handleSubmit">수정하기</button>
 */
const handleSubmit = async () => {
  console.log('🚀 게시글 수정 제출 시작...');
  
  // 입력 검증
  if (!textContent.value.trim()) {
    alert('내용을 입력해주세요.');
    return;
  }

  try {
    // 게시글 수정용 데이터 타입을 명시적으로 정의하여 타입 안전성 확보
    const postDto: {
      DiscoveryOption: string;
      Contents: Array<{
        $type: string;
        Text?: string;
        FileName?: string;
        Description?: string;
      }>;
      ParentPostId: string | null;
      DiscoveryOptionSelectedUserIds: string[];
    } = {
      DiscoveryOption: 'Friends',
      Contents: [],
      ParentPostId: null,
      DiscoveryOptionSelectedUserIds: []
    };

    // 텍스트 내용이 있는 경우 Contents 배열에 추가
    if (textContent.value.trim()) {
      postDto.Contents.push({ 
        $type: 'text', 
        Text: textContent.value.trim() 
      });
    }

    const formData = new FormData();

    // 새로운 파일이 선택된 경우 FormData와 Contents에 추가
    if (selectedFile.value) {
      formData.append('Files', selectedFile.value, selectedFile.value.name);
      postDto.Contents.push({
        $type: 'upload',
        FileName: selectedFile.value.name,
        Description: ''
      });
      
      console.log('📎 새로운 파일 첨부:', {
        name: selectedFile.value.name,
        size: selectedFile.value.size,
        type: selectedFile.value.type
      });
    }

    console.log('📋 게시글 수정용 postDto:', postDto);
    formData.append('JsonData', JSON.stringify(postDto));

    console.log('🚀 게시글 수정 API 호출 중...');
    const response = await apiClient.put(`/api/Post/${postId}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    
    console.log('✅ 게시글 수정 성공:', response.data);
    alert('수정 완료!');
    
    // 수정 완료 후 게시글 상세 페이지로 이동
    router.push(`/post/${postId}`);
    
  } catch (error: any) {
    console.error('❌ 게시글 수정 실패:', error);
    console.error('📊 에러 상세정보:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status,
      statusText: error.response?.statusText
    });
    
    // 에러 타입에 따른 사용자 친화적 메시지 표시
    if (error.response?.status === 500) {
      alert('서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해주세요.');
    } else if (error.response?.status === 403) {
      alert('게시글을 수정할 권한이 없습니다.');
    } else if (error.response?.status === 404) {
      alert('게시글을 찾을 수 없습니다.');
    } else if (error.response?.data) {
      alert(`수정 실패: ${error.response.data}`);
    } else {
      alert('수정에 실패했습니다. 네트워크 연결을 확인해주세요.');
    }
  }
};
</script>

<template>
  <!-- 
    게시글 수정 폼 컨테이너
    
    @description
    사용자가 기존 게시글의 내용을 수정할 수 있는 인터페이스를 제공합니다.
    텍스트 내용 편집, 이미지 교체, 실시간 미리보기 기능을 포함합니다.
  -->
  <div class="edit-container">
    <!-- 페이지 제목 -->
    <h2 class="edit-title">게시글 수정</h2>

    <!-- 
      텍스트 내용 편집 영역
      
      @features
      - 기존 텍스트 내용 표시
      - 실시간 편집 가능
      - 플레이스홀더 텍스트 제공
    -->
    <textarea
      v-model="textContent"
      placeholder="내용을 입력하세요"
      class="edit-textarea"
      rows="5"
    />

    <!-- 
      이미지 미리보기 영역
      
      @condition previewUrl이 존재할 때만 표시
      @description 기존 이미지 또는 새로 선택한 이미지를 미리보기로 표시
    -->
    <div v-if="previewUrl" class="image-preview">
      <img :src="previewUrl" alt="미리보기" />
    </div>

    <!-- 
      파일 업로드 버튼
      
      @features
      - 이미지 파일만 선택 가능 (accept="image/*")
      - 숨겨진 input을 사용한 커스텀 디자인
      - 선택 시 즉시 미리보기 업데이트
    -->
    <label class="file-upload">
      📁 이미지 선택
      <input 
        type="file" 
        @change="handleFileChange" 
        accept="image/*" 
        hidden 
      />
    </label>

    <!-- 
      수정 제출 버튼
      
      @action handleSubmit 함수 호출
      @description 수정된 내용을 서버에 전송하고 결과를 처리
    -->
    <button class="submit-btn" @click="handleSubmit">
      수정하기
    </button>
  </div>
</template>

<style scoped>
/* 
  게시글 수정 페이지 전용 스타일
  
  @design_principles
  - 깔끔하고 직관적인 인터페이스
  - 브랜드 색상 (#ed664d) 활용
  - 반응형 디자인 적용
  - 사용자 경험 중심의 인터랙션
*/

.edit-container {
  max-width: 600px;
  margin: 40px auto;
  padding: 24px;
  background: #fff;
  border-radius: 10px;
  box-shadow: 0 2px 6px rgba(0,0,0,0.1);
}

.edit-title {
  font-size: 1.4rem;
  margin-bottom: 20px;
  color: #333;
  font-weight: 600;
}

.edit-textarea {
  width: 100%;
  padding: 12px;
  font-size: 1rem;
  border-radius: 6px;
  border: 1px solid #ccc;
  margin-bottom: 16px;
  resize: vertical;
  min-height: 120px;
  font-family: inherit;
  line-height: 1.5;
  transition: border-color 0.2s;
}

.edit-textarea:focus {
  outline: none;
  border-color: #ed664d;
  box-shadow: 0 0 0 2px rgba(237, 102, 77, 0.1);
}

.image-preview {  
  margin-bottom: 16px;
  border-radius: 8px;
  overflow: hidden;
}

.image-preview img {
  width: 100%;
  max-height: 300px;
  object-fit: contain;
  border-radius: 8px;
}

.file-upload {
  display: inline-block;
  padding: 10px 16px;
  background-color: #f1f3f5;
  border-radius: 6px;
  border: 1px solid #ccc;
  cursor: pointer;
  margin-bottom: 16px;
  transition: all 0.2s;
  font-weight: 500;
}

.file-upload:hover {
  background-color: #e9ecef;
  border-color: #adb5bd;
}

.submit-btn {
  width: 100%;
  padding: 12px;
  background-color: #ed664d;
  color: white;
  font-weight: bold;
  border: none;
  border-radius: 6px;
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

@media (max-width: 768px) {
  .edit-container {
    margin: 20px;
    padding: 16px;
  }
  
  .edit-title {
    font-size: 1.2rem;
  }
}
</style>
